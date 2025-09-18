using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Корневой контейнер для сериализации пациентов в файл.
/// (Не трогаем — это обёртка для JsonUtility.)
/// </summary>
[Serializable]
public class PatientSave
{
    public Patient[] patients;
}

/// <summary>
/// Главный менеджер пациентов.
/// Совместим со старыми и новыми скриптами проекта:
/// - события: OnPatientsChanged, OnSelectedPatientChanged
/// - адаптеры: CurrentPatientID, SelectedPatientId, Current
/// - история сеансов: GetSessionHistory(int/string), AddSessionForCurrent(...)
/// - «мост»: NotifyPatientsChanged()
/// </summary>
public class PatientManager : MonoBehaviour
{
    public static PatientManager Instance { get; private set; }

    [Header("Initial data (used only if no save file found)")]
    [Tooltip("Стартовый набор пациентов на первый запуск.")]
    public Patient[] patients = Array.Empty<Patient>();

    [Header("Selection")]
    [Tooltip("Индекс выбранного пациента в массиве patients.")]
    public int selectedIndex = 0;

    // ДОБАВИТЬ: быстрый доступ к пациенту по id (понадобится ExcelExporter’у)
    public Patient FindById(int id)
    {
        if (patients == null) return null;
        for (int i = 0; i < patients.Length; i++)
            if (patients[i].id == id) return patients[i];
        return null;
    }

    // ====== Доступ к текущему пациенту ======
    public Patient Current =>
        (patients != null && patients.Length > 0)
            ? patients[Mathf.Clamp(selectedIndex, 0, patients.Length - 1)]
            : null;

    /// <summary>Старый адаптер: ID текущего пациента (или -1, если не выбран).</summary>
    public int CurrentPatientID => Current != null ? Current.id : -1;

    /// <summary>
    /// Новый адаптер для скриптов, ожидавших строковый ID.
    /// (Например, KPI-тестер или мосты сессий.)
    /// Берём имя/ид или конкатенацию, чтобы поле не было пустым.
    /// </summary>
    public string SelectedPatientId
    {
        get
        {
            var p = Current;
            if (p == null) return null;
            // Пытаемся вернуть осмысленный строковый идентификатор.
            // В приоритете — явное текстовое поле (если у вас есть), иначе "id:Имя".
            return !string.IsNullOrEmpty(p.displayName)
                ? $"{p.id}:{p.displayName}"
                : p.id.ToString();
        }
    }

    // ====== События (под них уже подписывается ваш UI) ======
    /// <summary>Вызывается при любом изменении состава/данных пациентов.</summary>
    public event Action OnPatientsChanged;

    /// <summary>Вызывается при смене выбранного пациента.</summary>
    public event Action<Patient> OnSelectedPatientChanged;

    // Для обратной совместимости со старыми скриптами,
    // которые ждали метод с таким названием:
    public void NotifyPatientsChanged() => OnPatientsChanged?.Invoke();

    // ====== История сеансов по пациентам (в рантайме) ======
    // Поддерживаем два ключа — и int ID, и string ID — чтобы ничего не ломать.
    private readonly Dictionary<int, List<SessionRecord>> _historyByInt =
        new Dictionary<int, List<SessionRecord>>();

    private readonly Dictionary<string, List<SessionRecord>> _historyByString =
        new Dictionary<string, List<SessionRecord>>(StringComparer.OrdinalIgnoreCase);

    public List<SessionRecord> GetSessionHistory(int patientId)
    {
        if (!_historyByInt.TryGetValue(patientId, out var list))
        {
            list = new List<SessionRecord>();
            _historyByInt[patientId] = list;
        }
        return list;
    }

    public List<SessionRecord> GetSessionHistory(string patientId)
    {
        if (string.IsNullOrEmpty(patientId))
            return new List<SessionRecord>();

        if (!_historyByString.TryGetValue(patientId, out var list))
        {
            list = new List<SessionRecord>();
            _historyByString[patientId] = list;
        }
        return list;
    }

    /// <summary>
    /// Универсальная точка добавления записи к выбранному пациенту.
    /// Пишем сразу в оба «хранилища» (int и string), чтобы все читатели получили данные.
    /// </summary>
    public void AddSessionForCurrent(SessionRecord record)
    {
        if (record == null) return;

        var p = Current;
        if (p != null)
            GetSessionHistory(p.id).Add(record);

        var s = SelectedPatientId;
        if (!string.IsNullOrEmpty(s))
            GetSessionHistory(s).Add(record);

        // Сообщим UI (KPI, карточки и т.д.), что данные могли измениться.
        OnSelectedPatientChanged?.Invoke(Current);
        OnPatientsChanged?.Invoke();
    }

