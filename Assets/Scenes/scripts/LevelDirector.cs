using UnityEngine;

public class LevelDirector : MonoBehaviour
{
    [Header("Refs")]
    public BallSpawnerBallCatch spawner;  // ���������� ���� ��� �������
    public ScoreManager score;            // ���������� ��� ScoreManager (���, ��� DontDestroyOnLoad)

    [Header("Level 2 visuals")]
    public Material blueMaterial;
    public Material redMaterial;
    [Range(0f, 1f)] public float redChance = 0.35f;

    [Header("Durations (sec)")]
    public float level1Duration = 60f;
    public float level2Duration = 60f;

    [Header("UI")]
    public GameObject graphButton;

    int currentLevel = 0;

    void Awake()
    {
        if (!spawner) spawner = FindObjectOfType<BallSpawnerBallCatch>();
        if (!score) score = FindObjectOfType<ScoreManager>();
    }

    void OnEnable()
    {
        // ������� �������� �� ���������� ������
        if (score) score.OnSessionFinished.AddListener(OnSessionFinished);
    }
    void OnDisable()
    {
        if (score) score.OnSessionFinished.RemoveListener(OnSessionFinished);
    }

    // --- ��������� ������ ��� ������/������ ---
    // ��������� ��� � ������ "������ ����" ������ ������� ScoreManager.StartSession
    public void StartLevel1()
    {
        currentLevel = 1;

        // ��������� ������ 1: ��� ������
        if (spawner)
        {
            spawner.useColors = false;
            // ��������� ���� �������� � ��� � ��� �� ���������
        }

        if (score)
        {
            score.SetShowStartButton(false);
            score.SetShowGraphButton(false);
            score.sessionDuration = level1Duration;
            score.StartSession(); // ��� ������� ���� ����
        }
    }

    // ����� �������� ������� �� ����������, ���� ����� �������������� ����� 2-� �������
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
            score.StartSession();
        }
    }

    // --- ���������� ���������� ������ ---
    void OnSessionFinished()
    {
        if (currentLevel == 1)
        {
            // ����� ��������� 2-� �������
            StartLevel2();
        }
        else
        {
            score.SetShowStartButton(true);
            if (score) score.SetShowGraphButton(true);
            // ��� ������ �������� � ����� �������� ��������� ����/������
            Debug.Log("[LevelDirector] ��� ������ ���������.");
        }
    }
}
