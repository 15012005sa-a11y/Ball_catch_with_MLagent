using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

/// <summary>
/// CoachAgent: агент, который регулирует сложность (spawnInterval, ballSpeed, targetRadius, spawnRadius)
/// на основе метрик пациента (SR/RT/ROM).
/// 
/// Обновление: передаём SuccessRate в CoachVisualizer.UpdateDashboard(...)
/// (после вашего Шага 1, где добавлен successRateText и изменена сигнатура UpdateDashboard).
/// </summary>
public class CoachAgent : Agent
{
    [Header("Links")]
    public DifficultyController difficulty;   // Обёртка над LevelDirector/BallSpawner
    public PerformanceWindow perf;            // Окно метрик (SR, RT, ROM)
    public BallSpawnerBallCatch spawner;      // Спавнер (для aiSpawnBias)

    [SerializeField] private CoachVisualizer visualizer;

    [Header("Targets")]
    [Range(0.5f, 0.9f)] public float targetSR = 0.7f; // целевая успешность

    [Header("Norm ranges")]
    [Tooltip("Максимум для нормировки времени реакции (сек)")]
    public float maxReactionSec = 2.0f;

    [Header("Action scales per round")]
    [Tooltip("Изменение интервала спавна за мини-раунд (сек)")]
    public float dSpawnIntervalMax = 0.15f;
    [Tooltip("Изменение скорости шара за мини-раунд (м/с)")]
    public float dBallSpeedMax = 0.50f;
    [Tooltip("Изменение радиуса цели за мини-раунд (м)")]
    public float dTargetRadiusMax = 0.03f;
    [Tooltip("Изменение радиуса области спавна за мини-раунд (м)")]
    public float dSpawnRadiusMax = 0.10f;
    [Tooltip("Изменение смещения точки спавна за мини-раунд (в долях диапазона -1..1)")]
    public float dSpawnBiasMax = 0.30f;

    [Header("Decision cadence")]
    [Tooltip("Сколько попыток в мини-раунде до следующего решения")]
    public int decisionsEveryNResults = 8;  // длина мини-раунда

    [Header("Episode")]
    [Tooltip("Сколько мини-раундов (окон) в одном эпизоде обучения")]
    public int windowsPerEpisode = 30;

    // Для отладки/внешних скриптов (если нужно)
    public float CurrentBallSpeed = 5f;
    public float CurrentSpawnRate = 2f;

    private int _epWindowCount = 0;
    private int _sinceLastDecision = 0;

    // === Lifecycle ===
    protected override void OnEnable()
    {
        base.OnEnable();
        if (perf != null) perf.OnResult += OnResult; // success, reactionSec, rom01
    }

    protected override void OnDisable()
    {
        if (perf != null) perf.OnResult -= OnResult;
        base.OnDisable();
    }

