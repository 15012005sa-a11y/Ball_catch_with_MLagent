using System;
using System.Collections.Generic;        // ← нужно для Dictionary/List
using UnityEngine;

public class PatientManager : MonoBehaviour
{
    public static PatientManager Instance { get; private set; }

    [Header("Seed data for demo")]
    public Patient[] patients = new Patient[2]
    {
        new Patient{ id=1, displayName="Patient 1", age=62, startedRehab="01.09.25",
            settings = new GameSettings{ level1DurationSec=180, level2DurationSec=120, restTimeSec=60, redChance=0.35f } },
        new Patient{ id=2, displayName="Patient 2", age=58, startedRehab="21.08.25",
            settings = new GameSettings{ level1DurationSec=180, level2DurationSec=120, restTimeSec=60, redChance=0.35f } },
    };

    public int selectedIndex = 0;

    public Patient Current =>
        (patients != null && patients.Length > 0)
            ? patients[Mathf.Clamp(selectedIndex, 0, patients.Length - 1)]
            : null;

    // ✅ Адаптер для старого кода (ScoreManager и др.)
    public int CurrentPatientID => Current != null ? Current.id : -1;

    public event Action<Patient> OnSelectedPatientChanged;

    // ===== История сеансов по пациентам =====
    private readonly Dictionary<int, List<SessionRecord>> _history =
        new Dictionary<int, List<SessionRecord>>();

    /// <summary>Вернуть историю сеансов пациента (создаст пустую, если её ещё нет).</summary>
    public List<SessionRecord> GetSessionHistory(int patientId)
    {
        if (!_history.TryGetValue(patientId, out var list))
        {
            list = new List<SessionRecord>();
            _history[patientId] = list;
        }
        return list;
    }

    /// <summary>Добавить запись о сеансе в историю пациента.</summary>
    public void AddSessionRecord(int patientId, SessionRecord record)
    {
        GetSessionHistory(patientId).Add(record);
    }
    // ========================================

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // если менеджер нужен между сценами
    }

    public void SelectByIndex(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, patients.Length - 1);
        OnSelectedPatientChanged?.Invoke(Current);
    }

    public void SelectById(int id)
    {
        for (int i = 0; i < patients.Length; i++)
            if (patients[i].id == id) { SelectByIndex(i); return; }
    }
}
