using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ������� ��������� ������: ��������� �� ������ ����� ����,
/// "�����" � ������������ hitProbability � ����� ����� �������.
/// �������� ������ ������ ScoreManager � ��� ��������� ��������� ��������.
/// </summary>
[DisallowMultipleComponent]
public class PlayerSimulator : MonoBehaviour
{
    [Header("�������� ���������")]
    [Range(0f, 1f)] public float hitProbability = 0.75f;   // ������� % ���������
    public float reactionMeanMs = 450f;                     // ������� ����� ������� (��)
    public float reactionStdMs = 120f;                     // ������� (��)

    [Header("�������������")]
    [Tooltip("����������� '����������� ��������' � ���� ������ ��� �������� �������������")]
    [Range(0f, 1f)] public float trunkCompProbability = 0.10f;

    [Tooltip("�����������/������������ �������� ������� (���) ��� ��������� �������")]
    public float minReactionSec = 0.08f;
    public float maxReactionSec = 1.20f;

    [Tooltip("������������� ��������� ����� ������ ����� ���������� ����������")]
    public bool autoLoopSessions = true;

    ScoreManager score;
    HashSet<int> handledSpawnIds = new HashSet<int>();

    #pragma warning disable CS0414
    private int lastSeenSpawnCount = 0;
    #pragma warning restore CS0414

    System.Random rng;

    void Awake()
    {
        score = FindObjectOfType<ScoreManager>(true);
        rng = new System.Random();
        if (!score)
            Debug.LogError("[PlayerSimulator] �� ������ ScoreManager � �����.");
    }

    void OnEnable()
    {
        if (score != null)
            score.OnSessionFinished.AddListener(OnSessionFinished);
        lastSeenSpawnCount = 0;
        handledSpawnIds.Clear();
    }

    void OnDisable()
    {
        if (score != null)
            score.OnSessionFinished.RemoveListener(OnSessionFinished);
    }

    void OnSessionFinished()
    {
        handledSpawnIds.Clear();
        lastSeenSpawnCount = 0;

        if (autoLoopSessions && score != null && score.isActiveAndEnabled)
        {
            // �������� ��������� ������ ����� ��������� �����
            StartCoroutine(StartNextSessionCo());
        }
    }

    IEnumerator StartNextSessionCo()
    {
        yield return new WaitForSeconds(0.5f);
        // ���� � ��� ���� ������� ����� �������� ����� LevelDirector � ��� �� ������.
        // ����� ����� �������� ���������� �������:
        score.StartSession();
    }

    void Update()
    {
        if (score == null || score.spawnTimes == null || score.spawnTimes.Count == 0)
            return;

        // ����������� ����� ������ �� ������� spawnTimes (���� = ballId)
        foreach (var kv in score.spawnTimes)
        {
            int ballId = kv.Key;
            if (handledSpawnIds.Contains(ballId)) continue;

            handledSpawnIds.Add(ballId);
            // ��� ������� ������ ������ ��������, ������� ��������� ��� "������� ������"
            StartCoroutine(ReactToSpawnCo(ballId));
        }
    }

    IEnumerator ReactToSpawnCo(int ballId)
    {
        // ���������� ����� ������� (������ ������ � ����������)
        float reactSec = Mathf.Clamp(Normal(reactionMeanMs, reactionStdMs) / 1000f, minReactionSec, maxReactionSec);
        yield return new WaitForSeconds(reactSec);

        if (score == null) yield break;

        // ��������� ����� ������� ��� ������ ����
        score.RecordReactionTime(reactSec);

        // ������, ������� ��� ��� ������������
        if (rng.NextDouble() < hitProbability)
        {
            score.AddScore(1);
        }
        else
        {
            score.RegisterMiss(); // ������ �������; �� ������� ���� ������ ���������� AddScore
        }
    }

    // ��������� ����������� ������������� (�����-�������)
    float Normal(float mean, float std)
    {
        // (0,1] ��� ������������ ���������
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        double randStdNormal = System.Math.Sqrt(-2.0 * System.Math.Log(u1)) *
                               System.Math.Sin(2.0 * System.Math.PI * u2);
        return (float)(mean + std * randStdNormal);
    }
}
