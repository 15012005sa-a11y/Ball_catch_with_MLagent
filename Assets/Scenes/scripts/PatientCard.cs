using System;
using System.Collections.Generic;

[Serializable]
public class PatientCard
{
    public string patientName;
    public int age;
    public string gender;

    // ������������ ���������
    public float initialBallSpeed;
    public float spawnInterval;

    // ������� ���������
    public DifficultyLevel difficultyLevel;

    // ������� ���������
    public List<SessionRecord> sessionHistory;

    public void AddSessionRecord(SessionRecord record)
    {
        sessionHistory.Add(record);
    }


    // �����������
    public PatientCard(string name, int age, string gender, float ballSpeed, float interval, DifficultyLevel difficulty)
    {
        this.patientName = name;
        this.age = age;
        this.gender = gender;
        this.initialBallSpeed = ballSpeed;
        this.spawnInterval = interval;
        this.difficultyLevel = difficulty;
        this.sessionHistory = new List<SessionRecord>();
    }
}

// ������� ����� ������
[Serializable]
public class SessionRecord
{
    public DateTime sessionDate;
    public int score;
    public float successRate;

    public SessionRecord(int score, float successRate)
    {
        this.sessionDate = DateTime.Now;
        this.score = score;
        this.successRate = successRate;
    }
}

public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard,
    Adaptive
}
