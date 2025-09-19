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
    // ОБНОВЛЁННЫЙ хедер: добавили "Ball speed" справа от Left hand
    // Обновлённый заголовок
    private const string Header =
        "PatientID;Date;Score;SuccessRate;Reaction;Right hand;Left hand;Ball speed";

    public void ExportSession(
    int patientId,
    int score,
    float successRate,
    float playTimeSec,      // для совместимости
    float avgReactionSec,
    float avgRightAngle,
    float avgLeftAngle,
    float ballSpeed,
    string patientDisplayName = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(patientDisplayName))
                patientDisplayName = PatientManager.Instance?.Current?.displayName ?? $"Patient_{patientId}";

            string path = GetPatientCsvPathByName(patientDisplayName);
            EnsureDirectory(Path.GetDirectoryName(path));
            EnsureHeaderUpToDate(path); // ваш helper

            string line = string.Join(SEP, new[]
            {
            patientId.ToString(CsvCulture),
            DateTime.Now.ToString("dd.MM.yyyy", CsvCulture),
            score.ToString(CsvCulture),
            successRate.ToString("0.##",   CsvCulture),
            avgReactionSec.ToString("0.###", CsvCulture),
            avgRightAngle.ToString("0.###",  CsvCulture),
            avgLeftAngle.ToString("0.###",   CsvCulture),
            ballSpeed.ToString("0.###",      CsvCulture),
        });

            AppendCsvLineWithRetry(path, line); // ваш helper
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ExcelExporter] CSV write error: {e.Message}");
        }
    }

    // создаёт файл c заголовком, безопасно, с повторными попытками
    // Создаёт файл с хедером или «апгрейдит» старый (делает .bak)
    private void EnsureHeaderUpToDate(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                using var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                using var sw = new StreamWriter(fs, new UTF8Encoding(true));
                sw.WriteLine(Header);
                return;
            }

            // файл есть — проверяем первую строку
            using (var sr = new StreamReader(path, true))
            {
                string first = sr.ReadLine() ?? string.Empty;
                if (first.Trim() == Header) return;
            }

            // старый формат — делаем бэкап и пишем новый заголовок
            string bak = path + ".bak";
            try { if (File.Exists(bak)) File.Delete(bak); } catch { }
            File.Move(path, bak);
            using var fs2 = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            using var sw2 = new StreamWriter(fs2, new UTF8Encoding(true));
            sw2.WriteLine(Header);
        }
        catch (IOException) { /* игнор, ниже всё равно попробуем дозапись с ретраями */ }
    }

    // Дозапись строки в CSV с ретраями и FileShare.Read
    private void AppendCsvLineWithRetry(string path, string line)
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                using var sw = new StreamWriter(fs, new UTF8Encoding(true));
                sw.WriteLine(line);
                return;
            }
            catch (IOException)
            {
                if (attempt == 7) throw;
                System.Threading.Thread.Sleep(200); // 0.2s
            }
        }
    }

    // --- перегрузка для старых вызовов без ballSpeed ---
    public void ExportSession(
        int patientId, int score, float successRate, float playTimeSec,
        float avgReactionSec, float avgRightAngle, float avgLeftAngle,
        string patientDisplayName = null)
    {
        ExportSession(patientId, score, successRate, playTimeSec,
                      avgReactionSec, avgRightAngle, avgLeftAngle,
                      0f, patientDisplayName);
    }

    // --- helper: создаёт файл с новым хедером или «апгрейдит» старый ---
    private void EnsureHeader(string path)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, Header + "\n", new UTF8Encoding(true));
            return;
        }

        using var sr = new StreamReader(path, true);
        string first = sr.ReadLine() ?? string.Empty;
        if (first.Trim() == Header) return; // уже новый формат

        // старый хедер → сохраняем резервную копию и создаём новый файл с хедером
        string bak = path + ".bak";
        try { if (File.Exists(bak)) File.Delete(bak); } catch { }
        File.Move(path, bak);
        File.WriteAllText(path, Header + "\n", new UTF8Encoding(true));
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
