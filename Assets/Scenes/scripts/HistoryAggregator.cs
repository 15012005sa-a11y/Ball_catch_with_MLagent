using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Агрегация истории и сглаживание.
/// Недели считаем как раньше: Calendar.GetWeekOfYear + FirstFourDayWeek, Monday.
/// Добавлены дженерик-перегрузки для любых двух метрик (A/B).
/// </summary>
public static class HistoryAggregator
{
    // ----------------------- ПУБЛИЧНЫЕ API (Right/Left как раньше) -----------------------

    /// <summary>Средние по неделям для Right/Left.</summary>
    public static List<(DateTime weekStart, float right, float left)>
        ToWeeklyAverages(IEnumerable<ProgressRow> data)
        => ToWeeklyAverages(data, r => r.RightHand, r => r.LeftHand);

    /// <summary>Средние по месяцам для Right/Left.</summary>
    public static List<(DateTime month, float right, float left)>
        ToMonthlyAverages(IEnumerable<ProgressRow> data)
        => ToMonthlyAverages(data, r => r.RightHand, r => r.LeftHand);

    // ----------------------- НОВОЕ: дженерик-перегрузки (для Success/Reaction и т.п.) ---

    /// <summary>
    /// Средние по неделям для произвольных двух метрик.
    /// Неделю и «год недели» считаем так же, как в вашей прежней версии (без ISOWeek).
    /// </summary>
    public static List<(DateTime weekStart, float a, float b)>
        ToWeeklyAverages(IEnumerable<ProgressRow> data,
                         Func<ProgressRow, float> selA,
                         Func<ProgressRow, float> selB)
    {
        var result = new List<(DateTime, float, float)>();
        if (data == null) return result;

        var cal = CultureInfo.InvariantCulture.Calendar;
        const CalendarWeekRule rule = CalendarWeekRule.FirstFourDayWeek;
        const DayOfWeek firstDay = DayOfWeek.Monday;

        var groups = data.GroupBy(r =>
        {
            int week = cal.GetWeekOfYear(r.Date, rule, firstDay);
            int weekYear = GetWeekYear(r.Date, cal, rule, firstDay);
            return new WeekKey(weekYear, week);
        });

        foreach (var g in groups.OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Week))
        {
            float avgA = (float)g.Average(selA);
            float avgB = (float)g.Average(selB);
            DateTime any = g.First().Date;
            DateTime start = StartOfWeek(any, firstDay);
            result.Add((start, avgA, avgB));
        }
        return result;
    }

    /// <summary>
    /// Средние по месяцам для произвольных двух метрик.
    /// </summary>
    public static List<(DateTime month, float a, float b)>
        ToMonthlyAverages(IEnumerable<ProgressRow> data,
                          Func<ProgressRow, float> selA,
                          Func<ProgressRow, float> selB)
    {
        var result = new List<(DateTime, float, float)>();
        if (data == null) return result;

        return data
            .GroupBy(r => new { r.Date.Year, r.Date.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => (
                new DateTime(g.Key.Year, g.Key.Month, 1),
                (float)g.Average(selA),
                (float)g.Average(selB)
            ))
            .ToList();
    }

    // ----------------------- Сглаживание -----------------------

    /// <summary>
    /// Скользящее среднее по окну (в элементах списка).
    /// Если window ≤ 1 — возвращает копию исходных значений.
    /// </summary>
    public static List<float> MovingAverage(IList<float> src, int window)
    {
        if (src == null || src.Count == 0) return new List<float>();
        if (window <= 1) return src.ToList();

        var res = new List<float>(src.Count);
        double sum = 0;
        var q = new Queue<float>();

        for (int i = 0; i < src.Count; i++)
        {
            q.Enqueue(src[i]); sum += src[i];
            if (q.Count > window) sum -= q.Dequeue();
            res.Add((float)(sum / q.Count));
        }
        return res;
    }

    public static List<(DateTime weekStart, float success)> ToWeeklyAvgSuccess(IEnumerable<ProgressRow> data)
    {
        var cal = CultureInfo.InvariantCulture.Calendar;
        const CalendarWeekRule rule = CalendarWeekRule.FirstFourDayWeek;
        const DayOfWeek firstDay = DayOfWeek.Monday;

        return data
            .GroupBy(r => new { Y = GetWeekYear(r.Date, cal, rule, firstDay), W = cal.GetWeekOfYear(r.Date, rule, firstDay) })
            .OrderBy(g => g.Key.Y).ThenBy(g => g.Key.W)
            .Select(g => (StartOfWeek(g.First().Date, firstDay), (float)g.Average(v => v.SuccessRate)))
            .ToList();
    }

    public static List<(DateTime weekStart, float reaction)> ToWeeklyAvgReaction(IEnumerable<ProgressRow> data)
    {
        var cal = CultureInfo.InvariantCulture.Calendar;
        const CalendarWeekRule rule = CalendarWeekRule.FirstFourDayWeek;
        const DayOfWeek firstDay = DayOfWeek.Monday;

        return data
            .GroupBy(r => new { Y = GetWeekYear(r.Date, cal, rule, firstDay), W = cal.GetWeekOfYear(r.Date, rule, firstDay) })
            .OrderBy(g => g.Key.Y).ThenBy(g => g.Key.W)
            .Select(g => (StartOfWeek(g.First().Date, firstDay), (float)g.Average(v => v.Reaction)))
            .ToList();
    }

    public static List<(DateTime month, float success)> ToMonthlyAvgSuccess(IEnumerable<ProgressRow> data)
    {
        return data
            .GroupBy(r => new { r.Date.Year, r.Date.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => (new DateTime(g.Key.Year, g.Key.Month, 1), (float)g.Average(v => v.SuccessRate)))
            .ToList();
    }

    public static List<(DateTime month, float reaction)> ToMonthlyAvgReaction(IEnumerable<ProgressRow> data)
    {
        return data
            .GroupBy(r => new { r.Date.Year, r.Date.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => (new DateTime(g.Key.Year, g.Key.Month, 1), (float)g.Average(v => v.Reaction)))
            .ToList();
    }

    // ----------------------- ВСПОМОГАТЕЛЬНОЕ (как в вашей версии) -----------------------

    /// <summary>Начало недели (понедельник, либо указанный firstDay).</summary>
    public static DateTime StartOfWeek(DateTime dt, DayOfWeek firstDay = DayOfWeek.Monday)
    {
        int diff = (7 + (dt.DayOfWeek - firstDay)) % 7;
        return dt.Date.AddDays(-diff);
    }

    /// <summary>
    /// «Год недели», который может отличаться от календарного для первых/последних дней года.
    /// </summary>
    private static int GetWeekYear(DateTime date, Calendar cal,
                                   CalendarWeekRule rule, DayOfWeek firstDay)
    {
        int week = cal.GetWeekOfYear(date, rule, firstDay);
        int year = date.Year;

        if (date.Month == 1 && week >= 52) return year - 1; // принадлежит прошлому году
        if (date.Month == 12 && week == 1) return year + 1;  // принадлежит следующему году
        return year;
    }

    private readonly struct WeekKey
    {
        public readonly int Year;
        public readonly int Week;
        public WeekKey(int year, int week) { Year = year; Week = week; }
    }
}
