// ===============================================
// ScoreManager (final)
// - Появится HomeButton только после финального финиша (после 2-го уровня)
// - Добавлен флаг LastSessionWasBetweenLevels, чтобы различать конец 1-го уровня
// - Сохранён мост на LevelDirector, чтобы гарантировать переход Rest → Level 2
// - Совместим с существующими скриптами (SetShowStartButton, StartSessionInternal(resetScore), RegisterMiss и т.д.)
// ===============================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class ScoreManager : MonoBehaviour
{
    // ==== Events / Singleton
    public UnityEvent OnSessionFinished = new UnityEvent(); // UnityEvent без параметров
    public static ScoreManager Instance { get; private set; }

    public event Action OnBallCaught;    // любой пойманный мяч
    public event Action OnGoodCatch;     // «правильный» мяч
    public event Action OnRedTouched;    // красный задет
    public event Action OnMissed;        // промах

    // ==== Inspector
    [Header("Exporter")] public ExcelExporter exporter;

    [Header("UI Elements")] public TMP_Text currentScoreText; public TMP_Text bestScoreText; public TMP_Text timerText;

    [Header("Session Settings")] public float sessionDuration = 20f;

    [Header("Dependencies")] public BallSpawnerBallCatch ballSpawner; public GameObject uiPanel; public GameObject startButton;

    [Header("Tracking")] public MotionLogger motionLogger;

    [Header("Audio")] public AudioClip popSound; private AudioSource _audio;

    [Header("Accuracy Tracking")] public float maxCatchDistance = 2f;

    [Header("Extra UI")] public GameObject graphButton;

    [Header("Penalties")] public int redPenalty = 1; public bool clampScoreToZero = true;

    [Header("UI wiring")] public bool autoWireStartButton = false;

    [HideInInspector] public bool showGraphButtonOnMenu = false;
    [HideInInspector] public bool showStartButtonOnMenu = true;

    [Header("Level durations (from PatientSettings)")]
    [SerializeField] private int level1Duration = 180;
    [SerializeField] private int level2Duration = 120;
    [SerializeField] private int restSeconds = 60; // пауза между уровнями
    public int RestSeconds => restSeconds;
    public int currentLevel = 1; // 1 или 2

    // Подавить показ меню при ближайшем завершении (для перехода 1→Rest→2)
    [NonSerialized] public bool suppressMenuOnEndOnce = false;

    // Флаг, которым можно надёжно отличить финиш 1-го уровня (между уровнями)
    public bool LastSessionWasBetweenLevels { get; private set; }

    // ==== Runtime
    private readonly List<float> _accuracy = new();
    private readonly List<float> _reactionTimes = new();
    public readonly Dictionary<int, float> spawnTimes = new();
    public int nextBallId = 0;
    private int _currentScore;
    private float _timer;
    private bool _sessionRunning;
    private bool _resetScoreOnStart = true;

    // ---- агрегатор многоэтапной сессии (L1+L2 в одну строку) ----
    bool _aggActive;
    int _aggScore;
    float _aggPlayTime;
    int _aggAttempts;                 // сколько мячей было предъявлено (оценка)
    float _aggBallSpeedSum; int _aggBallSpeedN;
    readonly List<float> _aggReactions = new();
    readonly List<float> _aggLeft = new();
    readonly List<float> _aggRight = new();
    // чтобы понимать, сбрасывался ли счёт между уровнями
    int _aggFirstStageEndScore = 0;   // счёт на конце L1

    // ==== Unity lifecycle
    private void Awake()
    {
        Instance = this;                     // новый менеджер в каждой сцене
        _audio = GetComponent<AudioSource>();
        if (!motionLogger) motionLogger = FindObjectOfType<MotionLogger>(true);
    }

    private void Start()
    {
        AutoReconnectRefs();
        uiPanel?.SetActive(true); if (graphButton) graphButton.SetActive(false);
        _sessionRunning = false; _timer = sessionDuration; _currentScore = 0; UpdateUI();
        var ps = PatientManager.Instance?.Current?.settings; if (ps != null) ApplySettingsFromPatient(ps);
        if (autoWireStartButton) WireStartButton();
        Debug.Log("[ScoreManager] Ready");
    }

    private void Update()
    {
        if (!_sessionRunning) return;
        _timer -= Time.deltaTime;
        if (_timer <= 0f) { _timer = 0f; EndSession(); }
        UpdateUI();
    }

    // ==== Public helpers (API, которые используются другими скриптами)
    public void SetShowStartButton(bool value)
    {
        showStartButtonOnMenu = value;
        if (!_sessionRunning && startButton) startButton.SetActive(value);
    }

    public void SetShowGraphButton(bool value)
    {
        showGraphButtonOnMenu = value;
        if (!_sessionRunning && graphButton) graphButton.SetActive(value);
    }

    public void SetLevel(int level) => currentLevel = Mathf.Clamp(level, 1, 2);

    public void RegisterMiss() => OnMissed?.Invoke();

    // ==== Session control
    public void StartSession()
    {
        Debug.Log("[ScoreManager] StartSession()");
        StartSessionInternal(resetScore: _resetScoreOnStart);
        _resetScoreOnStart = false;
    }

    public void StartSessionKeepScore()
    {
        Debug.Log("[ScoreManager] StartSessionKeepScore()");
        StartSessionInternal(resetScore: false);
    }

    // NB: имя параметра ДОЛЖНО быть resetScore — на него ссылаются внешние скрипты именованным аргументом
    private void StartSessionInternal(bool resetScore)
    {
        Time.timeScale = 1f;
        uiPanel?.SetActive(false); startButton?.SetActive(false);
        var ps = PatientManager.Instance?.Current?.settings; if (ps != null) ApplySettingsFromPatient(ps);
        if (resetScore) _currentScore = 0;

        _timer = (currentLevel == 1) ? level1Duration : level2Duration;
        sessionDuration = _timer;
        _sessionRunning = true;

        _accuracy.Clear(); _reactionTimes.Clear(); spawnTimes.Clear(); nextBallId = 0;

        if (!ballSpawner) ballSpawner = FindObjectOfType<BallSpawnerBallCatch>(true);
        if (ballSpawner) ballSpawner.StartSpawning(); else Debug.LogWarning("[ScoreManager] ballSpawner == null");

        if (motionLogger)
        {
            motionLogger.StartLogging();
            motionLogger.trackArmAngles = true;
        }

        Debug.Log($"[ScoreManager] Session START → level={currentLevel}, timer={_timer}s");
        UpdateUI();
    }

    private float GetBallSpeedForExport()
    {
        if (ballSpawner == null) return 0f;

        var t = ballSpawner.GetType();

        // пробуем поля
        var f = t.GetField("moveSpeed") ?? t.GetField("speed") ?? t.GetField("ballSpeed");
        if (f != null && f.FieldType == typeof(float))
            return (float)f.GetValue(ballSpawner);

        // пробуем свойства
        var p = t.GetProperty("MoveSpeed") ?? t.GetProperty("Speed") ?? t.GetProperty("BallSpeed");
        if (p != null && p.PropertyType == typeof(float))
            return (float)p.GetValue(ballSpawner, null);

        return 0f;
    }

    void AggReset()
    {
        _aggActive = false;
        _aggScore = 0; _aggPlayTime = 0; _aggAttempts = 0;
        _aggBallSpeedSum = 0; _aggBallSpeedN = 0;
        _aggReactions.Clear(); _aggLeft.Clear(); _aggRight.Clear();
    }

    void AggAddCurrentStage(float playTime, float ballSpeed)
    {
        // если это первый вызов — запоминаем счёт на конце L1
        if (!_aggActive)
            _aggFirstStageEndScore = _currentScore;

        _aggActive = true;

        _aggScore += _currentScore;        // суммарный на случай сброса
        _aggPlayTime += playTime;

        int attempts = 0;
        if (ballSpawner && ballSpawner.spawnInterval > 0f)
            attempts = Mathf.Max(1, Mathf.RoundToInt(playTime / ballSpawner.spawnInterval));
        else
            attempts = Mathf.Max(_currentScore, 1);
        _aggAttempts += attempts;

        if (_reactionTimes != null) _aggReactions.AddRange(_reactionTimes);
        if (motionLogger != null)
        {
            if (motionLogger.leftArmAngles != null) _aggLeft.AddRange(motionLogger.leftArmAngles);
            if (motionLogger.rightArmAngles != null) _aggRight.AddRange(motionLogger.rightArmAngles);
        }
        if (ballSpeed > 0f) { _aggBallSpeedSum += ballSpeed; _aggBallSpeedN++; }
    }


    (float avg, float sum) Avg(List<float> xs)
    {
        if (xs == null || xs.Count == 0) return (0f, 0f);
        float s = 0f; for (int i = 0; i < xs.Count; i++) s += xs[i];
        return (s / xs.Count, s);
    }

    private void EndSession()
    {
        _sessionRunning = false;

        if (ballSpawner) ballSpawner.StopSpawning();
        if (motionLogger)
        {
            string logName = "Motion_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            motionLogger.StopLogging(logName);
        }
        // best score
        int best = PlayerPrefs.GetInt("BestScore", 0);
        if (_currentScore > best) { PlayerPrefs.SetInt("BestScore", _currentScore); PlayerPrefs.Save(); }

        bool suppress = suppressMenuOnEndOnce;
        LastSessionWasBetweenLevels = suppress;
        suppressMenuOnEndOnce = false;

        if (!suppress)
        {
            uiPanel?.SetActive(true);
            if (startButton) startButton.SetActive(showStartButtonOnMenu);
            if (graphButton) graphButton.SetActive(showGraphButtonOnMenu);
        }
        else Debug.Log("[ScoreManager] Menu suppressed (between levels)");

        // ---------- метрики ТЕКУЩЕГО ЭТАПА ----------
        float stagePlayTime = Mathf.Max(sessionDuration - _timer, 0f);

        float stageAvgReaction = 0f;
        if (_reactionTimes != null && _reactionTimes.Count > 0)
        {
            float sum = 0f; for (int i = 0; i < _reactionTimes.Count; i++) sum += _reactionTimes[i];
            stageAvgReaction = sum / _reactionTimes.Count;
        }

        float stageRightAvg = 0f, stageLeftAvg = 0f;
        if (motionLogger != null)
        {
            if (motionLogger.rightArmAngles != null && motionLogger.rightArmAngles.Count > 0)
            { float s = 0; for (int i = 0; i < motionLogger.rightArmAngles.Count; i++) s += motionLogger.rightArmAngles[i]; stageRightAvg = s / motionLogger.rightArmAngles.Count; }
            if (motionLogger.leftArmAngles != null && motionLogger.leftArmAngles.Count > 0)
            { float s = 0; for (int i = 0; i < motionLogger.leftArmAngles.Count; i++) s += motionLogger.leftArmAngles[i]; stageLeftAvg = s / motionLogger.leftArmAngles.Count; }
        }

        float stageSuccessRate = 0f;
        if (ballSpawner && ballSpawner.spawnInterval > 0f)
            stageSuccessRate = _currentScore / (sessionDuration / ballSpawner.spawnInterval);

        float stageBallSpeed = GetBallSpeedForExport();

        // ---------- АГРЕГАЦИЯ ----------
        if (suppress)
        {
            // копим метрики L1 и выходим без записи CSV
            AggAddCurrentStage(stagePlayTime, stageBallSpeed);
            Debug.Log("[ScoreManager] Aggregating stage (L1) — CSV not written yet");
        }
        else
        {
            if (_aggActive)
            {
                // второй (последний) этап → дополняем агрегатор и пишем одну строку
                AggAddCurrentStage(stagePlayTime, stageBallSpeed);

                float aggSuccessRate = (_aggAttempts > 0) ? (float)_aggScore / _aggAttempts : 0f;
                // средние по всем собранным точкам
                float aggAvgReaction = 0f, aggAvgLeft = 0f, aggAvgRight = 0f;
                if (_aggReactions.Count > 0) { float s = 0; for (int i = 0; i < _aggReactions.Count; i++) s += _aggReactions[i]; aggAvgReaction = s / _aggReactions.Count; }
                if (_aggLeft.Count > 0) { float s = 0; for (int i = 0; i < _aggLeft.Count; i++) s += _aggLeft[i]; aggAvgLeft = s / _aggLeft.Count; }
                if (_aggRight.Count > 0) { float s = 0; for (int i = 0; i < _aggRight.Count; i++) s += _aggRight[i]; aggAvgRight = s / _aggRight.Count; }

                float aggBallSpeed = (_aggBallSpeedN > 0) ? _aggBallSpeedSum / _aggBallSpeedN : stageBallSpeed;

                if (exporter != null)
                {
                    int pid = PatientManager.Instance ? PatientManager.Instance.CurrentPatientID : -1;

                    // >>> НОВОЕ: общий счёт за игру
                    // если во 2-м уровне счётчик НЕ сбрасывался (S2 >= S1), берём _currentScore (счёт конца L2)
                    // если сбрасывался — берём сумму L1+L2 (_aggScore)
                    int finalScore = (_currentScore >= _aggFirstStageEndScore)
                        ? _currentScore
                        : _aggScore;

                    exporter.ExportSession(pid, finalScore, aggSuccessRate, _aggPlayTime,
                                           aggAvgReaction, aggAvgRight, aggAvgLeft, aggBallSpeed);
                }
                AggReset();
            }
            else
            {
                // одиночный запуск (только один уровень)
                if (exporter != null)
                {
                    int pid = PatientManager.Instance ? PatientManager.Instance.CurrentPatientID : -1;
                    exporter.ExportSession(pid, _currentScore, stageSuccessRate, stagePlayTime,
                                           stageAvgReaction, stageRightAvg, stageLeftAvg, stageBallSpeed);
                }
            }
        }

        Debug.Log("[ScoreManager] Session END");
        OnSessionFinished?.Invoke();

        // мост между уровнями — без изменений
        if (suppress)
        {
            try
            {
                var dir = FindObjectOfType<LevelDirector>(true);
                if (dir != null)
                {
                    var m = dir.GetType().GetMethod("StartRestThenLevel2External",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (m != null) { Debug.Log("[ScoreManager] Bridge → StartRestThenLevel2External()"); m.Invoke(dir, null); }
                    else
                    {
                        var m2 = dir.GetType().GetMethod("StartLevel2",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (m2 != null) { Debug.LogWarning("[ScoreManager] Bridge fallback → StartLevel2() without rest"); m2.Invoke(dir, null); }
                        else Debug.LogWarning("[ScoreManager] Bridge: LevelDirector methods not found");
                    }
                }
                else Debug.LogWarning("[ScoreManager] Bridge: LevelDirector not found");
            }
            catch (System.Exception e) { Debug.LogWarning($"[ScoreManager] Bridge error: {e.Message}"); }
        }
    }

    // ==== Gameplay API
    public void AddScore(int points)
    {
        if (!_sessionRunning) return;
        _currentScore += points;
        if (clampScoreToZero && _currentScore < 0) _currentScore = 0;
        UpdateUI();
        OnBallCaught?.Invoke();
        OnGoodCatch?.Invoke();
        if (popSound && _audio) _audio.PlayOneShot(popSound);
    }

    public void RedBallTouched()
    {
        if (!_sessionRunning) return;
        _currentScore -= Mathf.Abs(redPenalty);
        if (clampScoreToZero && _currentScore < 0) _currentScore = 0;
        UpdateUI();
        OnRedTouched?.Invoke();
        if (popSound && _audio) _audio.PlayOneShot(popSound);
    }

    public void RecordSpawn(int ballId) => spawnTimes[ballId] = Time.time;
    public void RegisterSpawn(int ballId) => RecordSpawn(ballId);
    public void RecordAccuracy(float distance)
    {
        float acc = 1f - Mathf.Clamp01(distance / maxCatchDistance);
        _accuracy.Add(acc);
    }

    public void RecordReactionTime(float reaction) => _reactionTimes.Add(reaction);

    // ==== UI wiring
    private void WireStartButton()
    {
        if (!startButton) return;
        var btn = startButton.GetComponent<Button>();
        if (!btn) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(StartSession);
    }

    private void UpdateUI()
    {
        if (currentScoreText) currentScoreText.text = $"My Score: {_currentScore}";
        if (bestScoreText) bestScoreText.text = $"Best Score: {PlayerPrefs.GetInt("BestScore", 0)}";
        if (timerText) timerText.text = $"Time: {Mathf.Ceil(_timer)}s";
    }

    private void AutoReconnectRefs()
    {
        if (!ballSpawner) ballSpawner = FindObjectOfType<BallSpawnerBallCatch>(true);
        if (!motionLogger) motionLogger = FindObjectOfType<MotionLogger>(true);
        if (!currentScoreText) currentScoreText = Resources.FindObjectsOfTypeAll<TMP_Text>().FirstOrDefault(t => t.name.ToLower().Contains("currentscore"));
        if (!bestScoreText) bestScoreText = Resources.FindObjectsOfTypeAll<TMP_Text>().FirstOrDefault(t => t.name.ToLower().Contains("bestscore"));
        if (!timerText) timerText = Resources.FindObjectsOfTypeAll<TMP_Text>().FirstOrDefault(t => t.name.ToLower().Contains("timer"));
    }

    // ==== Patient settings mapping (через рефлексию — значения могут называться по-разному)
    private static readonly BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public void ApplySettingsFromPatient(object settings)
    {
        level1Duration = Mathf.Clamp(GetInt(settings, new[] { "Level1Duration", "level1DurationSec" }, level1Duration), 5, 3600);
        level2Duration = Mathf.Clamp(GetInt(settings, new[] { "Level2Duration", "level2DurationSec" }, level2Duration), 5, 3600);
        restSeconds = Mathf.Clamp(GetInt(settings, new[] { "RestSeconds", "restTimeSec" }, restSeconds), 0, 600);
        Debug.Log($"[ScoreManager] Durations ← settings: L1={level1Duration}, L2={level2Duration}, Rest={restSeconds}");
    }

    private static int GetInt(object obj, string[] names, int defVal)
    {
        if (obj == null) return defVal;
        foreach (var n in names)
        {
            var f = obj.GetType().GetField(n, BF);
            if (f != null) { try { return Convert.ToInt32(f.GetValue(obj)); } catch { } }
            var p = obj.GetType().GetProperty(n, BF);
            if (p != null && p.CanRead) { try { return Convert.ToInt32(p.GetValue(obj, null)); } catch { } }
        }
        return defVal;
    }
}
