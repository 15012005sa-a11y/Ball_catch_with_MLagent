using System;
using System.Reflection;
using UnityEngine;

public class BallSpawnerBallCatch : MonoBehaviour
{
    [Header("Настройки спавна")]
    public GameObject ballPrefab;
    public Transform[] spawnPoints;
    public Transform playerTransform;

    [Header("Параметры движения (текущие)")]
    [Tooltip("Интервал между появлениями шаров, сек")]
    public float spawnInterval = 1.5f;

    [Tooltip("Текущая скорость шара (может адаптивно меняться)")]
    public float ballSpeed = 1f;

    [Header("Факторы изменения скорости")]
    [Tooltip("Умножается на текущую скорость, если поймано ≥80%")]
    public float speedIncreaseFactor = 1.1f;

    [Tooltip("Умножается на текущую скорость, если поймано ≤50%")]
    public float speedDecreaseFactor = 0.6f;

    // ---------- LEVEL 2: цветные шары ----------
    [Header("Level 2 — цветные шары")]
    [Tooltip("Включить правила уровня 2 (синие/красные шары)")]
    public bool useColors = false;

    [Range(0f, 1f)]
    [Tooltip("Вероятность КРАСНОГО шара (0.35 = 35%). Синий = 1 - красный.")]
    public float redChance = 0.35f;

    [Tooltip("Материал (или цвет) для СИНИХ шаров")]
    public Material blueMaterial;

    [Tooltip("Материал (или цвет) для КРАСНЫХ шаров")]
    public Material redMaterial;
    // --------------------------------------------

    private int spawnCount = 0;
    private int catchCount = 0;
    private bool isSpawning = false;
    private int nextBallId = 0;

    private float baseBallSpeed = 1f;

    // ---------- Unity ----------
    private void Awake()
    {
        var pm = PatientManager.Instance;
        if (pm != null) pm.OnSelectedPatientChanged += OnSelectedPatientChanged;
    }

    private void Start()
    {
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
        if (p != null) ApplySettings(p.settings); // settings может быть GameSettings или PatientSettings
    }

    private void ApplySettingsFromCurrentPatient()
    {
        var s = PatientManager.Instance?.Current?.settings;
        ApplySettings(s);
    }

    /// <summary>
    /// Универсальный приём настроек: принимает и GameSettings, и PatientSettings,
    /// читая значения по алиасам свойств/полей.
    /// </summary>
    public void ApplySettings(object settings)
    {
        if (settings == null) return;

        // Попробуем вызвать EnsureDefaults(), если он есть
        try
        {
            var m = settings.GetType().GetMethod("EnsureDefaults",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            m?.Invoke(settings, null);
        }
        catch { /* игнор */ }

        // Алиасы для чтения (имена в PascalCase и camelCase)
        float sSpawn = GetFloat(settings, new[] { "SpawnInterval", "spawnInterval" }, 1.5f);
        float sSpeed = GetFloat(settings, new[] { "BallSpeed", "ballSpeed" }, 1.0f);
        float sInc = GetFloat(settings, new[] { "SpeedIncreaseFactor", "speedIncreaseFactor" }, 1.1f);
        float sDec = GetFloat(settings, new[] { "SpeedDecreaseFactor", "speedDecreaseFactor" }, 0.6f);
        float sRed = GetFloat(settings, new[] { "RedChance", "redChance" }, 0.35f);

        spawnInterval = Mathf.Clamp(sSpawn, 0.05f, 10f);
        baseBallSpeed = Mathf.Max(0.05f, sSpeed);
        ballSpeed = baseBallSpeed; // сброс к базе при применении
        speedIncreaseFactor = Mathf.Max(0.1f, sInc);
        speedDecreaseFactor = Mathf.Max(0.1f, sDec);
        redChance = Mathf.Clamp01(sRed);

        // если спавн уже идёт — перезапустим с новым интервалом
        if (isSpawning)
        {
            CancelInvoke(nameof(SpawnBall));
            InvokeRepeating(nameof(SpawnBall), 0.01f, spawnInterval);
        }

        Debug.Log($"[Spawner] Settings applied: spawn={spawnInterval:F2}s, speed(base)={baseBallSpeed:F2}, " +
                  $"inc×{speedIncreaseFactor:F2}, dec×{speedDecreaseFactor:F2}, redChance={redChance:0.##}");
    }

    // ---------- Публичный API ----------
    public void StartSpawning()
    {
        if (isSpawning) return;

        spawnCount = 0;
        catchCount = 0;

        ApplySettingsFromCurrentPatient();

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

        var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        GameObject ball = Instantiate(ballPrefab, spawnPoint.position, Quaternion.identity);

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

        if (spawnCount % 10 == 0)
        {
            float ratio = catchCount / 10f;
            if (ratio >= 0.8f)
            {
                ballSpeed *= speedIncreaseFactor;
                Debug.Log($"[Adaptive] {catchCount}/10 → speed ↑ to {ballSpeed:F2}");
            }
            else if (ratio <= 0.5f)
            {
                ballSpeed *= speedDecreaseFactor;
                Debug.Log($"[Adaptive] {catchCount}/10 → speed ↓ to {ballSpeed:F2}");
            }
            catchCount = 0;
        }
    }

    private void HandleBallCaught() => catchCount++;

    // ---------- Reflection helpers ----------
    private static readonly BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static float GetFloat(object obj, string[] names, float defVal)
    {
        if (obj == null) return defVal;

        foreach (var n in names)
        {
            // Поле
            var f = obj.GetType().GetField(n, BF);
            if (f != null)
            {
                try { return Convert.ToSingle(f.GetValue(obj)); }
                catch { }
            }
            // Свойство
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
