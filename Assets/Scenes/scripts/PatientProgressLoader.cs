using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class PatientProgressLoader
{
    public const string DefaultFileName = "PatientProgress3.csv";
    private static readonly CultureInfo RU = new CultureInfo("ru-RU");

    public static string ResolveDefaultPath(string fileName = DefaultFileName)
        => Path.Combine(Application.persistentDataPath, fileName);

    /// <summary>Читает CSV и возвращает отсортированные записи.</summary>
    public static List<ProgressRow> LoadCsv(string path)
    {
        if (string.IsNullOrEmpty(path)) path = ResolveDefaultPath();

        var list = new List<ProgressRow>();
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[PatientProgressLoader] File not found: {path}");
            return list;
        }

        using (var sr = new StreamReader(path, System.Text.Encoding.UTF8, true))
        {
            string line;
            bool headerChecked = false;
            int lineNum = 0;

            while ((line = sr.ReadLine()) != null)
            {
                lineNum++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                // пропуск заголовка, если первая строка начинается с PatientID
                if (!headerChecked)
                {
                    headerChecked = true;
                    if (line.StartsWith("PatientID", StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                if (TryParseLine(line, out var row)) list.Add(row);
                else Debug.LogWarning($"[PatientProgressLoader] Bad line #{lineNum}: {line}");
            }
        }

        list.Sort((a, b) => a.Date.CompareTo(b.Date));
        return list;
    }

    /// <summary>Парсит строку CSV формата: PatientID;Date;Score;SuccessRate;Reaction;Right hand;Left hand</summary>
    public static bool TryParseLine(string csvLine, out ProgressRow row)
    {
        row = default;
        if (string.IsNullOrWhiteSpace(csvLine)) return false;

        var p = csvLine.Split(';');
        if (p.Length < 7) return false;

        try
        {
            row = new ProgressRow
            {
                PatientID = ParseInt(p[0]),
                Date = ParseDate(p[1]),
                Score = ParseInt(p[2]),
                SuccessRate = ParseFloat(p[3]),
                Reaction = ParseFloat(p[4]),
                RightHand = ParseFloat(p[5]),
                LeftHand = ParseFloat(p[6]),
            };
            return true;
        }
        catch { return false; }
    }

    // ---- helpers -------------------------------------------------------------

    private static string Clean(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.Trim();
        if (s.Length >= 2 && s[0] == '"' && s[^1] == '"') s = s.Substring(1, s.Length - 2);
        return s;
    }

    private static int ParseInt(string s) => int.Parse(Clean(s), NumberStyles.Integer, RU);

    private static float ParseFloat(string s)
        => float.Parse(Clean(s), NumberStyles.Float | NumberStyles.AllowThousands, RU);

    private static DateTime ParseDate(string s)
    {
        s = Clean(s);
        // Основной путь: обычный Parse по ru-RU
        if (DateTime.TryParse(s, RU, DateTimeStyles.AssumeLocal, out var dt)) return dt.Date;
        // Запасные форматы
        string[] fmts = { "yyyy-MM-dd", "dd.MM.yyyy", "dd/MM/yyyy" };
        if (DateTime.TryParseExact(s, fmts, RU, DateTimeStyles.AssumeLocal, out dt)) return dt.Date;
        return DateTime.MinValue;
    }

    /// <summary>Опционально: дозапись строки в CSV с автосозданием заголовка.</summary>
    public static void Append(string path, in ProgressRow row, bool ensureHeader = true)
    {
        if (string.IsNullOrEmpty(path)) path = ResolveDefaultPath();
        bool needHeader = ensureHeader && !File.Exists(path);

        using var sw = new StreamWriter(path, append: true, System.Text.Encoding.UTF8);
        if (needHeader)
            sw.WriteLine("PatientID;Date;Score;SuccessRate;Reaction;Right hand;Left hand");

        sw.WriteLine(string.Join(";", new[]
        {
            row.PatientID.ToString(RU),
            row.Date.ToString("yyyy-MM-dd", RU),
            row.Score.ToString(RU),
            row.SuccessRate.ToString(RU),
            row.Reaction.ToString(RU),
            row.RightHand.ToString(RU),
            row.LeftHand.ToString(RU),
        }));
    }
}
