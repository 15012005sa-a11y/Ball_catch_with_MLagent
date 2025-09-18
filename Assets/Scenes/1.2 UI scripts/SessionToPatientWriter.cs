using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;
using Component = UnityEngine.Component;

/// <summary>
/// ����� ����� ������� ������ ���������� �������� � PatientManager.
/// ������ �� ������� �� ���������� ��� ����� � SessionRecord/ScoreManager �
/// ��� �������� ���� ����� ��������� �� ���������� ��������� ������.
///
/// ��� ����������:
/// 1) �������� �� ����� ������ ����� AppShell (��� ������� �����).
/// 2) � ���������� ������� ������ �� PatientManager, ScoreManager (�������������) � MotionLogger (�������������).
/// 3) � ScoreManager � ����� "On Session Finished ()" �������� ����� public-������
///    SessionToPatientWriter.WriteCurrentSession().
///    (��� ������� ����� ����� ������� StartStamp() � ������ ������ ����.)
/// </summary>
public class SessionToPatientWriter : MonoBehaviour
{
    [Header("Refs (optional but recommended)")]
    [SerializeField] private PatientManager patientManager;   // �����������, ����� ���� ���� ������
    [SerializeField] private Component scoreManager;           // ��� ScoreManager (��� �� ��������� �����)
    [SerializeField] private Component motionLogger;           // ���� ������ ��� ����� ������� �������

    [Header("Diagnostics")]
    [SerializeField] private bool logDetails = true;

    private DateTime _sessionStartUtc;

    #region API ��� �������� �� ����������
    /// <summary>�������� ����� ������ (����� �������� �� ������ StartGame ��� �������).</summary>
    public void StartStamp() => _sessionStartUtc = DateTime.UtcNow;

    /// <summary>������� ���������� � �������� �� � PatientManager ��� �������� ��������.</summary>
    public void WriteCurrentSession()
    {
        if (patientManager == null)
        {
            Debug.LogWarning("[SessionToPatientWriter] PatientManager �� �������� � ������ ������.");
            return;
        }

        // �������� �������� ������� �� ScoreManager (���� �� ����)
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

        // ��������� ������� ������ ����� ������� (��)
        var rts = TryGetFloatList(scoreManager,
                      "CollectedReactionTimesMs", "reactionTimesMs", "ReactionTimesMs", "rt", "latenciesMs")
                  ?? TryGetFloatList(motionLogger,
                      "CollectedReactionTimesMs", "reactionTimesMs", "ReactionTimesMs", "rt", "latenciesMs")
                  ?? new List<float>();

        // ���������� ������. �� ������������ �� ���������� �����/���� SessionRecord �
        // �� ������ ������ ������������ � �������.
        var rec = new SessionRecord();

        // �����
        SetIfExists(rec, DateTime.UtcNow, "dateUtc", "DateUtc", "createdUtc");
        SetIfExists(rec, _sessionStartUtc != default ? _sessionStartUtc : DateTime.UtcNow,
            "startUtc", "StartUtc", "start", "sessionStartUtc");
        SetIfExists(rec, DateTime.UtcNow, "endUtc", "EndUtc", "finishUtc", "stopUtc", "endTime", "sessionEndUtc");

        // �����
        SetIfExists(rec, attempts, "attempts", "TotalAttempts", "tries", "shots");
        SetIfExists(rec, success, "success", "SuccessCount", "hits", "correct");
        SetIfExists(rec, currentScore, "currentScore", "score", "TotalScore");

        float successRate = attempts > 0 ? (float)success / attempts : 0f;
        SetIfExists(rec, successRate, "successRate", "Accuracy", "accuracy");
        SetIfExists(rec, playTimeSec, "playTimeSec", "durationSec", "playSeconds");

        // ������� ������� (��) � ������� � ������ ���������� ����/��������
        SetFloatList(rec, rts, "reactionTimesMs", "reactionTimes", "rt", "latenciesMs");

        patientManager.AddSessionForCurrent(rec);

        if (logDetails)
        {
            Debug.Log($"[SessionToPatientWriter] ������ ���������: attempts={attempts}, success={success}, score={currentScore}, rtCount={rts.Count}");
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