    // ====== Жизненный цикл и сохранение ======
    string SavePath => Path.Combine(Application.persistentDataPath, "patients.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Загружаем из файла; если файла нет — создаём дефолт и сохраняем.
        if (!LoadPatientsFromDisk())
        {
            if (patients == null || patients.Length == 0)
            {
                patients = new Patient[2]
                {
                    new Patient{
                        id = 1, displayName = "Patient 1", age = 62, startedRehab = "01.09.25",
                        settings = new GameSettings{ level1DurationSec=180, level2DurationSec=120, restTimeSec=60, redChance=0.35f }
                    },
                    new Patient{
                        id = 2, displayName = "Patient 2", age = 58, startedRehab = "21.08.25",
                        settings = new GameSettings{ level1DurationSec=180, level2DurationSec=120, restTimeSec=60, redChance=0.35f }
                    },
                };
            }
            SavePatientsToDisk();
        }

        // Нормализуем выбор.
        if (patients == null || patients.Length == 0) selectedIndex = 0;
        else selectedIndex = Mathf.Clamp(selectedIndex, 0, patients.Length - 1);
    }

    private void OnApplicationQuit()
    {
        SavePatientsToDisk();
    }

    // ====== Операции выбора ======
    public void SelectByIndex(int index)
    {
        if (patients == null || patients.Length == 0)
        {
            selectedIndex = 0;
            OnSelectedPatientChanged?.Invoke(null);
            return;
        }

        selectedIndex = Mathf.Clamp(index, 0, patients.Length - 1);
        OnSelectedPatientChanged?.Invoke(Current);
    }

    public void SelectById(int id)
    {
        if (patients == null) return;
        for (int i = 0; i < patients.Length; i++)
            if (patients[i].id == id) { SelectByIndex(i); return; }
    }

    // ====== Операции над списком пациентов ======
    public int GetNextId() =>
        (patients == null || patients.Length == 0) ? 1 : patients.Max(p => p.id) + 1;

    public void AddPatient(Patient p)
    {
        var list = new List<Patient>(patients ?? Array.Empty<Patient>());
        list.Add(p);
        patients = list.ToArray();

        SavePatientsToDisk();
        OnPatientsChanged?.Invoke();
        SelectByIndex(patients.Length - 1);

        // НОВОЕ: создаём пустой CSV под этого пациента (если его ещё нет)
        TryCreatePatientCsvSkeleton(p);
    }

    public void RemovePatient(int id)
    {
        var list = new List<Patient>(patients ?? Array.Empty<Patient>());
        int idx = list.FindIndex(x => x.id == id);
        if (idx < 0) return;

        list.RemoveAt(idx);
        patients = list.ToArray();

        _historyByInt.Remove(id); // подчистим историю по int
        // по string чистим «мягко»: если нужно, можно также пройтись и удалить ключи,
        // начинающиеся с $"{id}:" — оставим как есть, чтобы не трогать возможные внешние ключи.

        if (patients.Length == 0) selectedIndex = 0;
        else selectedIndex = Mathf.Clamp(selectedIndex > idx ? selectedIndex - 1 : selectedIndex, 0, patients.Length - 1);

        OnPatientsChanged?.Invoke();
        OnSelectedPatientChanged?.Invoke(Current);
    }

    /// <summary>Сохранить текущие данные и уведомить UI.</summary>
    public void SaveCurrentAndNotify()
    {
        SavePatientsToDisk();
        OnSelectedPatientChanged?.Invoke(Current);
        OnPatientsChanged?.Invoke();
    }

    public void ClearSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
    }

    // ====== Сериализация ======
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
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PatientManager] Load error: {e.Message}");
        }
        return false;
    }

    // НОВОЕ: заготовка с заголовком, чтобы Excel сразу открывал файл
    void TryCreatePatientCsvSkeleton(Patient p)
    {
        if (p == null) return;

        var exporter = FindObjectOfType<ExcelExporter>(includeInactive: true);
        if (exporter == null) return; // нет экспортёра в сцене — просто пропустим

        // ✅ фикс CS0176: обращаемся через имя типа
        string folder = Path.Combine(Application.persistentDataPath, ExcelExporter.progressFolder);
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        string safe = MakeSafeFileName($"{p.displayName}_patientProgress.csv");
        string path = Path.Combine(folder, safe);

        if (!File.Exists(path))
        {
            File.WriteAllText(path, "PatientID,Date,Score,Success,Reaction,Right hand,Left hand\n", new UTF8Encoding(true));
            Debug.Log($"[PatientManager] Created patient CSV: {path}");
        }
    }


    static string MakeSafeFileName(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        while (s.Contains("__")) s = s.Replace("__", "_");
        return s.Trim();
    }

    void SavePatientsToDisk()
    {
        try
        {
            var pack = new PatientSave { patients = patients ?? Array.Empty<Patient>() };
            string json = JsonUtility.ToJson(pack, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PatientManager] Save error: {e.Message}");
        }
    }
}
