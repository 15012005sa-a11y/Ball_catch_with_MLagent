// KpiTestDataSeeder.cs
// Генератор тестовых сессий для выбранного пациента.
// Повесьте на любой объект сцены AppShell, укажите PatientManager и нажмите кнопку Seed Now в инспекторе.

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class KpiTestDataSeeder : MonoBehaviour
{
    [Header("Refs")][SerializeField] private PatientManager patientManager;

    [Header("Seeding")]
    [Tooltip("Создавать данные при старте Play Mode")][SerializeField] private bool autoSeedOnPlay = false;
    [Tooltip("Очищать текущую историю пациента перед генерацией")][SerializeField] private bool clearBefore = true;
    [Tooltip("Сколько дней назад генерировать")][SerializeField] private int daysBack = 14;
    [Tooltip("Мин/макс кол-во сессий в день")][SerializeField] private Vector2Int sessionsPerDay = new Vector2Int(1, 2);
    [Tooltip("Диапазон длительности одной сессии (сек)")][SerializeField] private Vector2 durationSecRange = new Vector2(45, 120);
    [Tooltip("Базовое число попыток на сессию")][SerializeField] private int baseAttempts = 20;
    [Tooltip("Средняя доля успешных попыток (0..1)")][Range(0f, 1f)][SerializeField] private float meanSuccess = 0.7f;
    [Tooltip("Разброс доли успешных")][SerializeField] private float successDev = 0.15f;
    [Tooltip("Среднее RT (мс)")][SerializeField] private float meanRtMs = 650f;
    [Tooltip("Разброс RT (мс) — 3σ ≈ этот параметр")][SerializeField] private float rtNoiseMs = 250f;
    [Tooltip("Количество замеров реакции на сессию")][SerializeField] private Vector2Int reactionSamples = new Vector2Int(12, 26);
    [Tooltip("Seed для Random. -1 = случайный")][SerializeField] private int seed = 12345;

    private System.Random _rng;

    private void Reset()
    {
        if (!patientManager) patientManager = FindObjectOfType<PatientManager>();
    }

    private void Awake()
    {
        if (!patientManager) patientManager = FindObjectOfType<PatientManager>();
        _rng = (seed < 0) ? new System.Random() : new System.Random(seed);
    }

    private void Start()
    {
        if (autoSeedOnPlay) SeedNow();
    }

    [ContextMenu("Seed Now")]
    public void SeedNowContext()
    {
        SeedNow();
    }

    [ContextMenu("Seed Now")]
    public void SeedNow()
    {
        if (!patientManager)
            patientManager = FindObjectOfType<PatientManager>(true);

        if (!patientManager)
        {
            Debug.LogWarning("[KpiTestDataSeeder] PatientManager не назначен");
            return;
        }
        var pid = patientManager.SelectedPatientId;
        if (string.IsNullOrEmpty(pid))
        {
            Debug.LogWarning("[KpiTestDataSeeder] Не выбран пациент (SelectedPatientId)");
            return;
        }

        var history = patientManager.GetSessionHistory(pid);
        if (clearBefore) history.Clear();

        DateTime utcNow = DateTime.UtcNow;
        for (int d = daysBack - 1; d >= 0; d--)
        {
            int cnt = Range(sessionsPerDay.x, sessionsPerDay.y + 1);
            for (int i = 0; i < cnt; i++)
            {
                // Время в течение дня (UTC)
                var startUtc = utcNow.Date.AddDays(-d).AddHours(Range(10, 19)).AddMinutes(Range(0, 59));
                float durSec = Range(durationSecRange.x, durationSecRange.y);
                var endUtc = startUtc.AddSeconds(durSec);

                int attempts = Mathf.Max(1, baseAttempts + Range(-5, 6));
                float acc = Mathf.Clamp01((float)NextGaussian(meanSuccess, successDev));
                int success = Mathf.Clamp(Mathf.RoundToInt(attempts * acc), 0, attempts);

                // Реакции
                int n = Range(reactionSamples.x, reactionSamples.y + 1);
                var rts = new List<int>(n);
                for (int k = 0; k < n; k++)
                {
                    float rt = Mathf.Max(120f, (float)NextGaussian(meanRtMs, rtNoiseMs / 3f));
                    rts.Add(Mathf.RoundToInt(rt));
                }

                var rec = new SessionRecord();
                // Заполняем максимально совместимо с вашим форматом через рефлексию
                SetMember(rec, "startUtc", startUtc);
                SetMember(rec, "endUtc", endUtc);
                SetMember(rec, "attempts", attempts);
                SetMember(rec, "success", success);
                SetMember(rec, "reactionTimesMs", rts);
                // Дополнительно (на всякий случай):
                SetMember(rec, "dateUtc", startUtc);
                SetMember(rec, "totalScore", success);
                SetMember(rec, "playTimeSec", (float)durSec);

                patientManager.AddSessionForCurrent(rec);

                Debug.Log("[KPI Seeder] Seeded test sessions for: " +
                  (patientManager ? patientManager.Current?.displayName : "UNKNOWN"));
            }
        }

        Debug.Log($"[KpiTestDataSeeder] Сгенерировано: {patientManager.GetSessionHistory(pid).Count} записей для пациента '{pid}'");
    }

    // ===== helpers =====

    private int Range(int minInclusive, int maxExclusive)
        => _rng.Next(minInclusive, maxExclusive);

    private float Range(float minInclusive, float maxInclusive)
        => (float)(_rng.NextDouble() * (maxInclusive - minInclusive) + minInclusive);

    private double NextGaussian(double mean, double stdDev)
    {
        // Box–Muller
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = 1.0 - _rng.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * randStdNormal;
    }

    private static void SetMember<T>(object obj, string name, T value)
    {
        var t = obj.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        var f = t.GetField(name, flags);
        if (f != null && f.FieldType.IsAssignableFrom(typeof(T))) { f.SetValue(obj, value); return; }
        var p = t.GetProperty(name, flags);
        if (p != null && p.CanWrite && p.PropertyType.IsAssignableFrom(typeof(T))) { p.SetValue(obj, value); return; }
        // тихо игнорируем, если члена нет — скрипт совместим с вашим вариантом SessionRecord
    }
}
