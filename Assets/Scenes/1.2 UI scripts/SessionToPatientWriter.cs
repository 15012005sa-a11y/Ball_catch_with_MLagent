using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;
using Component = UnityEngine.Component;

/// <summary>
/// Пишет итоги игровой сессии выбранному пациенту в PatientManager.
/// НИЧЕГО не требует от конкретных имён полей в SessionRecord/ScoreManager –
/// все значения ищет через рефлексию по нескольким возможным именам.
///
/// Как подключить:
/// 1) Повесьте на любой объект сцены AppShell (или игровой сцены).
/// 2) В инспекторе задайте ссылки на PatientManager, ScoreManager (необязательно) и MotionLogger (необязательно).
/// 3) В ScoreManager в блоке "On Session Finished ()" добавьте вызов public-метода
///    SessionToPatientWriter.WriteCurrentSession().
///    (При желании можно также вызвать StartStamp() в момент старта игры.)
/// </summary>
public class SessionToPatientWriter : MonoBehaviour
{
    [Header("Refs (optional but recommended)")]
    [SerializeField] private PatientManager patientManager;   // обязательно, чтобы было куда писать
    [SerializeField] private Component scoreManager;           // ваш ScoreManager (тип не указываем жёстко)
    [SerializeField] private Component motionLogger;           // если именно тут лежат времена реакции

    [Header("Diagnostics")]
    [SerializeField] private bool logDetails = true;

    private DateTime _sessionStartUtc;

    #region API для привязки из инспектора
    /// <summary>Отметить время старта (можно повесить на кнопку StartGame или событие).</summary>
    public void StartStamp() => _sessionStartUtc = DateTime.UtcNow;

    /// <summary>Считать показатели и записать их в PatientManager для текущего пациента.</summary>
    public void WriteCurrentSession()
    {
        if (patientManager == null)
        {
            Debug.LogWarning("[SessionToPatientWriter] PatientManager не назначен – писать некуда.");
            return;
        }

        // Собираем числовые метрики из ScoreManager (если он есть)
        int attempts = GetInt(scoreManager,
            "attempts", "totalAttempts", "TotalAttempts", "tries", "Shots");
        int success = GetInt(scoreManager,
            "success", "successCount", "SuccessCount", "hits", "Correct");
        int currentScore = GetInt(scoreManager,
            "currentScore", "score", "CurrentScore");

        float playTimeSec = 0f;
        if (_sessionStartUtc != default)
            playTimeSec = (float)(DateTime.UtcNow - _sessionStartUtc).TotalSeconds;
        else
            playTimeSec = GetFloat(scoreManager, "playTimeSec", "SessionDurationSec", "durationSec");

        // Попробуем собрать список времён реакции (мс)
        var rts = TryGetFloatList(scoreManager,
                      "CollectedReactionTimesMs", "reactionTimesMs", "ReactionTimesMs", "rt", "latenciesMs")
                  ?? TryGetFloatList(motionLogger,
                      "CollectedReactionTimesMs", "reactionTimesMs", "ReactionTimesMs", "rt", "latenciesMs")
                  ?? new List<float>();

        // Сформируем запись. Не завязываемся на конкретный класс/поля SessionRecord –
        // он просто должен существовать в проекте.
        var rec = new SessionRecord();

        // Время
        SetIfExists(rec, DateTime.UtcNow, "dateUtc", "DateUtc", "createdUtc");
        SetIfExists(rec, _sessionStartUtc != default ? _sessionStartUtc : DateTime.UtcNow,
            "startUtc", "StartUtc", "start", "sessionStartUtc");
        SetIfExists(rec, DateTime.UtcNow, "endUtc", "EndUtc", "finishUtc", "stopUtc", "endTime", "sessionEndUtc");

        // Итоги
        SetIfExists(rec, attempts, "attempts", "TotalAttempts", "tries", "shots");
        SetIfExists(rec, success, "success", "SuccessCount", "hits", "correct");
        SetIfExists(rec, currentScore, "currentScore", "score", "TotalScore");

        float successRate = attempts > 0 ? (float)success / attempts : 0f;
        SetIfExists(rec, successRate, "successRate", "Accuracy", "accuracy");
        SetIfExists(rec, playTimeSec, "playTimeSec", "durationSec", "playSeconds");

        // Времена реакции (мс) – положим в первое подходящее поле/свойство
        SetFloatList(rec, rts, "reactionTimesMs", "reactionTimes", "rt", "latenciesMs");

        patientManager.AddSessionForCurrent(rec);

        if (logDetails)
        {
            Debug.Log($"[SessionToPatientWriter] Запись добавлена: attempts={attempts}, success={success}, score={currentScore}, rtCount={rts.Count}");
        }
    }
    #endregion

    #region Reflection helpers
    private static int GetInt(object target, params string[] names)
    {
        if (target == null) return 0;
        var t = target.GetType();
        foreach (var n in names)
        {
            var f = t.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && (f.FieldType == typeof(int) || f.FieldType == typeof(float)))
                return Convert.ToInt32(f.GetValue(target));

            var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && (p.PropertyType == typeof(int) || p.PropertyType == typeof(float)))
                return Convert.ToInt32(p.GetValue(target, null));
        }
        return 0;
    }

    private static float GetFloat(object target, params string[] names)
    {
        if (target == null) return 0f;
        var t = target.GetType();
        foreach (var n in names)
        {
            var f = t.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && (f.FieldType == typeof(float) || f.FieldType == typeof(int)))
                return Convert.ToSingle(f.GetValue(target));

            var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && (p.PropertyType == typeof(float) || p.PropertyType == typeof(int)))
                return Convert.ToSingle(p.GetValue(target, null));
        }
        return 0f;
    }

    private static List<float> TryGetFloatList(object target, params string[] names)
    {
        if (target == null) return null;
        var t = target.GetType();
        foreach (var n in names)
        {
            var f = t.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && typeof(IList<float>).IsAssignableFrom(f.FieldType))
            {
                var list = (IList<float>)f.GetValue(target);
                return list != null ? new List<float>(list) : new List<float>();
            }

            var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && typeof(IList<float>).IsAssignableFrom(p.PropertyType))
            {
                var list = (IList<float>)p.GetValue(target, null);
                return list != null ? new List<float>(list) : new List<float>();
            }
        }
        return null;
    }

    private static void SetIfExists(object target, object value, params string[] names)
    {
        var t = target.GetType();
        foreach (var n in names)
        {
            var f = t.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && f.FieldType.IsAssignableFrom(value?.GetType() ?? typeof(object)))
            {
                f.SetValue(target, value);
                return;
            }

            var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.CanWrite && p.PropertyType.IsAssignableFrom(value?.GetType() ?? typeof(object)))
            {
                p.SetValue(target, value, null);
                return;
            }
        }
    }

    private static void SetFloatList(object target, List<float> list, params string[] names)
    {
        var t = target.GetType();
        foreach (var n in names)
        {
            var f = t.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && typeof(IList<float>).IsAssignableFrom(f.FieldType))
            {
                f.SetValue(target, list != null ? new List<float>(list) : new List<float>());
                return;
            }

            var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.CanWrite && typeof(IList<float>).IsAssignableFrom(p.PropertyType))
            {
                p.SetValue(target, list != null ? new List<float>(list) : new List<float>(), null);
                return;
            }
        }
    }
    #endregion
}