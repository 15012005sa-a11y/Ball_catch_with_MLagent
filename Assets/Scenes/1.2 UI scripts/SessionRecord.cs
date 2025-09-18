using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ������� �������: ���� ������� ����� ��������.
/// �������� �������� �� ����� ����� � ������ �� ���� ������ �������.
/// </summary>
[Serializable]
public class SessionRecord
{
    // ����� ������/��������� (UTC)
    public DateTime startUtc = DateTime.UtcNow;
    public DateTime endUtc = DateTime.UtcNow;

    // �������� �������
    public int attempts;        // ����� ������� �� ������
    public int success;         // �������� (������� � �.�.)

    // ������� ������� � ������������� (��� �������/�������/StdDev)
    public List<int> reactionTimesMs = new List<int>();

    // �������������� �������� � ������ ��� KPI/�������
    public int totalScore;              // ����� ����
    public float successRate;           // 0..1 (��������� success/attempts)
    public float playTimeSec;           // ������������ � ��������

    // ������� (�� �����������, �� ������)
    public TimeSpan Duration => endUtc - startUtc;
    public double MeanRT => reactionTimesMs.Count == 0 ? 0 : reactionTimesMs.Average();
    public double MedianRT => reactionTimesMs.Count == 0 ? 0 : reactionTimesMs.OrderBy(v => v).ElementAt(reactionTimesMs.Count / 2);
    public double StdDevRT
    {
        get
        {
            if (reactionTimesMs.Count == 0) return 0;
            var avg = reactionTimesMs.Average();
            var variance = reactionTimesMs.Average(v => (v - avg) * (v - avg));
            return Math.Sqrt(variance);
        }
    }

    public override string ToString()
        => $"[{startUtc:u}..{endUtc:u}] attempts={attempts}, success={success}, score={totalScore}";
}