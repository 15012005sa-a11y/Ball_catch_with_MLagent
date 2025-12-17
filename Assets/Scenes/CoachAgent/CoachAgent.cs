using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using CoachEnv;

/// <summary>
/// CoachAgent (ML-Agents): регулирует сложность Ball Catch через RL.
/// 
/// Ключевая идея:
///  - Наблюдение: метрики окна из 10 мячей (ScoreManager.OnWindowFinished) + текущее состояние сложности.
///  - Действие: агент меняет параметры сложности (spawnInterval, ballSpeed, targetRadius, spawnRadius) и bias спавна.
///  - Награда: максимальна, когда пациент в "зоне потока" (примерно targetSR ± tolerance).
/// 
/// ВАЖНО:
///  - Здесь НЕТ жесткого "если плохо — уменьши скорость". Агент сам учится через reward.
///  - Решения запрашиваются ПОСЛЕ каждого окна из 10 мячей (RequestDecision() в OnWindowFinished).
///    Поэтому желательно убрать/отключить DecisionRequester или поставить ему большой период.
/// </summary>
public class CoachAgent : Agent
{
    [Header("Links")]
    public DifficultyController difficulty;       // Управляет spawnInterval/ballSpeed/...
    public BallSpawnerBallCatch spawner;         // Нужен для aiSpawnBias и рескейджула спавна
    public ScoreManager score;                   // Источник окна 10 мячей (OnWindowFinished)
    public PerformanceWindow perf;              // (Опционально) ROM/RT, если у тебя уже есть

    [SerializeField] private CoachVisualizer visualizer;

    [Header("Flow targets")]
    [Range(0.50f, 0.90f)] public float targetSR = 0.75f;
    [Range(0.00f, 0.20f)] public float tolerance = 0.05f;

    [Header("Norm ranges")]
    [Tooltip("Максимум для нормировки времени реакции (сек)")]
    public float maxReactionSec = 2.0f;
    [Tooltip("Максимум для нормировки throughput (мяч/мин). Пример: 120 = 2 мяча/сек")]
    public float maxThroughputPerMin = 120f;

    [Header("Action scales per decision (10-ball window)")]
    [Tooltip("Максимальное изменение интервала спавна за одно решение (сек)")]
    public float dSpawnIntervalMax = 0.15f;
    [Tooltip("Максимальное изменение скорости шара за одно решение (м/с)")]
    public float dBallSpeedMax = 0.50f;
    [Tooltip("Максимальное изменение радиуса цели за одно решение (м)")]
    public float dTargetRadiusMax = 0.03f;
    [Tooltip("Максимальное изменение радиуса области спавна за одно решение (м)")]
    public float dSpawnRadiusMax = 0.10f;
    [Tooltip("Максимальное изменение смещения точки спавна за одно решение (в долях диапазона -1..1)")]
    public float dSpawnBiasMax = 0.30f;

    [Header("Episode")]
    [Tooltip("Сколько окон (по 10 мячей) в одном эпизоде обучения")]
    public int windowsPerEpisode = 30;

    // Для UI/дебага
    public float CurrentBallSpeed;
    public float CurrentSpawnInterval;

    // --- internal state (последнее окно) ---
    private float _lastHitRate = 1f;          // 0..1
    private float _lastReactionSec = 0f;      // sec
    private float _lastThroughput01 = 0f;     // 0..1
    private int _windowCountThisEpisode = 0;

    protected override void OnEnable()
    {
        base.OnEnable();
        HookScore();
    }

    private void Start()
    {
        // на случай, если порядок инициализации такой, что ScoreManager появился позже
        HookScore();
    }

    protected override void OnDisable()
    {
        if (score != null)
            score.OnWindowFinished -= OnWindowFinished;

        base.OnDisable();
    }