    public override void OnEpisodeBegin()
    {
        _sinceLastDecision = 0;
        _epWindowCount = 0;

        if (perf != null) perf.ResetWindow();
        if (difficulty != null) difficulty.ResetToDefault();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float sr = perf ? perf.SuccessRate01 : 0f; // 0..1
        float rt = perf ? Mathf.Clamp01(perf.MeanReactionSec / Mathf.Max(0.1f, maxReactionSec)) : 0f; // 0..1
        float rom = perf ? Mathf.Clamp01(perf.MeanRom01) : 0f; // 0..1

        var st = difficulty ? difficulty.GetState01() : DifficultyController.State01.Zero;

        // Итог: 8 наблюдений (Behavior Parameters → Space Size = 8)
        sensor.AddObservation(sr);                  // 1
        sensor.AddObservation(rt);                  // 2
        sensor.AddObservation(rom);                 // 3
        sensor.AddObservation(st.spawnInterval01);  // 4
        sensor.AddObservation(st.ballSpeed01);      // 5
        sensor.AddObservation(st.targetRadius01);   // 6
        sensor.AddObservation(st.spawnRadius01);    // 7
        sensor.AddObservation(st.reserve01);        // 8 (зарезервировано)
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var a = actions.ContinuousActions;

        // Нормированные действия (-1..1)
        float a0 = Mathf.Clamp(a[0], -1f, 1f); // dSpawn (Интервал)
        float a1 = Mathf.Clamp(a[1], -1f, 1f); // dSpeed (Скорость)
        float a2 = Mathf.Clamp(a[2], -1f, 1f); // dTargetR
        float a3 = Mathf.Clamp(a[3], -1f, 1f); // dSpawnR
        float a4 = Mathf.Clamp(a[4], -1f, 1f); // dSpawnBias

        // 1) Применяем изменения сложности (масштабируем нормированные действия)
        if (difficulty != null)
        {
            difficulty.ApplyDeltas(
                a0 * dSpawnIntervalMax,
                a1 * dBallSpeedMax,
                a2 * dTargetRadiusMax,
                a3 * dSpawnRadiusMax
            );

            // (опционально) обновим для внешних потребителей
            CurrentBallSpeed = difficulty.BallSpeed;
            CurrentSpawnRate = difficulty.SpawnInterval;
        }

        // 2) Применяем смещение (Bias) в спавнере
        if (spawner != null)
        {
            spawner.aiSpawnBias = Mathf.Clamp(
                spawner.aiSpawnBias + a4 * dSpawnBiasMax,
                -1f, 1f
            );
        }

#if UNITY_EDITOR
        if ((Time.frameCount & 31) == 0)
            Debug.Log($"[AI] act: dSpawn={a0:F3}, dSpeed={a1:F3}, dTargetR={a2:F3}, dSpawnR={a3:F3}, bias={a4:F3}");
#endif

        // 3) ОБНОВЛЕНИЕ UI: добавили SuccessRate (Шаг 2)
        if (visualizer != null && difficulty != null && spawner != null)
        {
            float currentSR = perf != null ? perf.SuccessRate01 : 0f; // 0..1

            visualizer.UpdateDashboard(
                difficulty.BallSpeed,        // speed
                difficulty.SpawnInterval,    // interval
                spawner.aiSpawnBias,         // bias
                GetCumulativeReward(),       // reward
                currentSR,                   // <-- SuccessRate (0..1)
                a1,                          // speedDelta (нормированное действие -1..1)
                a0                           // intervalDelta (нормированное действие -1..1)
            );
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;

        // Инициализируем нулями
        ca[0] = 0; // Spawn Interval Delta
        ca[1] = 0; // Ball Speed Delta
        ca[2] = 0; // Target Radius Delta
        ca[3] = 0; // Spawn Radius Delta
        ca[4] = 0; // Spawn Bias

        if (perf == null) return;

        // Эвристика: имитация поведения тренера
        float currentSR = perf.SuccessRate01;
        float threshold = 0.1f;

        // Если игрок играет слишком хорошо → усложняем
        if (currentSR > targetSR + threshold)
        {
            ca[1] = 0.5f;   // ускорить
            ca[0] = -0.2f;  // уменьшить интервал (чаще)
        }
        // Если игрок играет плохо → упрощаем
        else if (currentSR < targetSR - threshold)
        {
            ca[1] = -0.5f;  // замедлить
            ca[0] = 0.2f;   // увеличить интервал (реже)
        }
    }

    // === Reward/Decision cadence ===
    private void OnResult(bool success, float reactionSec, float rom01)
    {
        _sinceLastDecision++;
        if (_sinceLastDecision < decisionsEveryNResults) return;
        _sinceLastDecision = 0;

        float sr = perf.SuccessRate01; // 0..1
        float rt = Mathf.Clamp01(perf.MeanReactionSec / Mathf.Max(0.1f, maxReactionSec));
        float rom = Mathf.Clamp01(perf.MeanRom01);

        float err = (sr - targetSR);
        AddReward(1f - (err * err) / (targetSR * targetSR + 1e-6f)); // удерживаем SR возле targetSR
        AddReward(+0.30f * rom);                                      // поощрение за ROM
        AddReward(-0.20f * rt);                                       // штраф за долгие реакции

        if (difficulty != null)
            AddReward(-0.05f * difficulty.LastRoundChangeMagnitude01); // штраф за резкие скачки сложности

        _epWindowCount++;
        if (_epWindowCount >= windowsPerEpisode)
        {
            EndEpisode();
            return;
        }

        Debug.Log($"[AI] result received: success={success}");
    }
}
