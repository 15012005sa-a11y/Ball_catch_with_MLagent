using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PatientUIManager : MonoBehaviour
{
    [Header("Inputs для создания пациента")]
    public TMP_InputField patientNameInput;
    public TMP_InputField ageInput;
    public TMP_InputField genderInput;
    public TMP_InputField ballSpeedInput;
    public TMP_InputField intervalInput;
    public TMP_Dropdown difficultyDropdown;  // значения: 0=Easy,1=Medium,2=Hard,3=Adaptive

    [Header("UI для выбора и истории")]
    public TMP_Dropdown patientDropdown;
    public TMP_Text historyDisplay;

    void Start()
    {
        UpdateDropdown();
    }

    public void CreatePatientButton()
    {
        string name = patientNameInput.text;
        int age = int.Parse(ageInput.text);
        string gender = genderInput.text;
        float ballSpeed = float.Parse(ballSpeedInput.text);
        float interval = float.Parse(intervalInput.text);
        DifficultyLevel difficulty = (DifficultyLevel)difficultyDropdown.value;

        PatientManager.Instance.CreatePatient(
            name,
            age,
            gender,
            ballSpeed,
            interval,
            difficulty
        );

        UpdateDropdown();
    }

    public void SelectPatientButton()
    {
        string selectedName = patientDropdown.options[patientDropdown.value].text;
        PatientManager.Instance.SelectPatient(selectedName);
    }

    public void ViewHistoryButton()
    {
        PatientCard patient = PatientManager.Instance.currentPatient;
        historyDisplay.text = "";

        if (patient != null)
        {
            foreach (var session in patient.sessionHistory)
            {
                historyDisplay.text +=
                    $"Date: {session.sessionDate}\n" +
                    $"Score: {session.score}\n" +
                    $"Success Rate: {session.successRate * 100}%\n\n";
            }
        }
        else
        {
            historyDisplay.text = "Пациент не выбран.";
        }
    }

    private void UpdateDropdown()
    {
        patientDropdown.ClearOptions();
        var names = new List<string>();
        foreach (var patient in PatientManager.Instance.patients)
            names.Add(patient.patientName);

        patientDropdown.AddOptions(names);
    }
}
