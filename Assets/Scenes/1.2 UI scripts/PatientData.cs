using System;
using UnityEngine;

[Serializable]
public class GameSettings
{
    public int level1DurationSec = 180;
    public int level2DurationSec = 120;
    public int restTimeSec = 60;
    [Range(0f, 1f)] public float redChance = 0.35f;
}

[Serializable]
public class Patient
{
    public int id;
    public string displayName = "Patient 1";
    public int age = 60;
    public string startedRehab = "01.09.25";
    public GameSettings settings = new GameSettings();

    // ✅ АДАПТЕР ДЛЯ СТАРОГО КОДА
    // Старые скрипты обращаются к patient.patientName — пробрасываем в displayName
    public string patientName
    {
        get => displayName;
        set => displayName = value;
    }
}

