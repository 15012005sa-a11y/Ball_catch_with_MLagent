using System;
using UnityEngine;

[Serializable]
public class SessionRecord
{
    // ����� ��������� �����
    public DateTime dateUtc = DateTime.UtcNow;

    // ����� (������� ��� ���� ���� �� ScoreManager/����������)
    public int totalScore;
    public float successRate;   // 0..1
    public float playTimeSec;

    // ���.������� (�� ������� � ����� ������, ���� �� �����)
    public float avgReactionSec;
    public float avgRightAngle;
    public float avgLeftAngle;

    // ��� �������� ����
    public override string ToString()
        => $"[{dateUtc:u}] score={totalScore}, success={(successRate * 100f):0.0}%, time={playTimeSec:0.0}s";
}
