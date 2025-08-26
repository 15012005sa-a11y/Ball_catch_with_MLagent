using UnityEngine;
using TMPro;
using System.Collections;

public class LevelDirector : MonoBehaviour
{
    [Header("Refs")]
    public BallSpawnerBallCatch spawner;
    public ScoreManager score;

    // Новый красивый отсчёт (опц.)
    public CountdownOverlay countdown;

    [Header("Level 2 visuals")]
    public Material blueMaterial;
    public Material redMaterial;
    [Range(0f, 1f)] public float redChance = 0.35f;

    [Header("Durations (sec)")]
    public float level1Duration = 15f;
    public float level2Duration = 15f;

    [Header("Voice")]
    public AudioSource voiceSource;       // активный AudioSource под Canvas (Play On Awake = off, Loop = off)
    public AudioClip voiceLevel1;
    public AudioClip voiceLevel2;
    public AudioClip voiceReady;
    [Tooltip("Доп. пауза между «Приготовьтесь» и «Уровень …»")]
    public float voiceGapExtra = 0.1f;

    // --- ЛЕГАСИ баннер (если countdown не задан) ---
    [Header("UI (legacy banner)")]
    public TMP_Text levelBannerText;          // баннерный текст (вне uiPanel!)
    [Tooltip("Сколько секунд подождать перед стартом уровня (отсчёт 3..2..1)")]
    public float prepDelaySeconds = 3f;
    [Tooltip("Текст перед отсчётом")]
    public string readyText = "Приготовьтесь";
    [Tooltip("Держать баннер N сек до начала отсчёта")]
    public float bannerHoldSeconds = 0f;
    public float bannerFadeTime = 0.5f;

    [TextArea] public string level1Banner = "1 уровень: разбивай все шарики";
    [TextArea] public string level2Banner = "2 уровень: не разбивай красных!";

    private int currentLevel = 0;

    void Awake()
    {
        if (!spawner) spawner = FindObjectOfType<BallSpawnerBallCatch>();
        if (!score) score = FindObjectOfType<ScoreManager>();

        if (levelBannerText)
        {
            var c = levelBannerText.color; c.a = 0f; levelBannerText.color = c;
            levelBannerText.gameObject.SetActive(false);
        }
    }

    void OnEnable() { if (score) score.OnSessionFinished.AddListener(OnSessionFinished); }
    void OnDisable() { if (score) score.OnSessionFinished.RemoveListener(OnSessionFinished); }

    // Привяжи к кнопке "Начать"
    public void StartLevel1()
    {
        currentLevel = 1;
        if (spawner) spawner.useColors = false;

        score.SetShowStartButton(false);
        score.SetShowGraphButton(false);
        score.sessionDuration = level1Duration;

        // Сначала «Приготовьтесь», потом «1 уровень»
        SpeakReadyThenLevel(voiceLevel1);

        StartCoroutine(PrepThen(() => score.StartSession(), level1Banner));
    }

    void StartLevel2()
    {
        currentLevel = 2;
        if (spawner)
        {
            spawner.useColors = true;
            spawner.redChance = redChance;
            spawner.blueMaterial = blueMaterial;
            spawner.redMaterial = redMaterial;
        }

        score.SetShowStartButton(false);
        score.SetShowGraphButton(false);
        score.sessionDuration = level2Duration;

        // Сначала «Приготовьтесь», потом «2 уровень»
        SpeakReadyThenLevel(voiceLevel2);

        StartCoroutine(PrepThen(() => score.StartSessionKeepScore(), level2Banner));
    }

    // --- Голосовая последовательность ---
    void SpeakReadyThenLevel(AudioClip levelClip)
    {
        // 1) «Приготовьтесь»
        PlayVoice(voiceReady);

        // 2) Через длину voiceReady + небольшой зазор — «Уровень …»
        float delay = (voiceReady != null ? voiceReady.length : 0.4f) + voiceGapExtra;
        PlayVoice(levelClip, delay);
    }

    void PlayVoice(AudioClip clip, float delay = 0f)
    {
        if (voiceSource == null || clip == null) return;
        if (delay <= 0f) voiceSource.PlayOneShot(clip);
        else StartCoroutine(PlayDelayed(clip, delay));
    }

    IEnumerator PlayDelayed(AudioClip c, float d)
    {
        yield return new WaitForSeconds(d);
        if (voiceSource != null && c != null) voiceSource.PlayOneShot(c);
    }

    // Универсальная подготовка: overlay ИЛИ legacy баннер
    IEnumerator PrepThen(System.Action startAction, string title)
    {
        int ticks = Mathf.Max(1, Mathf.CeilToInt(prepDelaySeconds));

        if (countdown != null)
        {
            // Красивый отсчёт 3..2..1..GO!
            yield return countdown.Run($"{title}\n{readyText}", ticks);
        }
        else
        {
            // Старый баннер с fade и текстом
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

    private void OnSessionFinished()
    {
        if (currentLevel == 1)
        {
            StartLevel2();
        }
        else
        {
            score.SetShowStartButton(true);
            score.SetShowGraphButton(true);
            currentLevel = 0;
        }
    }
}
