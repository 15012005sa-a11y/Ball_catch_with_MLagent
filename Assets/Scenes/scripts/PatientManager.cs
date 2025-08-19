using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PatientManager : MonoBehaviour
{
    public static PatientManager Instance { get; private set; }

    // Текущий выбранный пациент — индекс в списке patients
    public int CurrentPatientID { get; private set; } = -1;

    [Header("Patients List")]
    public List<PatientCard> patients = new List<PatientCard>();

    // Ссылка на карточку текущего пациента
    public PatientCard currentPatient { get; private set; }

    private string savePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Path.Combine(Application.persistentDataPath, "patients.json");
            LoadPatients();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Создает новую карточку пациента и сразу его выбирает.
    /// </summary>
    public void CreatePatient(string name, int age, string gender, float ballSpeed, float interval, DifficultyLevel difficulty)
    {
        var patient = new PatientCard(name, age, gender, ballSpeed, interval, difficulty);
        patients.Add(patient);
        CurrentPatientID = patients.Count - 1;
        currentPatient = patient;
        SavePatients();
    }

    /// <summary>
    /// Выбирает пациента по индексу в списке.
    /// </summary>
    public void SelectPatient(int id)
    {
        if (id >= 0 && id < patients.Count)
        {
            CurrentPatientID = id;
            currentPatient = patients[id];
        }
        else
        {
            Debug.LogWarning($"[SelectPatient] Пациент с ID {id} не найден.");
        }
    }

    /// <summary>
    /// Выбирает пациента по имени.
    /// </summary>
    public void SelectPatient(string name)
    {
        int index = patients.FindIndex(p => p.patientName == name);
        if (index >= 0)
        {
            SelectPatient(index);
        }
        else
        {
            Debug.LogWarning($"[SelectPatient] Пациент с именем '{name}' не найден.");
        }
    }

    /// <summary>
    /// Добавляет запись сессии текущему пациенту.
    /// </summary>
    public void AddSessionRecord(SessionRecord record)
    {
        if (currentPatient != null)
        {
            currentPatient.AddSessionRecord(record);
            SavePatients();
        }
        else
        {
            Debug.LogWarning("[AddSessionRecord] Пациент не выбран. Сессия не сохранена.");
        }
    }

    /// <summary>
    /// Сохраняет всех пациентов и их данные в JSON-файл.
    /// </summary>
    public void SavePatients()
    {
        var wrapper = new PatientListWrapper(patients);
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"[SavePatients] Данные сохранены: {savePath}");
        Debug.Log("Data folder: " + Application.persistentDataPath);

    }

    /// <summary>
    /// Загружает пациентов из JSON. Если список не пуст — автоматически выбирает первого.
    /// </summary>
    public void LoadPatients()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            var wrapper = JsonUtility.FromJson<PatientListWrapper>(json);
            patients = wrapper.patients ?? new List<PatientCard>();
            Debug.Log($"[LoadPatients] Загружено {patients.Count} пациентов из {savePath}");
        }
        else
        {
            Debug.Log($"[LoadPatients] Файл не найден ({savePath}). Создаём пустой список.");
            patients = new List<PatientCard>();
        }

        // Автовыбор первого пациента
        if (patients.Count > 0)
        {
            SelectPatient(0);
            Debug.Log($"[LoadPatients] Автовыбран пациент ID 0: {patients[0].patientName}");
        }
    }

    [System.Serializable]
    private class PatientListWrapper
    {
        public List<PatientCard> patients;
        public PatientListWrapper(List<PatientCard> list) { patients = list; }
    }
}
