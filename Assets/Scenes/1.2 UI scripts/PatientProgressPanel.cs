using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TMPro;

/// <summary>
/// Панель прогресса пациента: рендерит 4 KPI-карточки (длительность, accuracy, meanRT, median/σ)
/// и фильтрует сессии по периоду (7/30/All).
/// НЕ зависит от конкретной реализации SessionRecord:
/// поля/свойства читаются через рефлексию (startUtc/endUtc/attempts/success/reactionTimesMs и их варианты).
/// Зависит только от наличия KpiCardView (компонент карточки).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PatientProgressPanel : MonoBehaviour
{
    // -------------------- Внешние зависимости --------------------
    [Header("Data")]
    public PatientManager patientManager;      // перетащить ваш менеджер пациентов

    public enum Period { D7, D30, All }

    [Header("Filters (optional)")]
    public TMP_Dropdown periodDropdown;        // 3 опции: 7D, 30D, All
    public Period defaultPeriod = Period.D7;

    [Header("KPI Cards")]
    public KpiCardView kpiDuration;
    public KpiCardView kpiAccuracy;
    public KpiCardView kpiMeanRT;
    public KpiCardView kpiMedianSD;

    [Header("Charts (optional)")]
    public MonoBehaviour reactionChart;        // класс с методом: public void Draw(List<SessionRecord> data)
    public MonoBehaviour accuracyChart;        // класс с методом: public void Draw(List<SessionRecord> data)

    // -------------------- Внутреннее состояние --------------------
    private Period _period;
    private TimeZoneInfo _tz;

    // Кандидаты имён для полей/свойств SessionRecord (case-insensitive)
    private static readonly string[] StartNames = { "startUtc", "StartUtc", "start", "Start", "startTime", "StartTime", "sessionStartUtc", "SessionStartUtc" };
    private static readonly string[] EndNames = { "endUtc", "EndUtc", "end", "End", "finishUtc", "FinishUtc", "stopUtc", "StopUtc", "endTime", "EndTime", "sessionEndUtc", "SessionEndUtc" };
    private static readonly string[] AttemptsNames = { "attempts", "Attempts", "tries", "Tries", "totalAttempts", "TotalAttempts", "shots", "Shots" };
    private static readonly string[] SuccessNames = { "success", "Success", "successes", "Successes", "hits", "Hits", "correct", "Correct", "caught", "Caught" };
    private static readonly string[] RtNames = { "reactionTimesMs", "ReactionTimesMs", "reactionTimes", "ReactionTimes", "rt", "RT", "rtMs", "RTMs", "reaction_ms", "ReactionMs", "latenciesMs", "LatenciesMs" };

    // -------------------- Unity lifecycle --------------------
    private void Awake()
    {
        _tz = GetAlmatyTZ();
        _period = defaultPeriod;

        if (periodDropdown != null)
            periodDropdown.onValueChanged.AddListener(OnPeriodChanged);
    }

    private void OnEnable()
    {
        if (patientManager != null)
            patientManager.OnSelectedPatientChanged += Render;

        if (patientManager != null) Render(patientManager.Current);
        else ClearAll();
    }

    private void OnDisable()
    {
        if (patientManager != null)
            patientManager.OnSelectedPatientChanged -= Render;

        if (periodDropdown != null)
            periodDropdown.onValueChanged.RemoveListener(OnPeriodChanged);
    }

    // -------------------- UI events --------------------
    private void OnPeriodChanged(int idx)
    {
        _period = idx switch
        {
            0 => Period.D7,
            1 => Period.D30,
            _ => Period.All
        };
        if (patientManager != null) Render(patientManager.Current);
    }

    // -------------------- Публичный рендер --------------------
    public void Render(Patient p)
    {
        if (p == null) { ClearAll(); return; }

        // История сессий
        List<SessionRecord> sessions = patientManager.GetSessionHistory(p.id);
        if (sessions == null || sessions.Count == 0) { ClearAll(); return; }

        // Фильтр периода
        var slice = FilterByPeriod(sessions, _period, _tz, DateTime.UtcNow);

        // KPI
        RenderKpis(slice);

        // Графики (если заданы и есть метод Draw(List<SessionRecord>))
        if (reactionChart != null)
        {
            var mi = reactionChart.GetType().GetMethod("Draw", BindingFlags.Public | BindingFlags.Instance);
            if (mi != null) mi.Invoke(reactionChart, new object[] { slice });
        }
        if (accuracyChart != null)
        {
            var mi = accuracyChart.GetType().GetMethod("Draw", BindingFlags.Public | BindingFlags.Instance);
            if (mi != null) mi.Invoke(accuracyChart, new object[] { slice });
        }
    }

    private void ClearAll()
    {
        if (kpiDuration != null) { kpiDuration.SetLabel("Длительность сессии"); kpiDuration.SetValue("—"); kpiDuration.ClearSparkline(); }
        if (kpiAccuracy != null) { kpiAccuracy.SetLabel("Попытки / Успешно / Accuracy %"); kpiAccuracy.SetValue("—"); kpiAccuracy.ClearSparkline(); }
        if (kpiMeanRT != null) { kpiMeanRT.SetLabel("Среднее время реакции (мс)"); kpiMeanRT.SetValue("—"); kpiMeanRT.ClearSparkline(); }
        if (kpiMedianSD != null) { kpiMedianSD.SetLabel("Медиана / StdDev (мс)"); kpiMedianSD.SetValue("—"); kpiMedianSD.ClearSparkline(); }
    }

    // -------------------- KPI rendering --------------------
    private void RenderKpis(List<SessionRecord> slice)
    {
        if (kpiDuration != null) kpiDuration.SetLabel("Длительность сессии");
        if (kpiAccuracy != null) kpiAccuracy.SetLabel("Попытки / Успешно / Accuracy %");
        if (kpiMeanRT != null) kpiMeanRT.SetLabel("Среднее время реакции (мс)");
        if (kpiMedianSD != null) kpiMedianSD.SetLabel("Медиана / StdDev (мс)");

        if (slice == null || slice.Count == 0) { ClearAll(); return; }

        SessionRecord last = slice[slice.Count - 1];

        // Извлекаем поля последней сессии
        DateTime startUtc = GetDateTime(last, StartNames) ?? DateTime.UtcNow;
        DateTime endUtc = GetDateTime(last, EndNames) ?? startUtc;
        int attempts = GetInt(last, AttemptsNames);
        int success = GetInt(last, SuccessNames);
        float[] rt = GetFloatArray(last, RtNames);

        // Статистика реакций
        Moments(rt, out float meanMs, out float medianMs, out float stdMs);

        // 1) Длительность
        string durText = FormatDuration(endUtc - startUtc);
        if (kpiDuration != null) kpiDuration.SetValue(durText);

        // 2) Accuracy
        float accPct = attempts > 0 ? (100f * success / attempts) : 0f;
        string accText = $"{success}/{attempts}  •  {accPct:0.0} %";
        Color accColor = accPct >= 85f ? new Color(0.27f, 0.83f, 0.52f)
                         : accPct >= 70f ? new Color(0.96f, 0.77f, 0.26f)
                         : new Color(0.98f, 0.39f, 0.36f);
        if (kpiAccuracy != null) kpiAccuracy.SetValue(accText, accColor);

        // 3) Mean RT
        if (kpiMeanRT != null) kpiMeanRT.SetValue($"{meanMs:0} ms");

        // 4) Median + σ
        if (kpiMedianSD != null) kpiMedianSD.SetValue($"{medianMs:0} ms  •  σ {stdMs:0}");

        // --------- Спарклайны по последним N сессиям ---------
        int N = Mathf.Min(20, slice.Count);
        var tail = slice.GetRange(slice.Count - N, N);

        // a) Длительность (сек) — инверсия (меньше = лучше)
        if (kpiDuration != null)
        {
            float[] durArr = new float[N];
            for (int i = 0; i < N; i++)
            {
                var s0 = tail[i];
                var st0 = GetDateTime(s0, StartNames) ?? DateTime.UtcNow;
                var en0 = GetDateTime(s0, EndNames) ?? st0;
                durArr[i] = (float)(en0 - st0).TotalSeconds;
            }
            kpiDuration.SetSparkline(durArr, null, null, true);
        }

        // b) Accuracy %
        if (kpiAccuracy != null)
        {
            float[] accArr = new float[N];
            for (int i = 0; i < N; i++)
            {
                var s0 = tail[i];
                int at = GetInt(s0, AttemptsNames);
                int su = GetInt(s0, SuccessNames);
                accArr[i] = at > 0 ? (100f * su / at) : 0f;
            }
            kpiAccuracy.SetSparkline(accArr, 0f, 100f, false);
        }

        // c) Mean RT — инверсия
        if (kpiMeanRT != null)
        {
            float[] meanArr = new float[N];
            for (int i = 0; i < N; i++)
            {
                var s0 = tail[i];
                var r0 = GetFloatArray(s0, RtNames);
                Moments(r0, out float m, out _, out _);
                meanArr[i] = m;
            }
            kpiMeanRT.SetSparkline(meanArr, null, null, true);
        }

        // d) Median RT — инверсия
        if (kpiMedianSD != null)
        {
            float[] medArr = new float[N];
            for (int i = 0; i < N; i++)
            {
                var s0 = tail[i];
                var r0 = GetFloatArray(s0, RtNames);
                Moments(r0, out _, out float med, out _);
                medArr[i] = med;
            }
            kpiMedianSD.SetSparkline(medArr, null, null, true);
        }
    }

    // -------------------- Period/Time helpers --------------------
    private static string FormatDuration(TimeSpan ts)
    {
        return (ts.TotalHours >= 1.0)
            ? ts.ToString(@"hh\:mm\:ss")
            : ts.ToString(@"mm\:ss");
    }

    private static TimeZoneInfo GetAlmatyTZ()
    {
        string[] ids = { "Central Asia Standard Time", "Asia/Almaty" };
        foreach (var id in ids)
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { }
        }
        return TimeZoneInfo.Utc;
    }

    private static DateTime ToLocal(DateTime utc, TimeZoneInfo tz)
    {
        if (utc.Kind != DateTimeKind.Utc)
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
    }

    private static List<SessionRecord> FilterByPeriod(List<SessionRecord> src, Period p, TimeZoneInfo tz, DateTime utcNow)
    {
        if (src == null) return new List<SessionRecord>();
        if (p == Period.All)
        {
            src.Sort((a, b) =>
            {
                var sa = GetDateTime(a, StartNames) ?? DateTime.MinValue;
                var sb = GetDateTime(b, StartNames) ?? DateTime.MinValue;
                return sa.CompareTo(sb);
            });
            return src;
        }

        int days = (p == Period.D7) ? 7 : 30;
        DateTime nowLocal = ToLocal(utcNow, tz).Date;
        DateTime fromLocal = nowLocal.AddDays(-(days - 1));

        List<SessionRecord> result = new List<SessionRecord>(src.Count);
        for (int i = 0; i < src.Count; i++)
        {
            DateTime s = GetDateTime(src[i], StartNames) ?? DateTime.MinValue;
            DateTime d = ToLocal(s, tz).Date;
            if (d >= fromLocal && d <= nowLocal)
                result.Add(src[i]);
        }
        result.Sort((a, b) =>
        {
            var sa = GetDateTime(a, StartNames) ?? DateTime.MinValue;
            var sb = GetDateTime(b, StartNames) ?? DateTime.MinValue;
            return sa.CompareTo(sb);
        });
        return result;
    }

    // -------------------- Reflection helpers --------------------
    private static DateTime? GetDateTime(object obj, string[] candidateNames)
    {
        if (obj == null) return null;
        foreach (var name in candidateNames)
        {
            if (TryGetMember(obj, name, out object val) && val != null)
            {
                if (val is DateTime dt) return dt;
                if (val is string s && DateTime.TryParse(s, out var parsed)) return parsed;
                if (val is long ticks) return new DateTime(ticks, DateTimeKind.Utc);
            }
        }
        return null;
    }

    private static int GetInt(object obj, string[] candidateNames)
    {
        if (obj == null) return 0;
        foreach (var name in candidateNames)
        {
            if (TryGetMember(obj, name, out object val) && val != null)
            {
                if (val is int i) return i;
                if (val is short sh) return sh;
                if (val is long l) return (int)l;
                if (val is float f) return Mathf.RoundToInt(f);
                if (val is double d) return (int)Math.Round(d);
                if (val is string s && int.TryParse(s, out var parsed)) return parsed;
            }
        }
        return 0;
    }

    private static float[] GetFloatArray(object obj, string[] candidateNames)
    {
        if (obj == null) return Array.Empty<float>();
        foreach (var name in candidateNames)
        {
            if (TryGetMember(obj, name, out object val) && val != null)
            {
                switch (val)
                {
                    case float[] fa: return fa;
                    case double[] da:
                        {
                            float[] r = new float[da.Length];
                            for (int i = 0; i < da.Length; i++) r[i] = (float)da[i];
                            return r;
                        }
                    case List<float> lf: return lf.ToArray();
                    case List<double> ld:
                        {
                            float[] r = new float[ld.Count];
                            for (int i = 0; i < ld.Count; i++) r[i] = (float)ld[i];
                            return r;
                        }
                }
            }
        }
        return Array.Empty<float>();
    }

    private static bool TryGetMember(object obj, string name, out object value)
    {
        value = null;
        if (obj == null) return false;
        var t = obj.GetType();

        // Property (public instance, ignore case)
        var prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop != null)
        {
            value = prop.GetValue(obj, null);
            return true;
        }

        // Field (public instance, ignore case)
        var field = t.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (field != null)
        {
            value = field.GetValue(obj);
            return true;
        }

        return false;
    }

    // -------------------- Math helpers --------------------
    /// <summary>Среднее/медиана/ст.откл. по массиву (деление на N).</summary>
    private static void Moments(float[] x, out float mean, out float median, out float std)
    {
        mean = median = std = 0f;
        if (x == null || x.Length == 0) return;

        // mean
        double sum = 0;
        for (int i = 0; i < x.Length; i++) sum += x[i];
        mean = (float)(sum / x.Length);

        // median (через копию и сортировку)
        var copy = (float[])x.Clone();
        Array.Sort(copy);
        if ((copy.Length & 1) == 1)
            median = copy[copy.Length / 2];
        else
            median = 0.5f * (copy[copy.Length / 2 - 1] + copy[copy.Length / 2]);

        // std (делим на N)
        if (x.Length > 1)
        {
            double var = 0;
            for (int i = 0; i < x.Length; i++)
            {
                double d = x[i] - mean;
                var += d * d;
            }
            std = (float)Math.Sqrt(var / x.Length);
        }
    }
}
