using UnityEngine;

/// <summary>
/// DifficultyController: единая точка управления сложностью Ball Catch.
///
/// Управляет параметрами:
///  - Spawn Interval (интервал спавна)
///  - Ball Speed (скорость мяча)
///  - Target Radius (радиус цели)
///  - Spawn Radius (радиус области появления)
///
/// Дополнено по ТЗ:
///  - AdjustSpeed(float delta): меняет currentSpeed в диапазоне [-0.1..0.1],
///    ограничивает между minSpeed и maxSpeed и применяет к спавнеру.
///  - GetNormalizedSpeed(): возвращает скорость 0..1 для сенсоров нейросети.
/// </summary>
[DefaultExecutionOrder(-100)]
public class DifficultyController : MonoBehaviour
{
    // ===== Links =====
    [Header("Game refs")]
    [SerializeField] private BallSpawnerBallCatch spawner; // назначь в Inspector

    // ===== Speed control (requested API) =====
    [Header("Speed control")]
    [SerializeField] private float minSpeed = 0.20f;
    [SerializeField] private float maxSpeed = 5.00f;
    [SerializeField] private float currentSpeed = 2.00f;

    // ===== Other difficulty clamps =====
    [Header("Hard clamps (game units)")]
    [SerializeField] private Vector2 spawnIntervalSecRange = new(0.50f, 3.00f);
    [SerializeField] private Vector2 targetRadiusRange = new(0.05f, 0.20f);
    [SerializeField] private Vector2 spawnRadiusRange = new(0.20f, 1.50f);

    // ===== Smoothing =====
    [Header("Smoothing")]
    [Range(0f, 1f)] public float lerpRate = 1f;

    // ===== Public readouts (used by CoachAgent/UI) =====
    [Header("Readouts")]
    public float BallSpeed = 2f;       // фактическая скорость (игровые единицы)
    public float SpawnInterval = 2f;   // фактический интервал (сек)

    // ===== Internal state =====
    private float _spawnInterval;
    private float _ballSpeed;
    private float _targetRadius;
    private float _spawnRadius;

    /// <summary>Нормированная магнитуда изменения сложности (для штрафа в reward).</summary>
    public float LastRoundChangeMagnitude01 { get; private set; }

    // ===== Normalized state for ML =====
    public struct State01
    {
        public float spawnInterval01, ballSpeed01, targetRadius01, spawnRadius01, reserve01;
        public static State01 Zero => new State01
        {
            spawnInterval01 = 0f,
            ballSpeed01 = 0f,
            targetRadius01 = 0f,
            spawnRadius01 = 0f,
            reserve01 = 0f
        };
    }

    private void Awake()
    {
        // Инициализация внутреннего состояния из Inspector
        _spawnInterval = Mathf.Lerp(spawnIntervalSecRange.x, spawnIntervalSecRange.y, 0.60f);
        _ballSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
        _targetRadius = Mathf.Lerp(targetRadiusRange.x, targetRadiusRange.y, 0.50f);
        _spawnRadius = Mathf.Lerp(spawnRadiusRange.x, spawnRadiusRange.y, 0.50f);

        SyncReadoutsFromInternal();
        ApplyToGame();
    }

    private void OnValidate()
    {
        // --- basic range fixes ---
        if (maxSpeed < minSpeed) maxSpeed = minSpeed + 0.01f;
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        if (spawnIntervalSecRange.y < spawnIntervalSecRange.x) spawnIntervalSecRange.y = spawnIntervalSecRange.x + 0.01f;
        if (targetRadiusRange.y < targetRadiusRange.x) targetRadiusRange.y = targetRadiusRange.x + 0.001f;
        if (spawnRadiusRange.y < spawnRadiusRange.x) spawnRadiusRange.y = spawnRadiusRange.x + 0.001f;

        // clamp internal if we are in play mode / hot reload
        _spawnInterval = Mathf.Clamp(_spawnInterval, spawnIntervalSecRange.x, spawnIntervalSecRange.y);
        _ballSpeed = Mathf.Clamp(_ballSpeed, minSpeed, maxSpeed);
        _targetRadius = Mathf.Clamp(_targetRadius, targetRadiusRange.x, targetRadiusRange.y);
        _spawnRadius = Mathf.Clamp(_spawnRadius, spawnRadiusRange.x, spawnRadiusRange.y);

        SyncReadoutsFromInternal();
        ApplyToGame();
    }

    /// <summary>
    /// Сбрасывает параметры сложности к дефолтным (внутренние mid-values).
    /// </summary>
    public void ResetToDefault()
    {
        _spawnInterval = Mathf.Lerp(spawnIntervalSecRange.x, spawnIntervalSecRange.y, 0.60f);
        _ballSpeed = Mathf.Lerp(minSpeed, maxSpeed, 0.40f);
        _targetRadius = Mathf.Lerp(targetRadiusRange.x, targetRadiusRange.y, 0.50f);
        _spawnRadius = Mathf.Lerp(spawnRadiusRange.x, spawnRadiusRange.y, 0.50f);
        LastRoundChangeMagnitude01 = 0f;

        // синхронизируем Inspector-friendly переменную скорости
        currentSpeed = _ballSpeed;

        SyncReadoutsFromInternal();
        ApplyToGame();
    }

    /// <summary>
    /// Возвращает нормированные параметры сложности в диапазоне [0..1] для наблюдений агента.
    /// </summary>
    public State01 GetState01()
    {
        return new State01
        {
            spawnInterval01 = Mathf.InverseLerp(spawnIntervalSecRange.x, spawnIntervalSecRange.y, _spawnInterval),
            ballSpeed01 = GetNormalizedSpeed(),
            targetRadius01 = Mathf.InverseLerp(targetRadiusRange.x, targetRadiusRange.y, _targetRadius),
            spawnRadius01 = Mathf.InverseLerp(spawnRadiusRange.x, spawnRadiusRange.y, _spawnRadius),
            reserve01 = 0f
        };
    }

