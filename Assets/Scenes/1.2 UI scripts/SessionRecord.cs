using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Единица истории: один игровой сеанс пациента.
/// Обратите внимание на ИМЕНА полей — именно их ждут другие скрипты.
/// </summary>
[Serializable]
public class SessionRecord
{
    // Время старта/окончания (UTC)
    public DateTime startUtc = DateTime.UtcNow;
    public DateTime endUtc = DateTime.UtcNow;

    // Счётчики попыток
    public int attempts;        // всего попыток за сессию
    public int success;         // успешных (поймано и т.п.)

    // Времена реакции в миллисекундах (для средней/медианы/StdDev)
    public List<int> reactionTimesMs = new List<int>();

    // Необязательные агрегаты — удобно для KPI/отладки
    public int totalScore;              // общий счёт
    public float successRate;           // 0..1 (дублирует success/attempts)
    public float playTimeSec;           // длительность в секундах

    // Хелперы (не обязательны, но удобно)
    public TimeSpan Duration => endUtc - startUtc;
    public double MeanRT => reactionTimesMs.Count == 0 ? 0 : reactionTimesMs.Average();
    public double MedianRT => reactionTimesMs.Count == 0 ? 0 : reactionTimesMs.OrderBy(v => v).ElementAt(reactionTimesMs.Count / 2);
    public double StdDevRT
    {
        get
        {
            if (reactionTimesMs.Count == 0) return 0;
            var avg = reactionTimesMs.Average();
            var variance = reactionTimesMs.Average(v => (v - avg) * (v - avg));
            return Math.Sqrt(variance);
        }
    }

    public override string ToString()
        => $"[{startUtc:u}..{endUtc:u}] attempts={attempts}, success={success}, score={totalScore}";
}