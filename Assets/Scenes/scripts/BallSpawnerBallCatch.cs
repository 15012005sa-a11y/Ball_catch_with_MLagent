using System;
using System.Reflection;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

// ВОТ ЭТОЙ СТРОКИ НЕ ХВАТАЛО:
public class BallSpawnerBallCatch : MonoBehaviour
{
    [Header("Настройки спавна")]
    public GameObject ballPrefab;
    public Transform[] spawnPoints;
    public Transform playerTransform;

    public void SetBallSpeed(float v) { ballSpeed = v; }
    public void SetSpawnInterval(float v) { spawnInterval = v; }
    // Реализация методов управления для агента
    public void SetSpawnRadius(float normalizedRadius) { currentSpawnSpread = Mathf.Clamp01(normalizedRadius); }
    public void SetTargetRadius(float v) { /* если появится поле targetRadius, присвоить его здесь */ }

    [Header("Параметры движения (текущие)")]
    [Tooltip("Интервал между появлениями шаров, сек")]
    public float spawnInterval = 1.5f;

    [Header("AI-управление спавном")]
    [Range(-1f, 1f)]
    public float aiSpawnBias = 0f;
    // -1 = максимально левый/нижний спавнпоинт
    // +1 = максимально правый/верхний

    [Tooltip("Текущая скорость шара (может адаптивно меняться)")]
    public float ballSpeed = 2f;

    [Header("Факторы изменения скорости")]
    [Tooltip("Умножается на текущую скорость, если поймано ≥80%")]
    public float speedIncreaseFactor = 1.1f;

    // ВКЛ/ВЫКЛ локальной адаптации скорости (для ML — выключено)
    [Header("Self-adaptation (disable for ML training)")]
    public bool selfAdaptive = false;

    // Клампы скорости (защита от нуля и слишком больших значений)
    public Vector2 ballSpeedClamp = new Vector2(0.20f, 5.00f);

    [Tooltip("Умножается на текущую скорость, если поймано ≤50%")]
    public float speedDecreaseFactor = 0.6f;

    // ---------- LEVEL 2: цветные шары ----------
    [Header("Level 2 — цветные шары")]
    [Tooltip("Включить правила уровня 2 (синие/красные шары)")]
    public bool useColors = false;

    // Добавляем переменную для ширины охвата (0 = одна точка, 1 = все точки)
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
    // --------------------------------------------
    public event Action<GameObject> OnBallSpawned;

    private int spawnCount = 0;
    private int catchCount = 0;
    private bool isSpawning = false;
    private int nextBallId = 0;

    private void OnValidate() { ballSpeed = Mathf.Max(0.05f, ballSpeed); }

    private float baseBallSpeed = 1f;

    // ---------- Unity ----------
    private void Awake()
    {
        var pm = PatientManager.Instance;
        if (pm != null) pm.OnSelectedPatientChanged += OnSelectedPatientChanged;
        ClampRuntime();
    }