    /// <summary>
    /// Применяет дельты сложности (обычно от CoachAgent).
    /// dSpawn  : + увеличивает интервал (легче), - уменьшает (сложнее)
    /// dSpeed  : + увеличивает скорость (сложнее), - уменьшает (легче)
    /// dRadius : + увеличивает радиус цели (легче)
    /// dSpawnRad: + расширяет область спавна (обычно сложнее)
    /// </summary>
    public void ApplyDeltas(float dSpawn, float dSpeed, float dRadius, float dSpawnRad)
    {
        float prevS = _spawnInterval;
        float prevV = _ballSpeed;
        float prevR = _targetRadius;
        float prevSR = _spawnRadius;

        // 1) Clamp
        float nextSpawn = Mathf.Clamp(_spawnInterval + dSpawn, spawnIntervalSecRange.x, spawnIntervalSecRange.y);
        float nextSpeed = Mathf.Clamp(_ballSpeed + dSpeed, minSpeed, maxSpeed);
        float nextTR = Mathf.Clamp(_targetRadius + dRadius, targetRadiusRange.x, targetRadiusRange.y);
        float nextSpawnR = Mathf.Clamp(_spawnRadius + dSpawnRad, spawnRadiusRange.x, spawnRadiusRange.y);

        // 2) Smooth
        _spawnInterval = Mathf.Lerp(prevS, nextSpawn, lerpRate);
        _ballSpeed = Mathf.Lerp(prevV, nextSpeed, lerpRate);
        _targetRadius = Mathf.Lerp(prevR, nextTR, lerpRate);
        _spawnRadius = Mathf.Lerp(prevSR, nextSpawnR, lerpRate);

        // 3) Sync readouts + inspector speed
        currentSpeed = _ballSpeed;
        SyncReadoutsFromInternal();

        // 4) Magnitude for reward penalty
        var st = GetState01();
        float ps = Mathf.InverseLerp(spawnIntervalSecRange.x, spawnIntervalSecRange.y, prevS);
        float pv = Mathf.InverseLerp(minSpeed, maxSpeed, prevV);
        float pr = Mathf.InverseLerp(targetRadiusRange.x, targetRadiusRange.y, prevR);
        float psr = Mathf.InverseLerp(spawnRadiusRange.x, spawnRadiusRange.y, prevSR);
        LastRoundChangeMagnitude01 = Mathf.Sqrt(
            (st.spawnInterval01 - ps) * (st.spawnInterval01 - ps) +
            (st.ballSpeed01 - pv) * (st.ballSpeed01 - pv) +
            (st.targetRadius01 - pr) * (st.targetRadius01 - pr) +
            (st.spawnRadius01 - psr) * (st.spawnRadius01 - psr)
        );

        ApplyToGame();

#if UNITY_EDITOR
        Debug.Log($"[DIFF] speed={_ballSpeed:F2}, spawnInt={_spawnInterval:F2}, targetR={_targetRadius:F2}, spawnR={_spawnRadius:F2}");
        Debug.Log($"[DIFF] speed={_ballSpeed:F2} min={minSpeed:F2} max={maxSpeed:F2}");

#endif
    }

    // ==========================
    // ===== Requested API ======
    // ==========================

    /// <summary>
    /// Меняет текущую скорость на дельту [-0.1..0.1] (игровые единицы),
    /// ограничивает между minSpeed и maxSpeed и применяет к спавнеру.
    /// </summary>
    public void AdjustSpeed(float delta)
    {
        // Гарантируем диапазон входа (по ТЗ)
        delta = Mathf.Clamp(delta, -0.1f, 0.1f);

        currentSpeed += delta;
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        // Синхронизируем внутреннее состояние
        _ballSpeed = currentSpeed;
        SyncReadoutsFromInternal();

        // Применяем к игре
        ApplyToGame();
    }

    /// <summary>
    /// Возвращает скорость 0..1 для наблюдений нейросети.
    /// </summary>
    public float GetNormalizedSpeed()
    {
        return Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);
    }

    // ==========================
    // ===== Helpers ============
    // ==========================

    private void SyncReadoutsFromInternal()
    {
        BallSpeed = _ballSpeed;
        SpawnInterval = _spawnInterval;
    }

    /// <summary>
    /// Применяет текущие параметры к объектам игры.
    /// Важно: скорость применяется сразу через spawner.ballSpeed.
    /// </summary>
    private void ApplyToGame()
    {
        if (spawner == null)
            spawner = FindObjectOfType<BallSpawnerBallCatch>(true);

        if (spawner == null) return;

        spawner.externalDifficultyOverride = true;
        spawner.selfAdaptive = false; // на ML лучше выключить

        // 0) держим клампы скорости в одном месте (иначе спавнер отрежет)
        spawner.ballSpeedClamp = new Vector2(minSpeed, maxSpeed);

        // 1) скорость
        spawner.ballSpeed = Mathf.Max(0.05f, _ballSpeed);

        // 2) интервал: ВАЖНО — нужно рескейджулить InvokeRepeating
        float newInterval = Mathf.Max(0.05f, _spawnInterval);
        spawner.UpdateIntervalSafely(newInterval);

        // 3) радиус спавна (spawner ожидает 0..1)
        float normRadius = Mathf.InverseLerp(spawnRadiusRange.x, spawnRadiusRange.y, _spawnRadius);
        spawner.SetSpawnRadius(normRadius);

        // 4) если хочешь полностью убрать локальную авто-адаптацию в ML-режиме:
        // spawner.selfAdaptive = false;
    }

}
