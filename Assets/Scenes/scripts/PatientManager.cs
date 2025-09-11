using System;
using System.Collections.Generic;
using System.Linq;           // для Max(...)
using System.IO;            // для работы с файлом
using UnityEngine;

[Serializable]                 // упаковка данных для JsonUtility
public class PatientSave
{
    public Patient[] patients;
}

public class PatientManager : MonoBehaviour
{
    public static PatientManager Instance { get; private set; }

    [Header("Seed data for first run (used if no save file)")]
    public Patient[] patients = new Patient[0];

    public int selectedIndex = 0;

    public Patient Current =>
        (patients != null && patients.Length > 0)
            ? patients[Mathf.Clamp(selectedIndex, 0, patients.Length - 1)]
            : null;

    // Адаптер для старого кода
    public int CurrentPatientID => Current != null ? Current.id : -1;

    // ==== События ====
    public event Action<Patient> OnSelectedPatientChanged;
    public event Action OnPatientsChanged;

    // ===== История сеансов по пациентам (в рантайме; не сохраняем) =====
    private readonly Dictionary<int, List<SessionRecord>> _history =
        new Dictionary<int, List<SessionRecord>>();

    public List<SessionRecord> GetSessionHistory(int patientId)
    {
        if (!_history.TryGetValue(patientId, out var list))
        {
            list = new List<SessionRecord>();
            _history[patientId] = list;
        }
        return list;
    }

    public void AddSessionRecord(int patientId, SessionRecord record)
    {
        GetSessionHistory(patientId).Add(record);
    }
    // ===================================================================

    // ---------- путь к файлу сохранения ----------
    string SavePath => Path.Combine(Application.persistentDataPath, "patients.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);        // уничтожаем дубликаты при загрузке другой сцены
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);  // переносим менеджер между сценами
    
        // DontDestroyOnLoad(gameObject); // включите, если нужен между сценами

        // Пробуем загрузить сохранение; если файла нет — создаём дефолт и сохраняем
        if (!LoadPatientsFromDisk())
        {
            patients = new Patient[2]
            {
                new Patient{
                    id=1, displayName="Patient 1", age=62, startedRehab="01.09.25",
                    settings = new GameSettings{ level1DurationSec=180, level2DurationSec=120, restTimeSec=60, redChance=0.35f }
                },
                new Patient{
                    id=2, displayName="Patient 2", age=58, startedRehab="21.08.25",
                    settings = new GameSettings{ level1DurationSec=180, level2DurationSec=120, restTimeSec=60, redChance=0.35f }
                },
            };
            SavePatientsToDisk();
        }
    }

    public void RemovePatient(int id)
    {
        var list = new List<Patient>(patients ?? System.Array.Empty<Patient>());
        int idx = list.FindIndex(p => p.id == id);
        if (idx < 0) return;                // такого нет — выходим

        list.RemoveAt(idx);
        patients = list.ToArray();

        // если ведёте истории — подчистим
        _history.Remove(id);

        // поправим выбранный индекс
        if (patients.Length == 0) selectedIndex = 0;
        else selectedIndex = Mathf.Clamp(selectedIndex > idx ? selectedIndex - 1 : selectedIndex, 0, patients.Length - 1);

        OnPatientsChanged?.Invoke();        // перестроить карточки
        OnSelectedPatientChanged?.Invoke(Current);
    }

    private void OnApplicationQuit()
    {
        SavePatientsToDisk();
    }

    // ---------- Операции выбора ----------
    public void SelectByIndex(int index)
    {
        int max = Mathf.Max(0, (patients?.Length ?? 1) - 1);
        selectedIndex = Mathf.Clamp(index, 0, max);
        OnSelectedPatientChanged?.Invoke(Current);
    }

    public void SelectById(int id)
    {
        if (patients == null) return;
        for (int i = 0; i < patients.Length; i++)
            if (patients[i].id == id) { SelectByIndex(i); return; }
    }

    // ---------- Операции со списком пациентов ----------
    public int GetNextId() =>
        (patients == null || patients.Length == 0) ? 1 : patients.Max(p => p.id) + 1;

    public void AddPatient(Patient p)
    {
        var list = new List<Patient>(patients ?? Array.Empty<Patient>());
        list.Add(p);
        patients = list.ToArray();

        SavePatientsToDisk();            // сохраняем сразу
        OnPatientsChanged?.Invoke();     // уведомляем UI
        SelectByIndex(patients.Length - 1);
    }

    /// Вызовите после изменения настроек текущего пациента,
    /// чтобы правки попали в файл и UI обновился.
    public void SaveCurrentAndNotify()
    {
        SavePatientsToDisk();
        OnSelectedPatientChanged?.Invoke(Current);
        OnPatientsChanged?.Invoke();
    }

    // Опционально: очистить сохранение (для сброса/отладки)
    public void ClearSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
    }

    // ---------- Сериализация ----------
    bool LoadPatientsFromDisk()
    {
        try
        {
            if (!File.Exists(SavePath)) return false;
            string json = File.ReadAllText(SavePath);
            var pack = JsonUtility.FromJson<PatientSave>(json);
            if (pack?.patients != null && pack.patients.Length > 0)
            {
                patients = pack.patients;
                selectedIndex = Mathf.Clamp(selectedIndex, 0, patients.Length - 1);
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PatientManager] Load error: {e.Message}");
        }
        return false;
    }

    void SavePatientsToDisk()
    {
        try
        {
            var pack = new PatientSave { patients = patients ?? Array.Empty<Patient>() };
            string json = JsonUtility.ToJson(pack, true);
            File.WriteAllText(SavePath, json);
            // Debug.Log($"[PatientManager] Saved to {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PatientManager] Save error: {e.Message}");
        }
    }
}
