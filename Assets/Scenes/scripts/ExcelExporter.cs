using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Экспорт сводки в CSV (7 колонок), разделитель — ';'
/// Файл: PatientProgress3.csv в Application.persistentDataPath
/// Колонки: PatientID;Date;Score;SuccessRate;Reaction;Right hand;Left hand
/// </summary>
public class ExcelExporter : MonoBehaviour
{
    private string _filePath;
    private static readonly CultureInfo RU = new CultureInfo("ru-RU");
    private const char DELIM = ';';

    private void Awake()
    {
        _filePath = Path.Combine(Application.persistentDataPath, "PatientProgress3.csv");

        if (!File.Exists(_filePath))
        {
            using var sw = new StreamWriter(
                _filePath,
                append: false,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true) // UTF-8 BOM для Excel/WPS
            );
            sw.WriteLine(string.Join(DELIM, new[]
            {
                "PatientID","Date","Score","SuccessRate","Reaction","Right hand","Left hand"
            }));
            Debug.Log($"[ExcelExporter] Created CSV at {_filePath}");
        }
    }

    /// <summary>Основной экспорт: принимает уже подсчитанные средние значения.</summary>
    public void ExportSession(
        int patientID,
        int score,
        float successRate,
        float duration,
        float avgReaction,
        float avgRightAngle,
        float avgLeftAngle
    )
    {
        string dateStr = DateTime.Now.ToString("dd.MM.yyyy", RU); // короткая дата

        string line = string.Join(DELIM, new[]
        {
            patientID.ToString(RU),
            dateStr,
            score.ToString(RU),
            successRate.ToString("0.##", RU),
            avgReaction.ToString("0.###", RU),
            avgRightAngle.ToString("0.#", RU),
            avgLeftAngle.ToString("0.#", RU)
        });

        Append(line);
    }

    /// <summary>
    /// Перегрузка для совместимости: приходит список метрик — считаем только среднюю реакцию.
    /// Правую/левую руку тут не знаем → 0 (её теперь шлёт ScoreManager через основной метод).
    /// </summary>
    public void ExportSession(
        int patientID,
        int score,
        float successRate,
        float duration,
        IReadOnlyList<float> accuracies,
        IReadOnlyList<float> reactions
    )
    {
        float avgReaction = (reactions != null && reactions.Count > 0) ? reactions.Average() : 0f;
        ExportSession(patientID, score, successRate, duration, avgReaction, 0f, 0f);
    }

    private void Append(string line)
    {
        try
        {
            using var fs = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            sw.WriteLine(line);
            Debug.Log($"[ExcelExporter] CSV appended: {line}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ExcelExporter] Export failed: {ex.Message}");
        }
    }
}
