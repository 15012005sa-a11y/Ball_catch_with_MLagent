using System;
using UnityEngine;

/// <summary>
/// Настройки пациента, совместимые со «старыми» именами,
/// и удобные для нового UI (нижние имена полей).
/// ВАЖНО: это КЛАСС (reference type), чтобы его можно было сравнивать с null.
/// </summary>
[Serializable]
public class PatientSettings
{
    // ---------- Каноничные поля (новые имена, используются вашим UI) ----------
    public int level1DurationSec = 180;
    public int level2DurationSec = 120;
    public int restTimeSec = 60;
    public float redChance = 0.35f;

    // Новые параметры движения/скорости
    public float spawnInterval = 1.5f;
    public float ballSpeed = 1.0f;
    public float speedIncreaseFactor = 1.1f;
    public float speedDecreaseFactor = 0.6f;

    // Флаги из LevelDirector
    public bool stopKinectOnGameEnd = true;
    public bool stopBetweenLevels = false;

    // ---------- Свойства со старыми именами (для совместимости старого кода) ----------
    // Длительности/пауза/шанс
    public int Level1Duration { get => level1DurationSec; set => level1DurationSec = value; }
    public int Level2Duration { get => level2DurationSec; set => level2DurationSec = value; }
    public int RestSeconds { get => restTimeSec; set => restTimeSec = value; }
    public float RedChance { get => redChance; set => redChance = Mathf.Clamp01(value); }

    // Флаги
    public bool StopKinectOnGameEnd { get => stopKinectOnGameEnd; set => stopKinectOnGameEnd = value; }
    public bool StopBetweenLevels { get => stopBetweenLevels; set => stopBetweenLevels = value; }

    // Параметры движения/скорости
    public float SpawnInterval { get => spawnInterval; set => spawnInterval = Mathf.Max(0.01f, value); }
    public float BallSpeed { get => ballSpeed; set => ballSpeed = Mathf.Max(0.01f, value); }
    public float SpeedIncreaseFactor { get => speedIncreaseFactor; set => speedIncreaseFactor = Mathf.Max(0.01f, value); }
    public float SpeedDecreaseFactor { get => speedDecreaseFactor; set => speedDecreaseFactor = Mathf.Max(0.01f, value); }

    // ---------- Дефолты и миграция ----------
    public static PatientSettings Default => new PatientSettings();

    /// <summary>Подставляет разумные значения, если что-то не задано/сломано.</summary>
    public void EnsureDefaults()
    {
        if (level1DurationSec <= 0) level1DurationSec = 180;
        if (level2DurationSec <= 0) level2DurationSec = 120;
        if (restTimeSec < 0) restTimeSec = 60;
        if (redChance <= 0f || redChance > 1f) redChance = 0.35f;

        if (spawnInterval <= 0f) spawnInterval = 1.5f;
        if (ballSpeed <= 0f) ballSpeed = 1.0f;
        if (speedIncreaseFactor <= 0f) speedIncreaseFactor = 1.1f;
        if (speedDecreaseFactor <= 0f) speedDecreaseFactor = 0.6f;
    }
}
