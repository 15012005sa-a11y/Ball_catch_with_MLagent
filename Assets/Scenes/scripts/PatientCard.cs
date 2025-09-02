using System.Collections.Generic;
using UnityEngine;  // ← нужно для MonoBehaviour, Button, etc.

public class PatientCard : MonoBehaviour
{
    [Tooltip("ID пациента, который будет выбираться при клике по карточке")]
    public int patientId = 1;

    // Адаптер для старого кода: card.sessionHistory
    public List<SessionRecord> sessionHistory =>
        PatientManager.Instance != null
            ? PatientManager.Instance.GetSessionHistory(patientId)
            : _empty;

    private static readonly List<SessionRecord> _empty = new List<SessionRecord>();

    // назначь этот метод в OnClick() у Button на карточке
    public void OnClick()
    {
        if (PatientManager.Instance != null)
            PatientManager.Instance.SelectById(patientId);
    }
}
