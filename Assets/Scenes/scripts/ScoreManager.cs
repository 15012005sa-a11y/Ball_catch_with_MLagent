using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class ScoreManager : MonoBehaviour
{
    public UnityEvent OnSessionFinished;
    public static ScoreManager Instance { get; private set; }

    public event System.Action OnBallCaught;   // оставьте как было
    public event System.Action OnGoodCatch;    // только правильный шар
    public event System.Action OnRedTouched;   // красный шар (ошибка)
    public event System.Action OnMissed;     // НОВОЕ: промах синим шаром

    [Header("Exporter")]
    public ExcelExporter exporter;

    [Header("UI Elements")]
    public TMP_Text currentScoreText;
    public TMP_Text bestScoreText;
    public TMP_Text timerText;

    [Header("Session Settings")]
    [Tooltip("Длительность игрового таймера в секундах")]
    public float sessionDuration = 20f;

    [Header("Dependencies")]
    public BallSpawnerBallCatch ballSpawner;
    public GameObject uiPanel;
    public GameObject startButton;

    [Header("Tracking")]
    [Tooltip("Скрипт, который считает углы рук (Left/Right)")]
    public MotionLogger motionLogger;

    [Header("Audio")]
    public AudioClip popSound;
    private AudioSource audioSource;

    [Header("Accuracy Tracking")]
    [Tooltip("Макс. расстояние (в юнитах), после которого точность считается = 0")]
    public float maxCatchDistance = 2f;

    [Header("Extra UI")]
    public GameObject graphButton;

    [Header("Penalties")]
    [Tooltip("На сколько уменьшаем счёт при касании красного шара")]
    public int redPenalty = 1;
    [Tooltip("Запрещать уходить счёту в минус")]
    public bool clampScoreToZero = true;

    [HideInInspector] public bool showGraphButtonOnMenu = false;
    [HideInInspector] public bool showStartButtonOnMenu = true;

    // Метрики за сессию
    private readonly List<float> accuracyList = new();
    private readonly List<float> reactionTimes = new();

    // Для учёта времени спавна шаров
    public Dictionary<int, float> spawnTimes = new();
    public int nextBallId = 0;

    private int currentScore;
    private float timer;
    private bool sessionRunning;

    // Только при самом первом запуске обнуляем счёт
    private bool resetScoreOnStart = true;

    public void SetShowStartButton(bool value)
    {
        showStartButtonOnMenu = value;
        if (!sessionRunning && startButton) startButton.SetActive(value);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();

            if (motionLogger == null)
                motionLogger = FindObjectOfType<MotionLogger>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        uiPanel?.SetActive(true);
        if (graphButton) graphButton.SetActive(false);

        sessionRunning = false;
        timer = sessionDuration;
        currentScore = 0;
        UpdateUI();
    }

    private void Update()
    {
        if (!sessionRunning) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = 0f;
            EndSession();
        }
        UpdateUI();
    }

    /// <summary>Привязать к кнопке Start Game → OnClick()</summary>
    public void StartSession()
    {
        // Первый запуск обнуляет счёт, последующие уровни — нет
        StartSessionInternal(resetScore: resetScoreOnStart);
        resetScoreOnStart = false;
    }

    /// <summary>Запуск следующего уровня БЕЗ обнуления счёта.</summary>
    public void StartSessionKeepScore()
    {
        StartSessionInternal(resetScore: false);
    }

    public void RegisterMiss()
    {
        // здесь можно вести статистику промахов, если нужно
        OnMissed?.Invoke();
    }

    private void StartSessionInternal(bool resetScore)
    {
        uiPanel?.SetActive(false);
        startButton?.SetActive(false);

        if (resetScore) currentScore = 0;
        timer = sessionDuration;
        sessionRunning = true;

        accuracyList.Clear();
        reactionTimes.Clear();
        spawnTimes.Clear();
        nextBallId = 0;

        if (ballSpawner != null) ballSpawner.StartSpawning();
        else Debug.LogWarning("[ScoreManager] ballSpawner не назначен!");

        if (motionLogger != null)
        {
            motionLogger.StartLogging();
            motionLogger.trackArmAngles = true;
        }
        else
        {
            Debug.LogWarning("[ScoreManager] MotionLogger не найден — углы рук не будут записаны");
        }

        UpdateUI();
    }

    /// <summary>Вызывается при лопании СИНИХ (или обычных) шариков.</summary>
    public void AddScore(int points)
    {
        if (!sessionRunning) return;

        currentScore += points;
        if (clampScoreToZero && currentScore < 0) currentScore = 0;

        UpdateUI();

        OnBallCaught?.Invoke();   // для совместимости со старым кодом
        OnGoodCatch?.Invoke();    // НОВОЕ: только «правильная ловля»

        if (popSound != null) audioSource.PlayOneShot(popSound);
    }


    /// <summary>
    /// НОВОЕ: вызвать при касании КРАСНОГО шара — уменьшает счёт на redPenalty.
    /// </summary>
    public void RedBallTouched()
    {
        if (!sessionRunning) return;

        currentScore -= Mathf.Abs(redPenalty);
        if (clampScoreToZero && currentScore < 0) currentScore = 0;

        UpdateUI();

        // БЫЛО: OnBallCaught?.Invoke();  // убрать, чтобы красные не считались «ловлей»
        OnRedTouched?.Invoke();            // НОВОЕ: отдельный сигнал ошибки

        if (popSound != null) audioSource.PlayOneShot(popSound);
    }

    /// <summary>Запоминает время спавна шара с данным ID.</summary>
    public void RecordSpawn(int ballId) => spawnTimes[ballId] = Time.time;

    /// <summary>Обёртка RegisterSpawn для совместимости</summary>
    public void RegisterSpawn(int ballId) => RecordSpawn(ballId);

    /// <summary>Записывает точность (0…1) по расстоянию.</summary>
    public void RecordAccuracy(float distance)
    {
        float acc = 1f - Mathf.Clamp01(distance / maxCatchDistance);
        accuracyList.Add(acc);
        Debug.Log($"[ScoreManager] Recorded accuracy: {acc:F2} (dist={distance:F2})");
    }

    /// <summary>Записывает время реакции (секунд).</summary>
    public void RecordReactionTime(float reaction)
    {
        reactionTimes.Add(reaction);
        Debug.Log($"[ScoreManager] Recorded reaction: {reaction:F3}s");
    }

    public void SetShowGraphButton(bool value)
    {
        showGraphButtonOnMenu = value;
        if (!sessionRunning && graphButton) graphButton.SetActive(value);
    }

    private void EndSession()
    {
        sessionRunning = false;
        if (ballSpawner != null)
            ballSpawner.StopSpawning();

        if (motionLogger != null)
        {
            string logName = "Motion_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            motionLogger.StopLogging(logName);
        }

        int best = PlayerPrefs.GetInt("BestScore", 0);
        if (currentScore > best)
        {
            PlayerPrefs.SetInt("BestScore", currentScore);
            PlayerPrefs.Save();
        }

        uiPanel?.SetActive(true);

        float successRate = 0f;
        if (ballSpawner != null && ballSpawner.spawnInterval > 0f)
            successRate = currentScore / (sessionDuration / ballSpawner.spawnInterval);

        float playTime = Mathf.Max(sessionDuration - timer, 0f);
        float avgReaction = reactionTimes.Count > 0 ? reactionTimes.Average() : 0f;

        float avgRightAng = 0f, avgLeftAng = 0f;
        if (motionLogger != null)
        {
            if (motionLogger.rightArmAngles != null && motionLogger.rightArmAngles.Count > 0)
                avgRightAng = motionLogger.rightArmAngles.Average();

            if (motionLogger.leftArmAngles != null && motionLogger.leftArmAngles.Count > 0)
                avgLeftAng = motionLogger.leftArmAngles.Average();
        }

        Debug.Log($"[ScoreManager] Avg React: {avgReaction:F3}s | Right: {avgRightAng:F1}° | Left: {avgLeftAng:F1}°");

        if (exporter != null)
        {
            int pid = PatientManager.Instance != null
                ? PatientManager.Instance.CurrentPatientID
                : -1;

            exporter.ExportSession(
                pid,
                currentScore,
                successRate,
                playTime,
                avgReaction,
                avgRightAng,
                avgLeftAng
            );

            Debug.Log("[ScoreManager] Данные экспортированы (PatientProgress3.csv)");
        }
        else
        {
            Debug.LogWarning("[ScoreManager] ExcelExporter не назначен — экспорт пропущен");
        }

        OnSessionFinished?.Invoke();
        uiPanel?.SetActive(true);
        uiPanel?.SetActive(true);

        if (startButton) startButton.SetActive(showStartButtonOnMenu);
        if (graphButton) graphButton.SetActive(showGraphButtonOnMenu);
    }

    private void UpdateUI()
    {
        if (currentScoreText != null)
            currentScoreText.text = $"My Score: {currentScore}";
        if (bestScoreText != null)
            bestScoreText.text = $"Best Score: {PlayerPrefs.GetInt("BestScore", 0)}";
        if (timerText != null)
            timerText.text = $"Time: {Mathf.Ceil(timer)}s";
    }
}