    private void HookScore()
    {
        if (score == null)
            score = ScoreManager.Instance != null ? ScoreManager.Instance : FindObjectOfType<ScoreManager>(true);

        if (score != null)
        {
            score.OnWindowFinished -= OnWindowFinished;
            score.OnWindowFinished += OnWindowFinished;
        }
    }

    public override void OnEpisodeBegin()
    {
        _windowCountThisEpisode = 0;

        // безопасный дефолт перед первым окном
        _lastHitRate = 1f;
        _lastReactionSec = 0f;
        _lastThroughput01 = 0f;

        if (perf != null) perf.ResetWindow();
        if (difficulty != null) difficulty.ResetToDefault();

        SyncCurrents();

        // Первое решение (чтобы агент мог выставить стартовые параметры)
        RequestDecision();
    }

    /// <summary>
    /// 1) Observations (Space Size = 8):
    ///  [0] SR (hitRate окна 10),
    ///  [1] RT (avgReactionSec норм.),
    ///  [2] ROM (если perf есть, иначе 0),
    ///  [3..6] состояние сложности (spawnInterval, speed, targetRadius, spawnRadius) нормированное,
    ///  [7] ошибка (SR - targetSR).
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        float sr = Mathf.Clamp01(_lastHitRate);
        float rt01 = Mathf.Clamp01(_lastReactionSec / Mathf.Max(0.1f, maxReactionSec));
        float rom01 = perf != null ? Mathf.Clamp01(perf.MeanRom01) : 0f;

        var st = difficulty != null ? difficulty.GetState01() : DifficultyController.State01.Zero;

