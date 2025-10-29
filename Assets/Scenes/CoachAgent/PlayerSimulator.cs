using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Простой симулятор игрока: реагирует на каждый спавн мяча,
/// "ловит" с вероятностью hitProbability и пишет время реакции.
/// Работает поверх вашего ScoreManager — без изменения остальных скриптов.
/// </summary>
[DisallowMultipleComponent]
public class PlayerSimulator : MonoBehaviour
{
    [Header("Основные настройки")]
    [Range(0f, 1f)] public float hitProbability = 0.75f;   // целевой % попаданий
    public float reactionMeanMs = 450f;                     // среднее время реакции (мс)
    public float reactionStdMs = 120f;                     // разброс (мс)

    [Header("Дополнительно")]
    [Tooltip("Вероятность 'компенсации корпусом' — пока только для будущего использования")]
    [Range(0f, 1f)] public float trunkCompProbability = 0.10f;

    [Tooltip("Минимальная/максимальная задержка реакции (сек) для отсечения хвостов")]
    public float minReactionSec = 0.08f;
    public float maxReactionSec = 1.20f;

    [Tooltip("Автоматически запускать новую сессию после завершения предыдущей")]
    public bool autoLoopSessions = true;

    ScoreManager score;
    HashSet<int> handledSpawnIds = new HashSet<int>();

    #pragma warning disable CS0414
    private int lastSeenSpawnCount = 0;
    #pragma warning restore CS0414

    System.Random rng;

    void Awake()
    {
        score = FindObjectOfType<ScoreManager>(true);
        rng = new System.Random();
        if (!score)
            Debug.LogError("[PlayerSimulator] Не найден ScoreManager в сцене.");
    }

    void OnEnable()
    {
        if (score != null)
            score.OnSessionFinished.AddListener(OnSessionFinished);
        lastSeenSpawnCount = 0;
        handledSpawnIds.Clear();
    }

    void OnDisable()
    {
        if (score != null)
            score.OnSessionFinished.RemoveListener(OnSessionFinished);
    }

    void OnSessionFinished()
    {
        handledSpawnIds.Clear();
        lastSeenSpawnCount = 0;

        if (autoLoopSessions && score != null && score.isActiveAndEnabled)
        {
            // Запустим следующую сессию через маленькую паузу
            StartCoroutine(StartNextSessionCo());
        }
    }

    IEnumerator StartNextSessionCo()
    {
        yield return new WaitForSeconds(0.5f);
        // Если у вас есть переход между уровнями через LevelDirector — это не мешает.
        // Здесь старт простого одиночного запуска:
        score.StartSession();
    }

    void Update()
    {
        if (score == null || score.spawnTimes == null || score.spawnTimes.Count == 0)
            return;

        // Отслеживаем новые спавны по словарю spawnTimes (ключ = ballId)
        foreach (var kv in score.spawnTimes)
        {
            int ballId = kv.Key;
            if (handledSpawnIds.Contains(ballId)) continue;

            handledSpawnIds.Add(ballId);
            // Для каждого спавна создаём корутину, которая сработает как "реакция игрока"
            StartCoroutine(ReactToSpawnCo(ballId));
        }
    }

    IEnumerator ReactToSpawnCo(int ballId)
    {
        // Сэмплируем время реакции (модель Гаусса с отсечением)
        float reactSec = Mathf.Clamp(Normal(reactionMeanMs, reactionStdMs) / 1000f, minReactionSec, maxReactionSec);
        yield return new WaitForSeconds(reactSec);

        if (score == null) yield break;

        // Фиксируем время реакции для метрик окна
        score.RecordReactionTime(reactSec);

        // Решаем, поймали мяч или промахнулись
        if (rng.NextDouble() < hitProbability)
        {
            score.AddScore(1);
        }
        else
        {
            score.RegisterMiss(); // просто событие; на метрику окна влияет отсутствие AddScore
        }
    }

    // Генератор нормального распределения (Бокса-Мюллера)
    float Normal(float mean, float std)
    {
        // (0,1] для корректности логарифма
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        double randStdNormal = System.Math.Sqrt(-2.0 * System.Math.Log(u1)) *
                               System.Math.Sin(2.0 * System.Math.PI * u2);
        return (float)(mean + std * randStdNormal);
    }
}
