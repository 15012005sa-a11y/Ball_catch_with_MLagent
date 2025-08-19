using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PatientManager : MonoBehaviour
{
    public static PatientManager Instance { get; private set; }

    // ������� ��������� ������� � ������ � ������ patients
    public int CurrentPatientID { get; private set; } = -1;

    [Header("Patients List")]
    public List<PatientCard> patients = new List<PatientCard>();

    // ������ �� �������� �������� ��������
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
    /// ������� ����� �������� �������� � ����� ��� ��������.
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
    /// �������� �������� �� ������� � ������.
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
            Debug.LogWarning($"[SelectPatient] ������� � ID {id} �� ������.");
        }
    }

    /// <summary>
    /// �������� �������� �� �����.
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
            Debug.LogWarning($"[SelectPatient] ������� � ������ '{name}' �� ������.");
        }
    }

    /// <summary>
    /// ��������� ������ ������ �������� ��������.
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
            Debug.LogWarning("[AddSessionRecord] ������� �� ������. ������ �� ���������.");
        }
    }

    /// <summary>
    /// ��������� ���� ��������� � �� ������ � JSON-����.
    /// </summary>
    public void SavePatients()
    {
        var wrapper = new PatientListWrapper(patients);
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"[SavePatients] ������ ���������: {savePath}");
        Debug.Log("Data folder: " + Application.persistentDataPath);

    }

    /// <summary>
    /// ��������� ��������� �� JSON. ���� ������ �� ���� � ������������� �������� �������.
    /// </summary>
    public void LoadPatients()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            var wrapper = JsonUtility.FromJson<PatientListWrapper>(json);
            patients = wrapper.patients ?? new List<PatientCard>();
            Debug.Log($"[LoadPatients] ��������� {patients.Count} ��������� �� {savePath}");
        }
        else
        {
            Debug.Log($"[LoadPatients] ���� �� ������ ({savePath}). ������ ������ ������.");
            patients = new List<PatientCard>();
        }

        // ��������� ������� ��������
        if (patients.Count > 0)
        {
            SelectPatient(0);
            Debug.Log($"[LoadPatients] ���������� ������� ID 0: {patients[0].patientName}");
        }
    }

    [System.Serializable]
    private class PatientListWrapper
    {
        public List<PatientCard> patients;
        public PatientListWrapper(List<PatientCard> list) { patients = list; }
    }
}