        sensor.AddObservation(sr);                 // 1
        sensor.AddObservation(rt01);               // 2
        sensor.AddObservation(rom01);              // 3
        sensor.AddObservation(st.spawnInterval01); // 4
        sensor.AddObservation(st.ballSpeed01);     // 5
        sensor.AddObservation(st.targetRadius01);  // 6
        sensor.AddObservation(st.spawnRadius01);   // 7
        sensor.AddObservation(sr - targetSR);      // 8
    }

    /// <summary>
    /// 2) Actions (Continuous, Size = 5):
    ///  a0: dSpawnInterval (-1..1)
    ///  a1: dBallSpeed     (-1..1)
    ///  a2: dTargetRadius  (-1..1)
    ///  a3: dSpawnRadius   (-1..1)
    ///  a4: dSpawnBias     (-1..1)
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions)
    {
        var a = actions.ContinuousActions;

        float a0 = Mathf.Clamp(a[0], -1f, 1f);
        float a1 = Mathf.Clamp(a[1], -1f, 1f);
        float a2 = Mathf.Clamp(a[2], -1f, 1f);
        float a3 = Mathf.Clamp(a[3], -1f, 1f);
        float a4 = Mathf.Clamp(a[4], -1f, 1f);

        float dSpawn = a0 * dSpawnIntervalMax;
        float dSpeed = a1 * dBallSpeedMax;
        float dTR    = a2 * dTargetRadiusMax;
        float dSR    = a3 * dSpawnRadiusMax;

        // Применяем изменения сложности
        if (difficulty != null)
        {
            difficulty.ApplyDeltas(dSpawn, dSpeed, dTR, dSR);
        }

        // Смещение зоны спавна (Bias)
        if (spawner != null)
        {
            spawner.aiSpawnBias = Mathf.Clamp(spawner.aiSpawnBias + a4 * dSpawnBiasMax, -1f, 1f);
        }

        // Важно: если spawnInterval изменился, нужно пересоздать InvokeRepeating,
        // иначе частота спавна может не обновиться прямо во время игры.
        RescheduleSpawnerIfNeeded();

        SyncCurrents();

        if (visualizer != null && difficulty != null && spawner != null)
        {
            // Получаем текущий процент успеха (0.0 - 1.0)
            float currentSR = perf != null ? perf.SuccessRate01 : 0f;
            
            visualizer.UpdateDashboard(
                difficulty.BallSpeed,
                difficulty.SpawnInterval,
                spawner.aiSpawnBias,
                GetCumulativeReward(),
                dSpeed,
                dSpawn
            );
        }
    }

    /// <summary>
    /// 3) Reward: считаем на конце окна (10 мячей), после того как среда ответила на предыдущие действия.
    /// </summary>
    private void OnWindowFinished(WindowMetrics m)
    {
        // Обновляем последние метрики (для Observations)
        _lastHitRate = Mathf.Clamp01(m.hitRate);
        _lastReactionSec = Mathf.Max(0f, (m.avgReactionMs * 0.001f));
        _lastThroughput01 = Mathf.Clamp01(m.throughputPerMin / Mathf.Max(1f, maxThroughputPerMin));

        // --- Reward shaping (зона потока) ---
        float sr = _lastHitRate;
        float err = Mathf.Abs(sr - targetSR);

        // Бонус если в зоне потока
        if (err <= tolerance)
        {
            AddReward(+0.20f);
        }
        else
        {
            // Штраф пропорционален отклонению
            AddReward(-err);
        }

        // Мягко штрафуем слишком долгую реакцию (если она есть)
        float rt01 = Mathf.Clamp01(_lastReactionSec / Mathf.Max(0.1f, maxReactionSec));
        AddReward(-0.10f * rt01);

        // Небольшой бонус за стабильный темп (чтобы не пытался "заморозить" игру)
        AddReward(+0.05f * _lastThroughput01);

        // Штраф за резкие изменения сложности (стабилизирует политику)
        if (difficulty != null)
            AddReward(-0.05f * difficulty.LastRoundChangeMagnitude01);

        // Следующее решение
        _windowCountThisEpisode++;
        if (_windowCountThisEpisode >= windowsPerEpisode)
        {
            EndEpisode();
            return;
        }

        RequestDecision();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Тест руками: стрелки влево/вправо влияют на сложность
        var ca = actionsOut.ContinuousActions;
        ca[0] = 0f; // spawn interval
        ca[1] = 0f; // speed
        ca[2] = 0f;
        ca[3] = 0f;
        ca[4] = 0f;

        float x = Input.GetAxis("Horizontal");
        // вправо -> усложнить (скорость ↑, интервал ↓), влево -> упростить
        ca[1] = x;
        ca[0] = -0.4f * x;
    }

    private void SyncCurrents()
    {
        if (difficulty == null) return;
        CurrentBallSpeed = difficulty.BallSpeed;
        CurrentSpawnInterval = difficulty.SpawnInterval;
    }

    // Добавь переменную в класс CoachAgent
private float _lastRescheduleTime;

    private void RescheduleSpawnerIfNeeded()
    {
        if (spawner == null) return;

        // 1. Получаем текущий интервал из DifficultyController
        // (предполагаем, что difficulty уже обновил свои внутренние поля в OnActionReceived)
        float newInterval = difficulty.SpawnInterval; 
        
        // 2. Проверяем, реально ли нужно менять (защита от микро-колебаний float)
        // Если изменение меньше 50 мс, игнорируем
        if (Mathf.Abs(spawner.spawnInterval - newInterval) < 0.05f) return;

        // 3. Anti-Burst защита: не даем менять настройки спавнера чаще чем раз в 1 сек
        // Это предотвратит очередь шаров, даже если DecisionRequester сходит с ума
        if (Time.time - _lastRescheduleTime < 1.0f) return;

        _lastRescheduleTime = Time.time;

        // 4. Применяем
        // ВАЖНО: Мы не вызываем Stop/Start здесь напрямую, чтобы не сбивать таймер InvokeRepeating,
        // если BallSpawner поддерживает горячую замену.
        // Но так как он на InvokeRepeating, нам придется перезапустить, но АККУРАТНО.
        
        // Передаем команду в спавнер (см. Шаг Б)
        spawner.UpdateIntervalSafely(newInterval);
    }
}
