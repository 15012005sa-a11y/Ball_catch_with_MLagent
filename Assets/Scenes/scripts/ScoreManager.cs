// =============================
// REPLACE your existing ScoreManager.cs with this version
// (contains a fallback bridge to force "Rest → Level 2" even if
// the event chain is broken for any reason)
// =============================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class ScoreManager : MonoBehaviour
{
    public UnityEvent OnSessionFinished;                 // UnityEvent без параметров
    public static ScoreManager Instance { get; private set; }

    // Gameplay events
    public event Action OnBallCaught;                    // любой пойманный мяч
    public event Action OnGoodCatch;                     // «правильный» мяч
    public event Action OnRedTouched;                    // красный задет
    public event Action OnMissed;                        // промах

    [Header("Exporter")] public ExcelExporter exporter;

    [Header("UI Elements")] public TMP_Text currentScoreText;
    public TMP_Text bestScoreText;
    public TMP_Text timerText;

    [Header("Session Settings")] public float sessionDuration = 20f;

    [Header("Dependencies")] public BallSpawnerBallCatch ballSpawner;
    public GameObject uiPanel;
    public GameObject startButton;

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
    [SerializeField] private int restSeconds = 60;           // пауза между уровнями
    public int RestSeconds => restSeconds;

    public int currentLevel = 1;

    // Подавить показ меню при ближайшем завершении (для перехода 1→Rest→2)
    [NonSerialized] public bool suppressMenuOnEndOnce = false;

    // runtime
    private readonly List<float> _accuracy = new();
    private readonly List<float> _reactionTimes = new();
    public Dictionary<int, float> spawnTimes = new();
    public int nextBallId = 0;
    private int _currentScore;
    private float _timer;
    private bool _sessionRunning;
    private bool _resetScoreOnStart = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; DontDestroyOnLoad(gameObject);
            _audio = GetComponent<AudioSource>();
            if (!motionLogger) motionLogger = FindObjectOfType<MotionLogger>(true);
        }
        else { Destroy(gameObject); }
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

    // ===== Public helpers =====
    public void SetShowStartButton(bool value) { showStartButtonOnMenu = value; if (!_sessionRunning && startButton) startButton.SetActive(value); }
    public void SetShowGraphButton(bool value) { showGraphButtonOnMenu = value; if (!_sessionRunning && graphButton) graphButton.SetActive(value); }
    public void SetLevel(int level) { currentLevel = Mathf.Clamp(level, 1, 2); }
    public void RegisterMiss() => OnMissed?.Invoke();

    // ===== Session control =====
    public void StartSession() { Debug.Log("[ScoreManager] StartSession()"); StartSessionInternal(resetScore: _resetScoreOnStart); _resetScoreOnStart = false; }
    public void StartSessionKeepScore() { Debug.Log("[ScoreManager] StartSessionKeepScore()"); StartSessionInternal(resetScore: false); }

    // NB: параметр обязан называться именно resetScore — внешние скрипты используют именованный аргумент
    private void StartSessionInternal(bool resetScore)
    {
        Time.timeScale = 1f;
        uiPanel?.SetActive(false); startButton?.SetActive(false);
        var ps = PatientManager.Instance?.Current?.settings; if (ps != null) ApplySettingsFromPatient(ps);
        if (resetScore) _currentScore = 0;
        _timer = (currentLevel == 1) ? level1Duration : level2Duration; sessionDuration = _timer; _sessionRunning = true;
        _accuracy.Clear(); _reactionTimes.Clear(); spawnTimes.Clear(); nextBallId = 0;
        if (!ballSpawner) ballSpawner = FindObjectOfType<BallSpawnerBallCatch>(true);
        if (ballSpawner) ballSpawner.StartSpawning(); else Debug.LogWarning("[ScoreManager] ballSpawner == null");
        if (motionLogger) { motionLogger.StartLogging(); motionLogger.trackArmAngles = true; }
        Debug.Log($"[ScoreManager] Session START → level={currentLevel}, timer={_timer}s");
        UpdateUI();
    }

    private void EndSession()
    {
        _sessionRunning = false;
        if (ballSpawner) ballSpawner.StopSpawning();
        if (motionLogger) { string logName = "Motion_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"); motionLogger.StopLogging(logName); }

        int best = PlayerPrefs.GetInt("BestScore", 0); if (_currentScore > best) { PlayerPrefs.SetInt("BestScore", _currentScore); PlayerPrefs.Save(); }

        bool suppress = suppressMenuOnEndOnce; suppressMenuOnEndOnce = false; // одноразовый
        if (!suppress) { uiPanel?.SetActive(true); if (startButton) startButton.SetActive(showStartButtonOnMenu); if (graphButton) graphButton.SetActive(showGraphButtonOnMenu); }
        else { Debug.Log("[ScoreManager] Menu suppressed (between levels)"); }

        // простые метрики
        float successRate = 0f; if (ballSpawner && ballSpawner.spawnInterval > 0f) successRate = _currentScore / (sessionDuration / ballSpawner.spawnInterval);
        float playTime = Mathf.Max(sessionDuration - _timer, 0f); float avgReaction = _reactionTimes.Count > 0 ? _reactionTimes.Average() : 0f;
        if (exporter != null) { int pid = PatientManager.Instance ? PatientManager.Instance.CurrentPatientID : -1; exporter.ExportSession(pid, _currentScore, successRate, playTime, avgReaction, 0f, 0f); }

        Debug.Log("[ScoreManager] Session END");
        OnSessionFinished?.Invoke(); // обычная цепочка

        // ---- Fallback-bridge: если подавили меню, но второй уровень не стартовал по событию, запускаем сами ----
        if (suppress)
        {
            try
            {
                var dir = FindObjectOfType<LevelDirector>(true);
                if (dir != null)
                {
                    var m = dir.GetType().GetMethod("StartRestThenLevel2External", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (m != null) { Debug.Log("[ScoreManager] Bridge → StartRestThenLevel2External()"); m.Invoke(dir, null); }
                    else
                    {
                        // крайней мерой запустим сразу Level2 без отдыха
                        var m2 = dir.GetType().GetMethod("StartLevel2", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (m2 != null) { Debug.LogWarning("[ScoreManager] Bridge fallback → StartLevel2() without rest"); m2.Invoke(dir, null); }
                        else Debug.LogWarning("[ScoreManager] Bridge: LevelDirector methods not found");
                    }
                }
                else Debug.LogWarning("[ScoreManager] Bridge: LevelDirector not found");
            }
            catch (Exception e) { Debug.LogWarning($"[ScoreManager] Bridge error: {e.Message}"); }
        }
    }

    // ===== Gameplay API =====
    public void AddScore(int points)
    {
        if (!_sessionRunning) return; _currentScore += points; if (clampScoreToZero && _currentScore < 0) _currentScore = 0; UpdateUI();
        OnBallCaught?.Invoke(); OnGoodCatch?.Invoke(); if (popSound && _audio) _audio.PlayOneShot(popSound);
    }

    public void RedBallTouched()
    {
        if (!_sessionRunning) return; _currentScore -= Mathf.Abs(redPenalty); if (clampScoreToZero && _currentScore < 0) _currentScore = 0; UpdateUI();
        OnRedTouched?.Invoke(); if (popSound && _audio) _audio.PlayOneShot(popSound);
    }

    public void RecordSpawn(int ballId) => spawnTimes[ballId] = Time.time;
    public void RegisterSpawn(int ballId) => RecordSpawn(ballId);
    public void RecordAccuracy(float distance) { float acc = 1f - Mathf.Clamp01(distance / maxCatchDistance); _accuracy.Add(acc); }
    public void RecordReactionTime(float reaction) { _reactionTimes.Add(reaction); }

    private void WireStartButton()
    { if (!startButton) return; var btn = startButton.GetComponent<Button>(); if (!btn) return; btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(StartSession); }

    private void UpdateUI()
    { if (currentScoreText) currentScoreText.text = $"My Score: {_currentScore}"; if (bestScoreText) bestScoreText.text = $"Best Score: {PlayerPrefs.GetInt("BestScore", 0)}"; if (timerText) timerText.text = $"Time: {Mathf.Ceil(_timer)}s"; }

    private void AutoReconnectRefs()
    {
        if (!ballSpawner) ballSpawner = FindObjectOfType<BallSpawnerBallCatch>(true);
        if (!motionLogger) motionLogger = FindObjectOfType<MotionLogger>(true);
        if (!currentScoreText) currentScoreText = Resources.FindObjectsOfTypeAll<TMP_Text>().FirstOrDefault(t => t.name.ToLower().Contains("currentscore"));
        if (!bestScoreText) bestScoreText = Resources.FindObjectsOfTypeAll<TMP_Text>().FirstOrDefault(t => t.name.ToLower().Contains("bestscore"));
        if (!timerText) timerText = Resources.FindObjectsOfTypeAll<TMP_Text>().FirstOrDefault(t => t.name.ToLower().Contains("timer"));
    }

    // ===== Patient settings =====
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
            var f = obj.GetType().GetField(n, BF); if (f != null) { try { return Convert.ToInt32(f.GetValue(obj)); } catch { } }
            var p = obj.GetType().GetProperty(n, BF); if (p != null && p.CanRead) { try { return Convert.ToInt32(p.GetValue(obj, null)); } catch { } }
        }
        return defVal;
    }
}

