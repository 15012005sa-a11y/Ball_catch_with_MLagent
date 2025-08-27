using UnityEngine;
using TMPro;
using System.Collections;

public class LevelDirector : MonoBehaviour
{
    [Header("Refs")]
    public BallSpawnerBallCatch spawner;
    public ScoreManager score;
    public CountdownOverlay countdown;

    [Header("Kinect control")]
    [Tooltip("���������� ���� ������ � Kinect (��������, KinectController ��� KinectManager)")]
    public GameObject kinectController;
    public bool stopKinectOnGameEnd = true;
    public bool stopBetweenLevels = false;   // ���� ������ ��������� ����� ��������

    [Header("Level 2 visuals")]
    public Material blueMaterial;
    public Material redMaterial;
    [Range(0f, 1f)] public float redChance = 0.35f;

    [Header("Durations (sec)")]
    public float level1Duration = 20f;
    public float level2Duration = 20f;

    [Header("Voice")]
    public AudioSource voiceSource;
    public AudioClip voiceLevel1;
    public AudioClip voiceLevel2;
    public AudioClip voiceReady;
    public float voiceGapExtra = 0.1f;

    [Header("UI (legacy banner)")]
    public TMP_Text levelBannerText;
    public float prepDelaySeconds = 3f;
    public string readyText = "�������������";
    public float bannerHoldSeconds = 0f;
    public float bannerFadeTime = 0.5f;

    [TextArea] public string level1Banner = "1 �������: �������� ��� ������";
    [TextArea] public string level2Banner = "2 �������: �� �������� �������!";

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

    // === ���������� Kinect ===
    void StartKinectTracking()
    {
        // ������� 1: ������ ����������/������������ ������
        if (kinectController && !kinectController.activeSelf)
            kinectController.SetActive(true);

        // ������� 2 (���� ����������� ����� RFilkov): ����������� ��������� ������
        var km = FindObjectOfType<KinectManager>();   // ���� ������ ��� � ������ ������ �� ����������
        if (km != null)
        {
            try { km.StartKinect(); } catch { /* ����� ����� ������������� � ��� ��������� */ }
        }
    }

    void StopKinectTracking()
    {
        // ������� 1: ��������� ������ � Kinect
        var km = FindObjectOfType<KinectManager>();
        if (km != null) { try { km.StopKinect(); } catch { } }
    }

    // === ���� ������ 1 ===
    public void StartLevel1()
    {
        StartKinectTracking();    // ��������, ��� ������� �������

        currentLevel = 1;
        if (spawner) spawner.useColors = false;

        score.SetShowStartButton(false);
        score.SetShowGraphButton(false);
        score.sessionDuration = level1Duration;

        SpeakReadyThenLevel(voiceLevel1);

        StartCoroutine(PrepThen(() => score.StartSession(), level1Banner));
    }

    // === ���� ������ 2 ===
    void StartLevel2()
    {
        if (stopBetweenLevels) StopKinectTracking();   // �� ������� � ��������� ����� ��������
        StartKinectTracking();                         // � ����� �������� ����� ������ �������

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

        SpeakReadyThenLevel(voiceLevel2);

        StartCoroutine(PrepThen(() => score.StartSessionKeepScore(), level2Banner));
    }

    // === �����: ������� ���������������, ����� �������� �� ===
    void SpeakReadyThenLevel(AudioClip levelClip)
    {
        PlayVoice(voiceReady);
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

    // === ���������� � ������ ������ ===
    IEnumerator PrepThen(System.Action startAction, string title)
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

    // === ���������� ������ ===
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

            if (stopKinectOnGameEnd)
                StopKinectTracking();   // <- ����� ����� Kinect ����� ����� ����
        }
    }
}
