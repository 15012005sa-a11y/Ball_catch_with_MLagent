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

    [Header("SR band (goal)")]
    public float targetLow = 0.75f;
    public float targetHigh = 0.80f;
    public float hysteresis = 0.005f;      // анти-пиление
    [Range(0.05f, 0.5f)] public float emaAlpha = 0.25f;

    [Header("Activity")]
    public PlayerSimulatorLite simLite;     // OPTIONAL: только для Simulator
    [Range(0f, 1f)] public float rlWeight = 0.35f;   // 0 = чистый safety, 1 = чистый RL
    public bool debugCoach = true;

    [Header("Decision driver (runtime)")]
    [SerializeField] private bool driveDecisionsByTimer = true;
    [SerializeField, Range(0.1f, 2f)] private float decisionPeriodSec = 0.5f; // 2 раза/сек
    private float _nextDecisionTime = 0f;


    // EMA состояния
    private float _srEma, _rtEma, _romEma, _actEma;
    private bool _pendingApply = false;
    private float _lastDecisionTime;

    // === UI refresh (чтобы значения всегда были видны) ===
    [SerializeField, Range(0.05f, 1f)] private float uiRefreshPeriod = 0.25f;
    private float _uiNextTime = 0f;
    private float _lastA0 = 0f; // интервал action (-1..1)
    private float _lastA1 = 0f; // speed action (-1..1)

    private void Start()
    {
        // Показать хотя бы стартовые значения
        PushUI(_lastA1, _lastA0);
    }

    private void Update()
    {
        if (Time.unscaledTime < _uiNextTime) return;
        _uiNextTime = Time.unscaledTime + uiRefreshPeriod;

        PushUI(_lastA1, _lastA0);
    }

    private void TryAutoWire()
    {
        if (difficulty == null) difficulty = FindObjectOfType<DifficultyController>(true);
        if (spawner == null)    spawner    = FindObjectOfType<BallSpawnerBallCatch>(true);
        if (perf == null)       perf       = FindObjectOfType<PerformanceWindow>(true);

        if (visualizer == null)
        {
            var all = FindObjectsOfType<CoachVisualizer>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].gameObject.activeInHierarchy)
                {
                    visualizer = all[i];
                    break;
                }
            }
            if (visualizer == null && all.Length > 0) visualizer = all[0];
        }
    }

    private void FixedUpdate()
    {
        if (!driveDecisionsByTimer) return;
        if (Time.time < _nextDecisionTime) return;
        _nextDecisionTime = Time.time + decisionPeriodSec;

        _pendingApply = true;     // разрешаем применить ровно 1 раз
        RequestDecision();        // просим действие у модели/эвристики

        if (debugCoach) Debug.Log("[COACH] RequestDecision (timer)");
    }

    protected override void Awake()
    {
        base.Awake();     // важно для ML-Agents Agent
        TryAutoWire();    // твоё
    }

    private void PushUI(float speedDelta01, float intervalDelta01)
    {
        // если ссылки потерялись/не назначены — попробуем подцепить снова
        if (visualizer == null || difficulty == null || spawner == null)
            TryAutoWire();

        if (visualizer == null || difficulty == null || spawner == null)
        {
            Debug.Log($"[UI] MISSING refs: vis={(visualizer!=null)} diff={(difficulty!=null)} sp={(spawner!=null)} perf={(perf!=null)}");
            return;
        }

        float sr01 = (perf != null) ? perf.SuccessRate01 : _srEma;

        visualizer.UpdateDashboard(
            difficulty.BallSpeed,
            difficulty.SpawnInterval,
            spawner.aiSpawnBias,
            GetCumulativeReward(),
            sr01,
            speedDelta01,
            intervalDelta01
        );

        // этот лог теперь реально будет виден
        Debug.Log($"[UI] OK speed={difficulty.BallSpeed:F2} int={difficulty.SpawnInterval:F2} sr={sr01:F2}");
    }

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
        float activity01 = 0f;
        if (simLite != null) activity01 = simLite.GetActivity01();
        else
        {
            // прокси-активность, если нет handSpeed
        activity01 = Mathf.Clamp01(0.45f * rom + 0.35f * (1f - rt) + 0.20f * sr);
        }

        var st = difficulty ? difficulty.GetState01() : DifficultyController.State01.Zero;

        sensor.AddObservation(sr);                  // 1
        sensor.AddObservation(rt);                  // 2
        sensor.AddObservation(rom);                 // 3
        sensor.AddObservation(st.spawnInterval01);  // 4
        sensor.AddObservation(st.ballSpeed01);      // 5
        sensor.AddObservation(st.targetRadius01);   // 6
        sensor.AddObservation(st.spawnRadius01);    // 7
        sensor.AddObservation(activity01); // 8 ()
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (debugCoach && (Time.frameCount & 63) == 0)
        Debug.Log($"[COACH] OnActionReceived called pending={_pendingApply}");

        if (!_pendingApply) return;
        _pendingApply = false;

        var a = actions.ContinuousActions;
        if (a.Length < 5) return;

        float a0 = Mathf.Clamp(a[0], -1f, 1f); // dSpawn (интервал)
        float a1 = Mathf.Clamp(a[1], -1f, 1f); // dSpeed (скорость)
        float a2 = Mathf.Clamp(a[2], -1f, 1f); // dTargetR
        float a3 = Mathf.Clamp(a[3], -1f, 1f); // dSpawnR
        float a4 = Mathf.Clamp(a[4], -1f, 1f); // dSpawnBias

        // запоминаем последнее действие (для UI)
        _lastA0 = a0;   // interval action (-1..1)
        _lastA1 = a1;   // speed action (-1..1)

        // --- RL-дельты ---
        float dSpawnRL = a0 * dSpawnIntervalMax;
        float dSpeedRL = a1 * dBallSpeedMax;
        float dTR_RL   = a2 * dTargetRadiusMax;
        float dSR_RL   = a3 * dSpawnRadiusMax;

        // --- Safety-дельты (держим SR в коридоре) ---
        float lo = targetLow, hi = targetHigh;
        float srAim = Mathf.Lerp(hi, lo, _actEma);   // активный => целимся ближе к lo
        float sr = _srEma;

        float err = 0f;
        if (sr > hi + hysteresis) err = sr - hi;
        else if (sr < lo - hysteresis) err = sr - lo;
        else err = (sr - srAim) * 0.25f; // мягкая подстройка внутри коридора

        float bandHalf = 0.5f * (hi - lo); // 0.025 при 0.75..0.80
        float eN = Mathf.Clamp(err / Mathf.Max(1e-5f, bandHalf), -1f, 1f);

        // eN>0 => слишком легко => усложнить (speed↑, interval↓, targetR↓)
        float dSpeedSafe =  +eN * (0.60f * dBallSpeedMax);
        float dSpawnSafe =  -eN * (0.60f * dSpawnIntervalMax);
        float dTR_Safe   =  -eN * (0.40f * dTargetRadiusMax);
        float dSR_Safe   =  +eN * (0.40f * dSpawnRadiusMax);

        // --- Смешивание Safety и RL ---
        float dSpawn = Mathf.Lerp(dSpawnSafe, dSpawnRL, rlWeight);
        float dSpeed = Mathf.Lerp(dSpeedSafe, dSpeedRL, rlWeight);
        float dTR    = Mathf.Lerp(dTR_Safe,   dTR_RL,   rlWeight);
        float dSR    = Mathf.Lerp(dSR_Safe,   dSR_RL,   rlWeight);

        // 1) Применяем ИМЕННО ГИБРИДНЫЕ дельты (ОДИН раз)
        if (difficulty != null)
        {
            float preSpeed = difficulty.BallSpeed;
            float preInt   = difficulty.SpawnInterval;

            difficulty.ApplyDeltas(dSpawn, dSpeed, dTR, dSR);

            CurrentBallSpeed = difficulty.BallSpeed;
            CurrentSpawnRate = difficulty.SpawnInterval;

            if (debugCoach)
            {
                Debug.Log(
                    $"[COACH] apply: SRema={_srEma:F3} aim={srAim:F3} eN={eN:F2} rlW={rlWeight:F2} | " +
                    $"Δspeed={dSpeed:F3} Δint={dSpawn:F3} | " +
                    $"speed {preSpeed:F2}->{difficulty.BallSpeed:F2}, int {preInt:F2}->{difficulty.SpawnInterval:F2}"
                );
            }
        }

        // 2) Bias можно применять отдельно (не мешает safety)
        if (spawner != null)
        {
            spawner.aiSpawnBias = Mathf.Clamp(
                spawner.aiSpawnBias + a4 * dSpawnBiasMax,
                -1f, 1f
            );
        }

    #if UNITY_EDITOR
        if ((Time.frameCount & 31) == 0)
            Debug.Log($"[AI] act raw: a0={a0:F2} a1={a1:F2} a2={a2:F2} a3={a3:F2} a4={a4:F2}");
    #endif

        // 3) UI — лучше показывать реально применённые дельты (а не raw a0/a1)
        PushUI(_lastA1, _lastA0);

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

        float sr = perf ? perf.SuccessRate01 : 0f;
        float rt = perf ? Mathf.Clamp01(perf.MeanReactionSec / Mathf.Max(0.1f, maxReactionSec)) : 0f;
        float rom = perf ? Mathf.Clamp01(perf.MeanRom01) : 0f;

        float act = 0f;
        if (simLite != null) act = simLite.GetActivity01();
        else act = Mathf.Clamp01(0.45f * rom + 0.35f * (1f - rt) + 0.20f * sr);

        // EMA сглаживание по окнам
        _srEma = Mathf.Lerp(_srEma, sr, emaAlpha);
        _rtEma = Mathf.Lerp(_rtEma, rt, emaAlpha);
        _romEma = Mathf.Lerp(_romEma, rom, emaAlpha);
        _actEma = Mathf.Lerp(_actEma, act, emaAlpha);

        // ===== Training reward (только когда подключён тренер) =====
        bool isTraining = Academy.Instance.IsCommunicatorOn;

        if (isTraining)
        {
            // смещаем цель внутри коридора по активности:
            // активный -> 0.75, пассивный -> 0.80
            float srAim = Mathf.Lerp(targetHigh, targetLow, _actEma);
            float bandHalf = 0.5f * (targetHigh - targetLow); // 0.025
            float e = Mathf.Abs(_srEma - srAim);

            // 1) основной reward: максимум около srAim, отрицательный вне коридора
            float rBand;
            if (_srEma >= targetLow && _srEma <= targetHigh)
                rBand = 1f - Mathf.Clamp01(e / Mathf.Max(1e-5f, bandHalf)); // 1..0
            else
                rBand = -Mathf.Clamp01(e / 0.10f); // штраф

            AddReward(rBand);

            // 2) поощряем БОЛЬШУЮ сложность, но только если SR внутри коридора
            if (difficulty != null && _srEma >= targetLow && _srEma <= targetHigh)
            {
                var st = difficulty.GetState01();
                float frequent01 = 1f - st.spawnInterval01;
                float diff01 = Mathf.Clamp01(0.6f * st.ballSpeed01 + 0.4f * frequent01);
                AddReward(0.20f * diff01);
            }

            // 3) анти-скачки
            if (difficulty != null)
                AddReward(-0.05f * difficulty.LastRoundChangeMagnitude01);
        }

        // ===== просим новое решение и применяем его ОДИН раз =====
        _pendingApply = true;
        _lastDecisionTime = Time.time;
        RequestDecision();

        // ===== episode только в training =====
        if (isTraining)
        {
            _epWindowCount++;
            if (_epWindowCount >= windowsPerEpisode)
            {
                EndEpisode();
                return;
            }
        }

        if (debugCoach)
            Debug.Log($"[COACH] window SR={_srEma:F3} RT={_rtEma:F3} ROM={_romEma:F3} ACT={_actEma:F3}");
    }

}
