using UnityEngine;
using TMPro;

public class SelectedLabel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;  // перетащи сюда SubTitle TMP

    private void OnEnable()
    {
        if (PatientManager.Instance != null)
        {
            PatientManager.Instance.OnSelectedPatientChanged += OnChanged;
            OnChanged(PatientManager.Instance.Current);
        }
    }

    private void OnDisable()
    {
        if (PatientManager.Instance != null)
            PatientManager.Instance.OnSelectedPatientChanged -= OnChanged;
    }

    private void OnChanged(Patient p)
    {
        if (label == null) return;
        label.text = "selected: " + (p != null ? p.displayName : "-");
    }
}
