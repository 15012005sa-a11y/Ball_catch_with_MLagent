using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PatientSelectButton : MonoBehaviour
{
    [Header("ID этого пациента (задаётся биндeром)")]
    public int patientId;

    [Header("Иконка/рамка выбранного (опционально)")]
    public GameObject selectedMarker;

    private Button _btn;

    private void Awake()
    {
        _btn = GetComponent<Button>();
        if (_btn)
        {
            _btn.onClick.RemoveAllListeners();
            _btn.onClick.AddListener(OnClick);
        }
    }

    private void OnEnable()
    {
        var pm = PatientManager.Instance;
        if (pm != null)
        {
            // Подписываемся ТОЛЬКО на существующее событие
            pm.OnSelectedPatientChanged += OnSelectedChanged;
            UpdateSelectionMarker(pm);
        }
    }

    private void OnDisable()
    {
        var pm = PatientManager.Instance;
        if (pm != null)
            pm.OnSelectedPatientChanged -= OnSelectedChanged;
    }

    private void OnClick()
    {
        var pm = PatientManager.Instance;
        if (pm != null)
        {
            pm.SelectById(patientId);
            UpdateSelectionMarker(pm);
        }
    }

    private void OnSelectedChanged(Patient _)
    {
        UpdateSelectionMarker(PatientManager.Instance);
    }

    private void UpdateSelectionMarker(PatientManager pm)
    {
        if (!selectedMarker) return;

        bool isSelected =
            pm != null &&
            pm.Current != null &&
            pm.Current.id == patientId;

        selectedMarker.SetActive(isSelected);
    }
}