    private void Start()
    {
        // 1. Сортируем точки слева направо (по X) один раз при старте
        if (spawnPoints != null)
        {
            _sortedSpawnPoints = spawnPoints.OrderBy(t => t.position.x).ToList();
        }

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

        try
        {
            var m = settings.GetType().GetMethod("EnsureDefaults",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            m?.Invoke(settings, null);
        }
        catch { /* игнор */ }

        // Алиасы для чтения
        float sSpawn = GetFloat(settings, new[] { "SpawnInterval", "spawnInterval" }, 1.5f);
        float sSpeed = GetFloat(settings, new[] { "BallSpeed", "ballSpeed" }, 1.0f);
        float sInc = GetFloat(settings, new[] { "SpeedIncreaseFactor", "speedIncreaseFactor" }, 1.1f);
        float sDec = GetFloat(settings, new[] { "SpeedDecreaseFactor", "speedDecreaseFactor" }, 0.6f);
        float sRed = GetFloat(settings, new[] { "RedChance", "redChance" }, 0.35f);

        spawnInterval = Mathf.Clamp(sSpawn, 0.05f, 10f);
        baseBallSpeed = Mathf.Max(0.05f, sSpeed);
        ballSpeed = baseBallSpeed;
        speedIncreaseFactor = Mathf.Max(0.1f, sInc);
        speedDecreaseFactor = Mathf.Max(0.1f, sDec);
        redChance = Mathf.Clamp01(sRed);

        CancelInvoke();
        if (gameObject.activeInHierarchy && isSpawning)
        {
            InvokeRepeating(nameof(SpawnBall), 0.01f, spawnInterval);
        }

        TryUpdateActiveBallsSpeed(baseBallSpeed);

        Debug.Log($"[Spawner] Settings applied: spawn={spawnInterval:F2}s, speed(base)={baseBallSpeed:F2}, " +
                  $"inc×{speedIncreaseFactor:F2}, dec×{speedDecreaseFactor:F2}, redChance={redChance:0.##}");
    }

    private void TryUpdateActiveBallsSpeed(float newBaseSpeed)
    {
        var rbs = FindObjectsOfType<Rigidbody>(false);
        for (int i = 0; i < rbs.Length; i++)
        {
            var rb = rbs[i];
            if (!rb || !rb.gameObject.activeInHierarchy) continue;
            if (!rb.gameObject.name.ToLower().Contains("ball") && !rb.gameObject.CompareTag("Ball"))
                continue;

            var mb = rb.GetComponent<MonoBehaviour>();
            if (mb)
            {
                var t = mb.GetType();
                var m = t.GetMethod("SetBaseSpeed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                     ?? t.GetMethod("SetSpeed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(float))
                {
                    try { m.Invoke(mb, new object[] { newBaseSpeed }); continue; } catch { }
                }
            }
            if (rb.velocity.sqrMagnitude > 0.0001f)
                rb.velocity = rb.velocity.normalized * newBaseSpeed;
        }
    }

    // ---------- Публичный API ----------
    public void StartSpawning()
    {
        if (isSpawning) return;

        spawnCount = 0;
        catchCount = 0;
        aiSpawnBias = 0f;

        ApplySettingsFromCurrentPatient();

        // Если PreferencesPanel нет в проекте, закомментируйте следующие 3 строки:
        var pid = PatientManager.Instance?.Current?.id ?? -1;
        // var tune = PreferencesPanel.LoadSpawnerTuning(pid); 
        // if (tune != null) ApplySettings(tune);

        isSpawning = true;
        InvokeRepeating(nameof(SpawnBall), 1f, spawnInterval);
    }

    public void StopSpawning()
    {
        if (!isSpawning) return;
        isSpawning = false;
        CancelInvoke(nameof(SpawnBall));
    }

    public void SpawnBall()
    {
        if (ballPrefab == null || spawnPoints == null || spawnPoints.Length == 0 || playerTransform == null)
            return;

        spawnCount++;

        var spawnPoint = GetBiasedSpawnPoint();
        if (spawnPoint == null) return;

        GameObject ball = Instantiate(ballPrefab, spawnPoint.position, Quaternion.identity);
        ball.tag = "Ball";

        int ballId = nextBallId++;
        var collision = ball.GetComponent<BallCollision>();
        if (collision != null)
            collision.BallId = ballId;

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
            rb.velocity = (target - spawnPoint.position).normalized * ballSpeed;
        }

        if (selfAdaptive && spawnCount % 10 == 0)
        {
            float ratio = catchCount / 10f;
            if (ratio >= 0.8f)
                ballSpeed = Mathf.Clamp(ballSpeed * speedIncreaseFactor, ballSpeedClamp.x, ballSpeedClamp.y);
            else if (ratio <= 0.5f)
                ballSpeed = Mathf.Clamp(ballSpeed * speedDecreaseFactor, ballSpeedClamp.x, ballSpeedClamp.y);
            catchCount = 0;
        }

        OnBallSpawned?.Invoke(ball);
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

        // 1. Размер окна
        float windowSizeFloat = Mathf.Lerp(1f, count, currentSpawnSpread);
        int windowSize = Mathf.RoundToInt(windowSizeFloat);

        // 2. Центр с учетом Bias
        float centerIndex = (count - 1) / 2f;
        float biasOffset = aiSpawnBias * (count / 2f);
        float targetCenter = centerIndex + biasOffset;

        // 3. Границы
        int minIndex = Mathf.RoundToInt(targetCenter - (windowSize / 2f));
        int maxIndex = minIndex + windowSize - 1;

        // 4. Коррекция границ
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

        // 5. Случайная точка внутри окна
        int finalIndex = UnityEngine.Random.Range(minIndex, maxIndex + 1);

        return _sortedSpawnPoints[finalIndex];
    }

    private void HandleBallCaught() => catchCount++;

    private static readonly BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

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
