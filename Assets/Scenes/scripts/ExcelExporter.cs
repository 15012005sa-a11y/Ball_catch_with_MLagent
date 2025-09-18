using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class ExcelExporter : MonoBehaviour
{
    // Папка, куда складываем все CSV (используйте это же в другом коде)
    public const string progressFolder = "Progress";

    // Локаль под Excel с запятыми и «;» как разделителем
    private static readonly CultureInfo CsvCulture = CultureInfo.GetCultureInfo("ru-RU");
    private const string SEP = ";";

    // Заголовок — как на вашем образце
    private const string Header =
        "PatientID;Date;Score;SuccessRate;Reaction;Right hand;Left hand";

    /// <summary>
    /// Добавляет строку с результатами одной сессии пациента в его CSV.
    /// </summary>
    public void ExportSession(
        int patientId,
        int score,
        float successRate,
        float playTimeSec,      // оставлено для совместимости сигнатуры
        float avgReactionSec,
        float avgRightAngle,
        float avgLeftAngle,
        string patientDisplayName = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(patientDisplayName))
                patientDisplayName = PatientManager.Instance?.Current?.displayName ?? $"Patient_{patientId}";

            string path = GetPatientCsvPathByName(patientDisplayName);
            EnsureDirectory(Path.GetDirectoryName(path));

            bool needHeader = !File.Exists(path);
            using var sw = new StreamWriter(
                path,
                append: true,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true) // BOM, чтобы Excel точно понял кодировку
            );

            if (needHeader)
                sw.WriteLine(Header);

            // Формат строго под скрин: ; как разделитель, запятая в числах
            string line = string.Join(SEP, new[]
            {
                patientId.ToString(CsvCulture),
                DateTime.Now.ToString("dd.MM.yyyy", CsvCulture),
                score.ToString(CsvCulture),
                successRate.ToString("0.##", CsvCulture),
                avgReactionSec.ToString("0.###", CsvCulture),
                avgRightAngle.ToString("0.###", CsvCulture),
                avgLeftAngle.ToString("0.###", CsvCulture),
            });

            sw.WriteLine(line);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ExcelExporter] CSV write error: {e.Message}");
        }
    }

    // ---------- ВСПОМОГАТЕЛЬНОЕ ----------

    public static string GetPatientCsvPathById(int patientId)
    {
        string name = PatientManager.Instance?.FindById(patientId)?.displayName ?? $"Patient_{patientId}";
        return GetPatientCsvPathByName(name);
    }

    private static string GetPatientCsvPathByName(string displayName)
    {
        string folder = Path.Combine(Application.persistentDataPath, progressFolder);
        string file = MakeSafeFileName($"{displayName}_patientProgress.csv");
        return Path.Combine(folder, file);
    }

    private static void EnsureDirectory(string folder)
    {
        if (string.IsNullOrEmpty(folder)) return;
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
    }

    private static string MakeSafeFileName(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        s = s.Replace(' ', '_');
        while (s.Contains("__")) s = s.Replace("__", "_");
        return s;
    }
}
