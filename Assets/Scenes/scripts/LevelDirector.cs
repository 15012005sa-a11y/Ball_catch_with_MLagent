using System;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using CoachEnv;

[DefaultExecutionOrder(-40)]
public class LevelDirector : MonoBehaviour
{
    // События “игра началась/закончилась” — по желанию можно использовать во внешнем UI
    public event Action OnGameStarted;
    public event Action OnGameFinished;

    [Header("Episode by windows")]
    public int windowsPerEpisode = 30;   // K

    private int _windowsInEpisode = 0;
    [Header("Agent (необязательно)")]
    public CoachAgent coach;   // если используете — перетащите сюда

    [Header("Game flow")]
    public bool twoLevels = true;         // играть 2 уровня подряд
    private int _currentLevel = 0;        // 0 – нет игры, 1 – L1, 2 – L2
    private bool _gameStarted;

    [Header("Refs")]
    public BallSpawnerBallCatch spawner;  // спавнер шаров
    public ScoreManager score;            // ScoreManager сцены
    public CountdownOverlay countdown;    // красивый “готовьтесь” (можно пусто)

    [Header("Durations (сек)")]
    public int level1Duration = 500;
    public int level2Duration = 600;

    [Header("Level-2 visuals")]
    public Material blueMaterial;
    public Material redMaterial;
    [Range(0f, 1f)] public float redChance = 0.35f;

    [Header("Rest (между уровнями)")]
    [Tooltip("Если 0 — попробуем взять ScoreManager.RestSeconds")]
    public float restBetweenLevelsSeconds = 3f;
    public TMP_Text restText;
    public CanvasGroup restGroup;
    public float restFadeTime = 0.25f;

    [Header("Kinect")]
    public GameObject kinectController;
    public bool stopKinectOnGameEnd = true;
    public bool stopBetweenLevels = false;

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

    [Header("Finish UI")]
    public CanvasGroup homeButtonGroup;   // CanvasGroup на кнопке “Домой”
    public GameObject homeButtonObject;   // можно оставить пустым — возьмём из CanvasGroup

    private static readonly BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    // ---------- LIFECYCLE ----------

    private void Awake()
    {
        ReconnectRefs();

        if (levelBannerText)
        {
            var c = levelBannerText.color;
            c.a = 0f;
            levelBannerText.color = c;
            levelBannerText.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        ReconnectRefs();

        // подстрахуемся: если ссылка не проставлена в инспекторе — возьмём синглтон
        if (score == null) score = ScoreManager.Instance;

        if (score != null)
        {
            score.OnWindowFinished += HandleWindowFinished;
            score.EpisodeFinished += OnEpisodeFinished;
        }
        else
        {
            Debug.LogError("[LevelDirector] ScoreManager not found — установите ссылку в инспекторе");
        }
    }

    private void OnDisable()
    {
        if (score != null)
        {
            score.OnWindowFinished -= HandleWindowFinished;
            score.EpisodeFinished -= OnEpisodeFinished;
        }
    }

    // Обработчик окна (сигнатура должна соответствовать Action<WindowMetrics>)
    private void HandleWindowFinished(WindowMetrics m)
    {
        // ваша логика награды (как уже сделали)
        float sweet = 1f - Mathf.Min(Mathf.Abs(m.hitRate - 0.80f) / 0.20f, 1f);
        float pace = 1f - Mathf.Min(Mathf.Abs(m.throughputPerMin - 30f) / 30f, 1f);
        float reward = 1.2f * sweet + 0.3f * pace - 1f * Mathf.Clamp01(m.trunkCompFrac);
        coach?.AddReward(reward);
        coach?.RequestDecision();

        // считаем окна в текущем эпизоде
        _windowsInEpisode++;
        if (_windowsInEpisode >= Mathf.Max(1, windowsPerEpisode))
        {
            // завершаем эпизод и сразу перезапускаем
            coach?.EndEpisode();
            _windowsInEpisode = 0;

            // мягкий рестарт уровня без показа меню
            if (score != null)
            {
                score.suppressMenuOnEndOnce = true;   // не показывать UI
                score.StartSessionKeepScore();        // новый отрезок без сброса счёта (или StartSession(), если нужно)
            }
        }
    }

    private void ReconnectRefs()
    {
        if (!spawner) spawner = FindObjectOfType<BallSpawnerBallCatch>(true);
        if (!score) score = FindObjectOfType<ScoreManager>(true);
    }

    // ---------- ПУСК ИГРЫ ----------

    public void StartGameplay()
    {
        if (!_gameStarted)
        {
            _gameStarted = true;
            OnGameStarted?.Invoke();
        }

        // спрятать домашнюю кнопку на старте
        var hb = FindObjectOfType<HomeButtonAfterFinalSession>(true);
        hb?.Hide();

        StartLevel1();
    }

    public void StartLevel1()
    {
        ReconnectRefs();
        if (!_gameStarted) { _gameStarted = true; OnGameStarted?.Invoke(); }

        StartKinectTracking();
        _currentLevel = 1;

        if (spawner)
        {
            spawner.useColors = false; // в L1 цвета не нужны
        }

        // Между L1 и L2 меню не показываем
        if (score && twoLevels) score.suppressMenuOnEndOnce = true;

        SpeakReadyThenLevel(voiceLevel1);

        StartCoroutine(PrepThen(() =>
        {
            PushDurationsToScoreManager();        // передадим длительности в ScoreManager
            SafeStart(level: 1, keepScore: false); // L1 — счёт с нуля
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

        // После L2 пусть меню появится
        if (score) score.suppressMenuOnEndOnce = false;

        SpeakReadyThenLevel(voiceLevel2);

        StartCoroutine(PrepThen(() =>
        {
            PushDurationsToScoreManager();
            SafeStart(level: 2, keepScore: true); // L2 — оставляем счёт (продолжаем)
        }, level2Banner));
    }

    // ---------- CALLBACKS от ScoreManager ----------

    // Весь эпизод закончился — закрываем эпизод у агента
    private void OnEpisodeFinished()
    {
        if (coach != null) coach.EndEpisode();

        if (_currentLevel == 1 && twoLevels)
        {
            // переход к L2
            StopAllCoroutines();
            StartCoroutine(RestThenStartLevel2());
        }
        else
        {
            // конец всей игры
            FinishAll();
        }
    }

    // ---------- ВСПОМОГАТЕЛЬНОЕ ----------

    private void SafeStart(int level, bool keepScore)
    {
        ReconnectRefs();
        if (!score)
        {
            Debug.LogError("[LevelDirector] ScoreManager not found");
            return;
        }

        score.SetLevel(level);
        if (keepScore) score.StartSessionKeepScore();
        else score.StartSession();
    }

    // Публичный мост для ScoreManager (рефлексия вызывает этот метод)
    public void StartRestThenLevel2External()
    {
        StopAllCoroutines();
        StartCoroutine(RestThenStartLevel2());
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

    private void FinishAll()
    {
        // показать кнопку Домой
        ShowHomeButton();

        if (score)
        {
            score.SetShowStartButton(false);
            score.SetShowGraphButton(true);
        }

        _currentLevel = 0;
        _gameStarted = false;

        if (stopKinectOnGameEnd) StopKinectTracking();

        OnGameFinished?.Invoke();
    }

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
