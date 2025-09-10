using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class LevelDirector : MonoBehaviour
{
    public event Action OnGameStarted;
    public event Action OnGameFinished;

    [Header("Game flow")]
    public bool twoLevels = true;                // если false — завершаем сразу после 1 уровня
    private int currentLevel = 0;
    private bool gameStarted = false;

    [Header("Refs")]
    public BallSpawnerBallCatch spawner;
    public ScoreManager score;
    public CountdownOverlay countdown;

    [Header("Rest (между уровнями)")]
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

    [Header("Durations (sec)")]
    public float level1Duration = 15f;
    public float level2Duration = 15f;

    [Header("Voice")]
    public AudioSource voiceSource;
    public AudioClip voiceLevel1, voiceLevel2, voiceReady;
    public float voiceGapExtra = 0.1f;

    [Header("Banner")]
    public TMP_Text levelBannerText;
    public float prepDelaySeconds = 3f;
    public string readyText = "Приготовьтесь";
    public float bannerHoldSeconds = 0f;
    public float bannerFadeTime = 0.5f;
    [TextArea] public string level1Banner = "1 уровень: разбивай все шарики";
    [TextArea] public string level2Banner = "2 уровень: не разбивай красных!";

    private void Awake()
    {
        if (!spawner) spawner = FindObjectOfType<BallSpawnerBallCatch>();
        if (!score) score = FindObjectOfType<ScoreManager>();

        if (levelBannerText)
        {
            var c = levelBannerText.color; c.a = 0f; levelBannerText.color = c;
            levelBannerText.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (score != null)
            score.OnSessionFinished.AddListener(OnSessionFinished);
    }

    private void OnDisable()
    {
        if (score != null)
            score.OnSessionFinished.RemoveListener(OnSessionFinished);
    }

    // ==== Запуск из кнопки «Начать игру» ====
    public void StartGameplay()
    {
        if (!gameStarted)
        {
            gameStarted = true;
            OnGameStarted?.Invoke(); // спрячем «Главная»
        }
        StartLevel1();
    }

    public void StartLevel1()
    {
        if (!gameStarted)
        {
            gameStarted = true;
            OnGameStarted?.Invoke();
        }

        StartKinectTracking();
        currentLevel = 1;

        if (spawner) spawner.useColors = false;
        if (score)
        {
            score.SetShowStartButton(false);
            score.SetShowGraphButton(false);
            score.sessionDuration = level1Duration;
        }

        SpeakReadyThenLevel(voiceLevel1);
        StartCoroutine(PrepThen(() => score?.StartSession(), level1Banner));
    }

    private void StartLevel2()
    {
        if (!twoLevels)
        {
            // Вообще не запускаем второй уровень
            FinishAll();
            return;
        }

        if (stopBetweenLevels) StopKinectTracking();
        StartKinectTracking();

        currentLevel = 2;

        if (spawner)
        {
            spawner.useColors = true;
            spawner.redChance = redChance;
            spawner.blueMaterial = blueMaterial;
            spawner.redMaterial = redMaterial;
        }

        if (score)
        {
            score.SetShowStartButton(false);
            score.SetShowGraphButton(false);
            score.sessionDuration = level2Duration;
        }

        SpeakReadyThenLevel(voiceLevel2);
        StartCoroutine(PrepThen(() => score?.StartSessionKeepScore(), level2Banner));
    }

    // ==== Завершения сессий от ScoreManager ====
    private void OnSessionFinished()
    {
        if (currentLevel == 1 && twoLevels)
        {
            StartCoroutine(WaitAndStartLevel2());
        }
        else
        {
            FinishAll();
        }
    }

    private void FinishAll()
    {
        if (score)
        {
            score.SetShowStartButton(true);
            score.SetShowGraphButton(true);
        }
        currentLevel = 0;

        if (stopKinectOnGameEnd) StopKinectTracking();

        gameStarted = false;
        OnGameFinished?.Invoke(); // показать «Главная»
    }

    // ==== Вспомогательное ====
    private void StartKinectTracking()
    {
        if (kinectController && !kinectController.activeSelf)
            kinectController.SetActive(true);

        if (kinectController)
        {
            var comp = kinectController.GetComponent("KinectManager");
            if (comp != null)
            {
                try { comp.GetType().GetMethod("StartKinect")?.Invoke(comp, null); } catch { }
            }
        }
    }

    private void StopKinectTracking()
    {
        if (kinectController)
        {
            var comp = kinectController.GetComponent("KinectManager");
            if (comp != null)
            {
                try { comp.GetType().GetMethod("StopKinect")?.Invoke(comp, null); } catch { }
            }
            // kinectController.SetActive(false); // если нужно
        }
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

    private IEnumerator WaitAndStartLevel2()
    {
        int ticks = Mathf.Max(1, Mathf.CeilToInt(restBetweenLevelsSeconds));

        if (restText)
        {
            if (restGroup) yield return StartCoroutine(FadeGroup(restGroup, 0f, 1f, restFadeTime));
            restText.gameObject.SetActive(true);

            for (int s = ticks; s > 0; s--)
            {
                restText.text = $"Отдых... {s} сек";
                yield return new WaitForSeconds(1f);
            }

            if (restGroup) yield return StartCoroutine(FadeGroup(restGroup, 1f, 0f, restFadeTime));
            restText.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(restBetweenLevelsSeconds);
        }

        StartLevel2();
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
}
