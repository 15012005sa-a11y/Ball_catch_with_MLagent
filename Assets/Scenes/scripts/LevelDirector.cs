using UnityEngine;
using TMPro;
using System.Collections;

public class LevelDirector : MonoBehaviour
{
    [Header("Refs")]
    public BallSpawnerBallCatch spawner;   // перетащи сюда спавнер
    public ScoreManager score;             // перетащи ScoreManager (DontDestroyOnLoad)

    [Header("Level 2 visuals")]
    public Material blueMaterial;
    public Material redMaterial;
    [Range(0f, 1f)] public float redChance = 0.35f;

    [Header("Durations (sec)")]
    public float level1Duration = 60f;
    public float level2Duration = 60f;

    [Header("UI")]
    public GameObject graphButton;         // опционально
    [Tooltip("TMP_Text, который показывает надпись уровня. ДОЛЖЕН быть за пределами uiPanel ScoreManager.")]
    public TMP_Text levelBannerText;

    [Tooltip("Сколько секунд держать баннер на экране (без учёта fade)")]
    public float bannerSeconds = 3f;
    [Tooltip("Длительность плавного появления/исчезновения")]
    public float bannerFadeTime = 1f;

    [TextArea] public string level1Banner = "1 уровень: разбивай все шарики";
    [TextArea] public string level2Banner = "2 уровень: не разбивай красных!";

    private int currentLevel = 0;
    private Coroutine bannerRoutine;

    void Awake()
    {
        if (!spawner) spawner = FindObjectOfType<BallSpawnerBallCatch>();
        if (!score) score = FindObjectOfType<ScoreManager>();

        // гарантируем, что баннер стартует невидимым
        if (levelBannerText)
        {
            var c = levelBannerText.color;
            c.a = 0f;
            levelBannerText.color = c;
            levelBannerText.gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        if (score) score.OnSessionFinished.AddListener(OnSessionFinished);
    }
    void OnDisable()
    {
        if (score) score.OnSessionFinished.RemoveListener(OnSessionFinished);
    }

    // === ПУСК УРОВНЯ 1 (привяжи эту функцию к Button_StartGame) ===
    public void StartLevel1()
    {
        currentLevel = 1;

        if (spawner)
        {
            spawner.useColors = false; // без цветных правил на уровне 1
        }

        if (score)
        {
            score.SetShowStartButton(false);
            score.SetShowGraphButton(false);
            score.sessionDuration = level1Duration;

            ShowLevelBanner(level1Banner);   // плавный баннер
            score.StartSession();            // первый запуск обнуляет счёт только один раз
        }
    }

    // === ПУСК УРОВНЯ 2 ===
    public void StartLevel2()
    {
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

            ShowLevelBanner(level2Banner);   // плавный баннер
            score.StartSessionKeepScore();   // важно: сохраняем общий счёт
        }
    }

    // === завершение сессии от ScoreManager ===
    private void OnSessionFinished()
    {
        if (currentLevel == 1)
        {
            // сразу запускаем 2-й уровень
            StartLevel2();
        }
        else
        {
            // оба уровня пройдены — вернём кнопки/график
            if (score)
            {
                score.SetShowStartButton(true);
                score.SetShowGraphButton(true);
            }
            Debug.Log("[LevelDirector] Все уровни завершены.");
        }
    }

    // === Баннер уровня с fade-in / hold / fade-out ===
    private void ShowLevelBanner(string text)
    {
        if (!levelBannerText) return;

        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(BannerRoutine(text, bannerSeconds, bannerFadeTime));
    }

    private IEnumerator BannerRoutine(string msg, float holdSeconds, float fadeSeconds)
    {
        levelBannerText.text = msg;
        levelBannerText.gameObject.SetActive(true);

        Color c = levelBannerText.color;

        // fade-in
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, t / fadeSeconds);
            levelBannerText.color = c;
            yield return null;
        }
        c.a = 1f; levelBannerText.color = c;

        // hold
        yield return new WaitForSeconds(holdSeconds);

        // fade-out
        t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / fadeSeconds);
            levelBannerText.color = c;
            yield return null;
        }
        c.a = 0f; levelBannerText.color = c;

        levelBannerText.gameObject.SetActive(false);
        bannerRoutine = null;
    }
}
