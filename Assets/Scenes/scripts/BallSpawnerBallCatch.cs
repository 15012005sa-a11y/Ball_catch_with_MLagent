// ===============================
// FILE: BallSpawnerBallCatch.cs
// ===============================
// ВАЖНО: это полная версия файла. Просто замените содержимое вашего BallSpawnerBallCatch.cs этим кодом.

using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BallSpawnerBallCatch : MonoBehaviour
{
    [Header("Настройки спавна")]
    public GameObject ballPrefab;
    public Transform[] spawnPoints;
    public Transform playerTransform;

    // Реализация методов управления для агента/контроллера
    [Header("External Difficulty Override (CoachAgent)")]
    public bool externalDifficultyOverride = false;

    [Header("Debug")]
    public bool debugSpawner = false;

    public void SetBallSpeed(float v)
    {
        ballSpeed = Mathf.Clamp(v, ballSpeedClamp.x, ballSpeedClamp.y);
    }

    public void SetSpawnInterval(float v)
    {
        spawnInterval = Mathf.Clamp(v, 0.05f, 10f);
        UpdateIntervalSafely(spawnInterval);
    }

    // 0..1 (0 = одна точка, 1 = все точки)
    public void SetSpawnRadius(float normalizedRadius)
    {
        currentSpawnSpread = Mathf.Clamp01(normalizedRadius);
    }

    public void SetTargetRadius(float v)
    {
        // если появится поле targetRadius — присвоить его здесь
    }

    [Header("Параметры движения (текущие)")]
    [Tooltip("Интервал между появлениями шаров, сек")]
    public float spawnInterval = 1.5f;

    [Header("AI-управление спавном")]
    [Range(-1f, 1f)]
    public float aiSpawnBias = 0f;

    [Tooltip("Текущая скорость шара (может адаптивно меняться)")]
    public float ballSpeed = 2f;

    [Header("Факторы изменения скорости")]
    [Tooltip("Умножается на текущую скорость, если поймано ≥80%")]
    public float speedIncreaseFactor = 1.1f;

    [Tooltip("Умножается на текущую скорость, если поймано ≤50%")]
    public float speedDecreaseFactor = 0.6f;

    [Header("Self-adaptation (disable for ML training)")]
    public bool selfAdaptive = false;

    // Клампы скорости (защита от нуля и слишком больших значений)
    public Vector2 ballSpeedClamp = new Vector2(0.20f, 5.00f);

    // ---------- LEVEL 2: цветные шары ----------
    [Header("Level 2 — цветные шары")]
    public bool useColors = false;

    [Header("AI Control Params")]
    [Range(0f, 1f)] public float currentSpawnSpread = 1.0f;

    // Список отсортированных точек
    private List<Transform> _sortedSpawnPoints;

    [Range(0f, 1f)]
    [Tooltip("Вероятность КРАСНОГО шара (0.35 = 35%). Синий = 1 - красный.")]
    public float redChance = 0.35f;

    [Tooltip("Материал (или цвет) для СИНИХ шаров")]
    public Material blueMaterial;

    [Tooltip("Материал (или цвет) для КРАСНЫХ шаров")]
    public Material redMaterial;

    public event Action<GameObject> OnBallSpawned;

    private int spawnCount = 0;
    private int catchCount = 0;
    private bool isSpawning = false;
    public bool IsSpawning => isSpawning;

    private int nextBallId = 0;

    private float baseBallSpeed = 1f;
    private float _lastSpawnTime = -999f;

    private static readonly BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private void OnValidate()
    {
        ballSpeed = Mathf.Clamp(ballSpeed, ballSpeedClamp.x, ballSpeedClamp.y);
        spawnInterval = Mathf.Max(0.05f, spawnInterval);
    }

    private void Awake()
    {
        var pm = PatientManager.Instance;
        if (pm != null) pm.OnSelectedPatientChanged += OnSelectedPatientChanged;
        ClampRuntime();
    }

    private void Start()
    {
        // Сортируем точки слева направо (по X)
        if (spawnPoints != null)
            _sortedSpawnPoints = spawnPoints.OrderBy(t => t.position.x).ToList();

        ApplySettingsFromCurrentPatient();

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnGoodCatch += HandleBallCaught;
    }

    private void OnDestroy()
    {
        var pm = PatientManager.Instance;
        if (pm != null) pm.OnSelectedPatientChanged -= OnSelectedPatientChanged;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnGoodCatch -= HandleBallCaught;
    }

    // ---------- Применение настроек ----------
    private void OnSelectedPatientChanged(Patient p)
    {
        if (p != null) ApplySettings(p.settings);
    }

    private void ApplySettingsFromCurrentPatient()
    {
        var s = PatientManager.Instance?.Current?.settings;
        ApplySettings(s);
    }

    public void ApplySettings(object settings)
    {
        if (settings == null) return;

        // EnsureDefaults (если есть)
        try
        {
            var m = settings.GetType().GetMethod("EnsureDefaults", BF);
            m?.Invoke(settings, null);
        }
        catch { /* ignore */ }

        // Алиасы для чтения
        float sSpawn = GetFloat(settings, new[] { "SpawnInterval", "spawnInterval" }, 1.5f);
        float sSpeed = GetFloat(settings, new[] { "BallSpeed", "ballSpeed" }, 1.0f);
        float sInc = GetFloat(settings, new[] { "SpeedIncreaseFactor", "speedIncreaseFactor" }, 1.1f);
        float sDec = GetFloat(settings, new[] { "SpeedDecreaseFactor", "speedDecreaseFactor" }, 0.6f);
        float sRed = GetFloat(settings, new[] { "RedChance", "redChance" }, 0.35f);

        // Применяем
        spawnInterval = Mathf.Clamp(sSpawn, 0.05f, 10f);
        baseBallSpeed = Mathf.Clamp(sSpeed, 0.05f, ballSpeedClamp.y);

        // ВАЖНО: если сложностью управляет DifficultyController/Coach — НЕ перетираем ballSpeed
        if (!externalDifficultyOverride)
        {
            ballSpeed = Mathf.Clamp(baseBallSpeed, ballSpeedClamp.x, ballSpeedClamp.y);
            TryUpdateActiveBallsSpeed(ballSpeed);
        }

        speedIncreaseFactor = Mathf.Max(0.1f, sInc);
        speedDecreaseFactor = Mathf.Max(0.1f, sDec);
        redChance = Mathf.Clamp01(sRed);

        ClampRuntime();

        // Не делаем CancelInvoke() без имени — иначе можно «сломать» другие Invokes.
        // Перестраиваем спавн ТОЛЬКО если уже спавним.
        if (isSpawning && gameObject.activeInHierarchy)
        {
            UpdateIntervalSafely(spawnInterval);
        }

#if UNITY_EDITOR
        Debug.Log($"[Spawner] Settings applied: spawn={spawnInterval:F2}s, baseSpeed={baseBallSpeed:F2}, ballSpeed={ballSpeed:F2}, override={externalDifficultyOverride}");
#endif
    }

    // ---------- Публичный API ----------
    public void StartSpawning()
    {
        if (isSpawning) return;

        spawnCount = 0;
        catchCount = 0;
        aiSpawnBias = 0f;

        ApplySettingsFromCurrentPatient();

        isSpawning = true;

        // Стартуем аккуратно
        CancelInvoke(nameof(SpawnBall));
        _lastSpawnTime = Time.time;
        InvokeRepeating(nameof(SpawnBall), 1f, spawnInterval);
    }

    public void StopSpawning()
    {
        if (!isSpawning) return;
        isSpawning = false;
        CancelInvoke(nameof(SpawnBall));
    }

    /// <summary>
    /// Обновляет интервал спавна без «спама» и без скачков.
    /// Сохраняет фазу: следующий шар — через остаток времени до периода.
    /// </summary>
    public void UpdateIntervalSafely(float newInterval)
    {
        newInterval = Mathf.Clamp(newInterval, 0.05f, 10f);

        // если почти не изменилось — не трогаем таймер (важно против спама)
        if (Mathf.Abs(newInterval - spawnInterval) < 0.02f)
        {
            spawnInterval = newInterval;
            return;
        }

        spawnInterval = newInterval;

        if (!isSpawning || !gameObject.activeInHierarchy)
            return;

        CancelInvoke(nameof(SpawnBall));

        float elapsed = Time.time - _lastSpawnTime;
        float delay = Mathf.Clamp(spawnInterval - elapsed, 0.01f, spawnInterval);

        InvokeRepeating(nameof(SpawnBall), delay, spawnInterval);
    }

    public void SpawnBall()
    {
        _lastSpawnTime = Time.time;

        if (ballPrefab == null || spawnPoints == null || spawnPoints.Length == 0 || playerTransform == null)
            return;

        spawnCount++;

        var spawnPoint = GetBiasedSpawnPoint();
        if (spawnPoint == null) return;

        GameObject ball = Instantiate(ballPrefab, spawnPoint.position, Quaternion.identity);
        ball.tag = "Ball";

        int ballId = nextBallId++;
        var collision = ball.GetComponent<BallCollision>();
        if (collision != null) collision.BallId = ballId;

        // Цвета
        if (useColors)
        {
            bool makeRed = UnityEngine.Random.value < redChance;
            if (collision != null) collision.isRed = makeRed;

            var rend = ball.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                if (makeRed && redMaterial != null) rend.material = redMaterial;
                if (!makeRed && blueMaterial != null) rend.material = blueMaterial;
            }
        }
        else
        {
            if (collision != null) collision.isRed = false;
        }

        ScoreManager.Instance?.RegisterSpawn(ballId);

        if (ball.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

            Vector3 target = new Vector3(spawnPoint.position.x, spawnPoint.position.y, playerTransform.position.z);
            rb.velocity = (target - spawnPoint.position).normalized * Mathf.Clamp(ballSpeed, ballSpeedClamp.x, ballSpeedClamp.y);   
        }

        if (debugSpawner)
            {
                Debug.Log($"[SPAWN] usedSpeed={ballSpeed:F2} velMag={rb.velocity.magnitude:F2}");
            }

        // Локальная адаптация скорости (в тренинге ML обычно выключаем)
        if (!externalDifficultyOverride)
        {
            float ratio = catchCount / 10f;
            if (ratio >= 0.8f)
                ballSpeed = Mathf.Clamp(ballSpeed * speedIncreaseFactor, ballSpeedClamp.x, ballSpeedClamp.y);
            else if (ratio <= 0.5f)
                ballSpeed = Mathf.Clamp(ballSpeed * speedDecreaseFactor, ballSpeedClamp.x, ballSpeedClamp.y);
            catchCount = 0;
        }

        OnBallSpawned?.Invoke(ball);

#if UNITY_EDITOR
        Debug.Log($"Spawned at {Time.time} | Frame: {Time.frameCount}");
#endif
    }

    private void ClampRuntime()
    {
        ballSpeed = Mathf.Clamp(ballSpeed, ballSpeedClamp.x, ballSpeedClamp.y);
        spawnInterval = Mathf.Max(0.05f, spawnInterval);
    }

    private Transform GetBiasedSpawnPoint()
    {
        if (_sortedSpawnPoints == null || _sortedSpawnPoints.Count == 0)
            return null;

        int count = _sortedSpawnPoints.Count;

        // 1) Размер окна
        float windowSizeFloat = Mathf.Lerp(1f, count, currentSpawnSpread);
        int windowSize = Mathf.Clamp(Mathf.RoundToInt(windowSizeFloat), 1, count);

        // 2) Центр с учетом Bias
        float centerIndex = (count - 1) / 2f;
        float biasOffset = aiSpawnBias * (count / 2f);
        float targetCenter = centerIndex + biasOffset;

        // 3) Границы
        int minIndex = Mathf.RoundToInt(targetCenter - (windowSize / 2f));
        int maxIndex = minIndex + windowSize - 1;

        // 4) Коррекция границ
        if (minIndex < 0)
        {
            minIndex = 0;
            maxIndex = minIndex + windowSize - 1;
        }
        if (maxIndex >= count)
        {
            maxIndex = count - 1;
            minIndex = maxIndex - windowSize + 1;
        }

        minIndex = Mathf.Clamp(minIndex, 0, count - 1);
        maxIndex = Mathf.Clamp(maxIndex, 0, count - 1);

        // 5) Случайная точка внутри окна
        int finalIndex = UnityEngine.Random.Range(minIndex, maxIndex + 1);
        return _sortedSpawnPoints[finalIndex];
    }

    private void HandleBallCaught() => catchCount++;

    private void TryUpdateActiveBallsSpeed(float newSpeed)
    {
        // Обновляет активные мячи (если в проекте есть соответствующий метод SetSpeed/SetBaseSpeed)
        // и/или корректирует velocity.
        var rbs = FindObjectsOfType<Rigidbody>(false);
        for (int i = 0; i < rbs.Length; i++)
        {
            var rb = rbs[i];
            if (!rb || !rb.gameObject.activeInHierarchy) continue;

            // фильтр
            if (!rb.gameObject.name.ToLower().Contains("ball") && !rb.gameObject.CompareTag("Ball"))
                continue;

            var mb = rb.GetComponent<MonoBehaviour>();
            if (mb)
            {
                var t = mb.GetType();
                var m = t.GetMethod("SetBaseSpeed", BF) ?? t.GetMethod("SetSpeed", BF);
                if (m != null && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(float))
                {
                    try { m.Invoke(mb, new object[] { newSpeed }); continue; } catch { }
                }
            }

            if (rb.velocity.sqrMagnitude > 0.0001f)
                rb.velocity = rb.velocity.normalized * newSpeed;
        }
    }

    private static float GetFloat(object obj, string[] names, float defVal)
    {
        if (obj == null) return defVal;

        foreach (var n in names)
        {
            var f = obj.GetType().GetField(n, BF);
            if (f != null)
            {
                try { return Convert.ToSingle(f.GetValue(obj)); }
                catch { }
            }
            var p = obj.GetType().GetProperty(n, BF);
            if (p != null && p.CanRead)
            {
                try { return Convert.ToSingle(p.GetValue(obj, null)); }
                catch { }
            }
        }
        return defVal;
    }
}


// ===============================
// FILE: DifficultyController.cs  (ПАТЧ: только метод ApplyToGame)
// ===============================
// ВАЖНО: замените ТОЛЬКО метод ApplyToGame в вашем DifficultyController.cs на этот.

/*
private void ApplyToGame()
{
    if (!spawner) return;

    // говорим спавнеру: теперь сложность управляется извне
    spawner.externalDifficultyOverride = true;

    // interval: обновляем через безопасный рескейджул
    float newInterval = Mathf.Max(0.05f, _spawnInterval);
    spawner.UpdateIntervalSafely(newInterval);

    // speed: просто выставляем (кламп внутри спавнера)
    spawner.SetBallSpeed(Mathf.Max(0.05f, _ballSpeed));

    // spawn radius spread 0..1
    float normRadius = Mathf.InverseLerp(spawnRadiusRange.x, spawnRadiusRange.y, _spawnRadius);
    spawner.SetSpawnRadius(normRadius);
}
*/
