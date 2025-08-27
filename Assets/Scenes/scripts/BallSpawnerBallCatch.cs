using System;
using UnityEngine;

public class BallSpawnerBallCatch : MonoBehaviour
{
    [Header("Настройки спавна")]
    public GameObject ballPrefab;
    public Transform[] spawnPoints;
    public Transform playerTransform;

    [Header("Параметры движения")]
    public float spawnInterval = 1.5f;
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

    private int spawnCount = 0;   // сколько всего заспавнено шаров
    private int catchCount = 0;   // сколько из них поймано
    private bool isSpawning = false;

    // Счётчик для выдачи уникального ID каждому шару
    private int nextBallId = 0;

    private void Start()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnGoodCatch += HandleBallCaught; // было OnBallCaught
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnGoodCatch -= HandleBallCaught; // было OnBallCaught
    }



    /// <summary>Запуск повторяющегося спавна.</summary>
    public void StartSpawning()
    {
        if (isSpawning) return;

        spawnCount = 0;
        catchCount = 0;

        isSpawning = true;
        InvokeRepeating(nameof(SpawnBall), 1f, spawnInterval);
    }

    /// <summary>Остановка спавна.</summary>
    public void StopSpawning()
    {
        if (!isSpawning) return;

        isSpawning = false;
        CancelInvoke(nameof(SpawnBall));
    }

    /// <summary>Создаёт один шар, выдаёт ему ID и сообщает ScoreManager о времени спавна.</summary>
    public void SpawnBall()
    {
        if (ballPrefab == null || spawnPoints == null || spawnPoints.Length == 0 || playerTransform == null)
            return;

        spawnCount++;

        // 1) Instantiate
        var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        GameObject ball = Instantiate(ballPrefab, spawnPoint.position, Quaternion.identity);

        // 2) Уникальный ID (для реакции)
        int ballId = nextBallId++;
        var collision = ball.GetComponent<BallCollision>();
        if (collision != null)
            collision.BallId = ballId;

        // 3) LEVEL 2: назначить тип/цвет, если включено
        if (useColors)
        {
            bool makeRed = UnityEngine.Random.value < redChance; // true = красный, false = синий

            // сообщим в BallCollision, красный ли шар (для штрафа)
            if (collision != null)
                collision.isRed = makeRed;

            // покрасим визуально
            var rend = ball.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                if (makeRed && redMaterial != null) rend.material = redMaterial;
                if (!makeRed && blueMaterial != null) rend.material = blueMaterial;
            }
        }
        else
        {
            // если цвета не используются — шар считается «не красным»
            if (collision != null) collision.isRed = false;
        }

        // 4) Зарегистрировать время спавна (для реакции)
        ScoreManager.Instance?.RegisterSpawn(ballId);

        // 5) Полёт строго в сторону игрока по оси Z (без "ухода" в сторону)
        if (ball.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

            // цель: та же X,Y что у точки спавна, Z как у игрока
            Vector3 target = new Vector3(spawnPoint.position.x, spawnPoint.position.y, playerTransform.position.z);
            rb.velocity = (target - spawnPoint.position).normalized * ballSpeed;
        }

        // 6) Адаптив каждые 10 шаров
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

    private void HandleBallCaught()
    {
        catchCount++;
    }
}
