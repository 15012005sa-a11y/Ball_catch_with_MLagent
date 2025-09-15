using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using TMPro;

[DefaultExecutionOrder(-40)]
public class LevelDirector : MonoBehaviour
{
    public event Action OnGameStarted;
    public event Action OnGameFinished;

    [Header("Finish UI")]
    [Tooltip("Кнопка Домой, которую нужно показать после завершения всех уровней")]
    public CanvasGroup homeButtonGroup; // повесьте сюда CanvasGroup объекта HomeButton

    [Tooltip("Если нужно дополнительно активировать сам объект кнопки")]
    public GameObject homeButtonObject; // можно оставить пустым — будет взят из homeButtonGroup
    [Header("Game flow")]
    public bool twoLevels = true;

    private int _currentLevel = 0; // 0 – не идёт, 1 – уровень 1, 2 – уровень 2
    private bool _gameStarted;

    [Header("Refs")]
    public BallSpawnerBallCatch spawner;
    public ScoreManager score;
    public CountdownOverlay countdown;

    [Header("Rest UI (между уровнями)")]
    [Tooltip("Приоритет: это значение. Если 0 – пробуем взять из ScoreManager.RestSeconds")]
    public float restBetweenLevelsSeconds = 3f;
    public TMP_Text restText;
    public CanvasGroup restGroup;
    public float restFadeTime = 0.25f;

    [Header("Kinect")]
    public GameObject kinectController;
    public bool stopKinectOnGameEnd = true;
    public bool stopBetweenLevels = false;

    [Header("Level 2 visuals")]
    public Material blueMaterial;
    public Material redMaterial;
    [Range(0f, 1f)] public float redChance = 0.35f;

    [Header("Voice")]
    public AudioSource voiceSource;
    public AudioClip voiceLevel1, voiceLevel2, voiceReady;
    public float voiceGapExtra = 0.1f;

    [Header("Banner")]
    public TMP_Text levelBannerText;
    public float prepDelaySeconds = 3f;
    public string readyText = "Приготовьтесь";
    public float bannerHoldSeconds = 0f;
    public float bannerFadeTime = 0.35f;
    [TextArea] public string level1Banner = "1 уровень: разбивайте все шарики";
    [TextArea] public string level2Banner = "2 уровень: не разбивайте красных!";

    [Header("Durations (legacy external config)")]
    public int level1Duration = 180;
    public int level2Duration = 120;

    private static readonly BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    // Публичный хук, который может дернуть ScoreManager, если по событию не начался отдых/Level2
    public void StartRestThenLevel2External()
    {
        StopAllCoroutines();
        var mi = GetType().GetMethod("RestThenStartLevel2", BindingFlags.Instance | BindingFlags.NonPublic);
        if (mi != null)
        {
            var ie = mi.Invoke(this, null) as IEnumerator;
            if (ie != null) StartCoroutine(ie);
        }
        else
        {
            // крайней мерой — сразу второй уровень
            var m2 = GetType().GetMethod("StartLevel2", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m2 != null) m2.Invoke(this, null);
        }
    }

