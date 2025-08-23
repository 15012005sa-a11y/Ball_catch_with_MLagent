using UnityEngine;
using TMPro;
using System.Collections;

public class LevelDirector : MonoBehaviour
{
    [Header("Refs")]
    public BallSpawnerBallCatch spawner;   // �������� ���� �������
    public ScoreManager score;             // �������� ScoreManager (DontDestroyOnLoad)

    [Header("Level 2 visuals")]
    public Material blueMaterial;
    public Material redMaterial;
    [Range(0f, 1f)] public float redChance = 0.35f;

    [Header("Durations (sec)")]
    public float level1Duration = 60f;
    public float level2Duration = 60f;

    [Header("UI")]
    public GameObject graphButton;         // �����������
    [Tooltip("TMP_Text, ������� ���������� ������� ������. ������ ���� �� ��������� uiPanel ScoreManager.")]
    public TMP_Text levelBannerText;

    [Tooltip("������� ������ ������� ������ �� ������ (��� ����� fade)")]
    public float bannerSeconds = 3f;
    [Tooltip("������������ �������� ���������/������������")]
    public float bannerFadeTime = 1f;

    [TextArea] public string level1Banner = "1 �������: �������� ��� ������";
    [TextArea] public string level2Banner = "2 �������: �� �������� �������!";

    private int currentLevel = 0;
    private Coroutine bannerRoutine;

    void Awake()
    {
        if (!spawner) spawner = FindObjectOfType<BallSpawnerBallCatch>();
        if (!score) score = FindObjectOfType<ScoreManager>();

        // �����������, ��� ������ �������� ���������
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

    // === ���� ������ 1 (������� ��� ������� � Button_StartGame) ===
    public void StartLevel1()
    {
        currentLevel = 1;

        if (spawner)
        {
            spawner.useColors = false; // ��� ������� ������ �� ������ 1
        }

        if (score)
        {
            score.SetShowStartButton(false);
            score.SetShowGraphButton(false);
            score.sessionDuration = level1Duration;

            ShowLevelBanner(level1Banner);   // ������� ������
            score.StartSession();            // ������ ������ �������� ���� ������ ���� ���
        }
    }

    // === ���� ������ 2 ===
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

            ShowLevelBanner(level2Banner);   // ������� ������
            score.StartSessionKeepScore();   // �����: ��������� ����� ����
        }
    }

    // === ���������� ������ �� ScoreManager ===
    private void OnSessionFinished()
    {
        if (currentLevel == 1)
        {
            // ����� ��������� 2-� �������
            StartLevel2();
        }
        else
        {
            // ��� ������ �������� � ����� ������/������
            if (score)
            {
                score.SetShowStartButton(true);
                score.SetShowGraphButton(true);
            }
            Debug.Log("[LevelDirector] ��� ������ ���������.");
        }
    }

    // === ������ ������ � fade-in / hold / fade-out ===
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
