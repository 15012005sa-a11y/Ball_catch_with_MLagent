using System;
using UnityEngine;

[Serializable]
public class PatientSettings
{
    // --- Длительности/перерывы ---
    public int Level1Duration = 180;     // сек
    public int Level2Duration = 120;     // сек
    public int RestSeconds = 60;      // сек

    // --- Поведение игры ---
    public float RedChance = 0.35f;       // 0..1

    // --- Новые параметры движения/скорости (ИМЕНА ДОЛЖНЫ БЫТЬ ПУБЛИЧНЫМИ) ---
    public float SpawnInterval = 1.5f; // сек между спавнами
    public float BallSpeed = 1.0f; // базовая скорость
    public float SpeedIncreaseFactor = 1.1f; // множитель, если точность ≥80%
    public float SpeedDecreaseFactor = 0.6f; // множитель, если точность ≤50%

    // --- Флаги управления Kinect (как было) ---
    public bool StopKinectOnGameEnd = true;
    public bool StopBetweenLevels = false;

    // ---------- АЛИАСЫ ДЛЯ СТАРОГО КОДА (camelCase) ----------
    // PreferencesPanel умеет работать и с PascalCase, и с этими алиасами
    public int level1DurationSec { get => Level1Duration; set => Level1Duration = value; }
    public int level2DurationSec { get => Level2Duration; set => Level2Duration = value; }
    public int restTimeSec { get => RestSeconds; set => RestSeconds = value; }
    public float redChance { get => RedChance; set => RedChance = value; }

    public float spawnInterval { get => SpawnInterval; set => SpawnInterval = value; }
    public float ballSpeed { get => BallSpeed; set => BallSpeed = value; }
    public float speedIncreaseFactor { get => SpeedIncreaseFactor; set => SpeedIncreaseFactor = value; }
    public float speedDecreaseFactor { get => SpeedDecreaseFactor; set => SpeedDecreaseFactor = value; }

    // ---------- ДЕФОЛТЫ/САНИТАЙЗЕР ----------
    public void EnsureDefaults()
    {
        if (Level1Duration <= 0) Level1Duration = 180;
        if (Level2Duration <= 0) Level2Duration = 120;
        if (RestSeconds < 0) RestSeconds = 60;

        if (RedChance < 0f || RedChance > 1f) RedChance = 0.35f;

        if (SpawnInterval <= 0f) SpawnInterval = 1.5f;
        if (BallSpeed <= 0f) BallSpeed = 1.0f;
        if (SpeedIncreaseFactor <= 0f) SpeedIncreaseFactor = 1.1f;
        if (SpeedDecreaseFactor <= 0f) SpeedDecreaseFactor = 0.6f;
    }
}