    private void Awake()
    {
        ReconnectRefs();
        if (levelBannerText)
        {
            var c = levelBannerText.color; c.a = 0f; levelBannerText.color = c;
            levelBannerText.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        ReconnectRefs();
        if (score != null)
        {
            score.OnSessionFinished.RemoveListener(OnSessionFinished);
            score.OnSessionFinished.AddListener(OnSessionFinished);
        }
    }

    private void OnDisable()
    {
        if (score != null) score.OnSessionFinished.RemoveListener(OnSessionFinished);
    }

    private void ReconnectRefs()
    {
        if (!spawner) spawner = FindObjectOfType<BallSpawnerBallCatch>(true);
        if (!score) score = FindObjectOfType<ScoreManager>(true);
    }

    // ====== ПУСК ИГРЫ ======
    public void StartGameplay()
    {
        if (!_gameStarted)
        {
            _gameStarted = true;
            OnGameStarted?.Invoke();
        }
        StartLevel1();
    }

    public void StartLevel1()
    {
        ReconnectRefs();
        if (!_gameStarted) { _gameStarted = true; OnGameStarted?.Invoke(); }

        StartKinectTracking();
        _currentLevel = 1;

        if (spawner) spawner.useColors = false;

        // Между 1 и 2 уровнем меню не нужно
        if (score && twoLevels) score.suppressMenuOnEndOnce = true;

        SpeakReadyThenLevel(voiceLevel1);
        StartCoroutine(PrepThen(() =>
        {
            PushDurationsToScoreManager();
            SafeStart(level: 1, keepScore: false);
        }, level1Banner));
    }

    private void StartLevel2()
    {
        if (!twoLevels) { FinishAll(); return; }

        if (stopBetweenLevels) StopKinectTracking();
        StartKinectTracking();
        _currentLevel = 2;

        if (spawner)
        {
            spawner.useColors = true;
            spawner.redChance = redChance;
            spawner.blueMaterial = blueMaterial;
            spawner.redMaterial = redMaterial;
        }

        // На второй уровень меню тоже не должно появляться до конца
        if (score) score.suppressMenuOnEndOnce = false; // после 2-го пусть меню появится

        SpeakReadyThenLevel(voiceLevel2);
        StartCoroutine(PrepThen(() =>
        {
            PushDurationsToScoreManager();
            SafeStart(level: 2, keepScore: true);
        }, level2Banner));
    }

    private void SafeStart(int level, bool keepScore)
    {
        ReconnectRefs();
        if (!score) { Debug.LogError("[LevelDirector] ScoreManager not found"); return; }
        score.SetLevel(level);
        if (keepScore) score.StartSessionKeepScore(); else score.StartSession();
    }

    // ====== Завершение уровня из ScoreManager ======
    private void OnSessionFinished()
    {
        if (_currentLevel == 1 && twoLevels)
        {
            // Первый уровень завершён — запускаем отдых → потом 2-й
            StopAllCoroutines();
            StartCoroutine(RestThenStartLevel2());
        }
        else
        {
            FinishAll();
        }
    }

    private IEnumerator RestThenStartLevel2()
    {
        // 1) Берём из инспектора, 2) если 0 — из ScoreManager.RestSeconds
        int restSec = Mathf.Max(0, Mathf.RoundToInt(restBetweenLevelsSeconds));
        if (restSec <= 0 && score != null && score.RestSeconds > 0)
            restSec = score.RestSeconds;

        if (restSec > 0)
        {
            if (restText)
            {
                if (restGroup) yield return StartCoroutine(FadeGroup(restGroup, 0f, 1f, restFadeTime));
                restText.gameObject.SetActive(true);
                for (int s = restSec; s > 0; s--)
                {
                    restText.text = $"Отдых... {s} сек";
                    yield return new WaitForSeconds(1f);
                }
                if (restGroup) yield return StartCoroutine(FadeGroup(restGroup, 1f, 0f, restFadeTime));
                restText.gameObject.SetActive(false);
            }
            else
            {
                yield return new WaitForSeconds(restSec);
            }
        }

        StartLevel2();
    }

    // Вызывается в конце 2-го уровня (или в конце единственного уровня)
    private void ShowHomeButton()
    {
        var go = homeButtonObject ? homeButtonObject : (homeButtonGroup ? homeButtonGroup.gameObject : null);
        if (!go) return;


        go.SetActive(true);
        if (homeButtonGroup)
        {
            homeButtonGroup.alpha = 1f;
            homeButtonGroup.interactable = true;
            homeButtonGroup.blocksRaycasts = true;
        }
    }

    // Вызовите из FinishAll()
    private void FinishAll()
    {
        if (score)
        {
            score.SetShowStartButton(false); // скрыть кнопку старт, если не нужна
            score.SetShowGraphButton(true);
        }
        _currentLevel = 0;
        if (stopKinectOnGameEnd) StopKinectTracking();
        _gameStarted = false;
        OnGameFinished?.Invoke();


        // Показать HomeButton
        ShowHomeButton();
    }

// ====== Helpers ======
private void StartKinectTracking()
    {
        if (kinectController && !kinectController.activeSelf)
            kinectController.SetActive(true);
    }

    private void StopKinectTracking()
    {
        if (kinectController && kinectController.activeSelf)
            kinectController.SetActive(false);
    }

    private void SpeakReadyThenLevel(AudioClip levelClip)
    {
        PlayVoice(voiceReady);
        float delay = (voiceReady ? voiceReady.length : 0.4f) + voiceGapExtra;
        PlayVoice(levelClip, delay);
    }

    private void PlayVoice(AudioClip clip, float delay = 0f)
    {
        if (!voiceSource || !clip) return;
        if (delay <= 0f) voiceSource.PlayOneShot(clip);
        else StartCoroutine(PlayDelayed(clip, delay));
    }

    private IEnumerator PlayDelayed(AudioClip c, float d)
    {
        yield return new WaitForSeconds(d);
        if (voiceSource && c) voiceSource.PlayOneShot(c);
    }

    private IEnumerator PrepThen(Action startAction, string title)
    {
        int ticks = Mathf.Max(1, Mathf.CeilToInt(prepDelaySeconds));

        if (countdown != null)
        {
            yield return countdown.Run($"{title}\n{readyText}", ticks);
        }
        else
        {
            if (levelBannerText)
            {
                levelBannerText.gameObject.SetActive(true);
                yield return StartCoroutine(FadeText(0f, 1f, bannerFadeTime));

                string head = string.IsNullOrEmpty(readyText) ? title : (title + "\n" + readyText);
                levelBannerText.text = head;

                if (bannerHoldSeconds > 0f)
                    yield return new WaitForSeconds(bannerHoldSeconds);

                for (int s = ticks; s > 0; s--)
                {
                    levelBannerText.text = head + "\n" + s.ToString();
                    yield return new WaitForSeconds(1f);
                }

                yield return StartCoroutine(FadeText(1f, 0f, bannerFadeTime));
                levelBannerText.gameObject.SetActive(false);
            }
            else
            {
                yield return new WaitForSeconds(prepDelaySeconds);
            }
        }

        startAction?.Invoke();
    }

    private IEnumerator FadeText(float from, float to, float duration)
    {
        if (!levelBannerText || duration <= 0f) yield break;
        Color c = levelBannerText.color; c.a = from; levelBannerText.color = c;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, t / duration);
            levelBannerText.color = c;
            yield return null;
        }
        c.a = to; levelBannerText.color = c;
    }

    private IEnumerator FadeGroup(CanvasGroup g, float from, float to, float dur)
    {
        if (!g) yield break;
        g.gameObject.SetActive(true);
        g.alpha = from;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            g.alpha = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        g.alpha = to;
        if (to <= 0f) g.gameObject.SetActive(false);
    }

    private void PushDurationsToScoreManager()
    {
        if (!score) return;
        try
        {
            var t = score.GetType();
            var f1 = t.GetField("level1Duration", BF);
            var f2 = t.GetField("level2Duration", BF);
            if (f1 != null) f1.SetValue(score, Mathf.Clamp(level1Duration, 5, 3600));
            if (f2 != null) f2.SetValue(score, Mathf.Clamp(level2Duration, 5, 3600));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LevelDirector] Push durations failed: {e.Message}");
        }
    }
}
