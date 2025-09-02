using UnityEngine;
using UnityEngine.UI;

public class PatientCardHighlight : MonoBehaviour
{
    public int patientId = 1;
    public Image background; // перетащи Image карточки
    public Color normal = new Color32(0x3B, 0x54, 0xB5, 0xFF);
    public Color selected = new Color32(0x4A, 0x6B, 0xD6, 0xFF);

    private void OnEnable()
    {
        if (PatientManager.Instance != null)
        {
            PatientManager.Instance.OnSelectedPatientChanged += _ => Refresh();
            Refresh();
        }
    }
    private void OnDisable()
    {
        if (PatientManager.Instance != null)
            PatientManager.Instance.OnSelectedPatientChanged -= _ => Refresh();
    }
    private void Refresh()
    {
        if (background == null) return;
        var cur = PatientManager.Instance?.Current?.id ?? -1;
        background.color = (cur == patientId) ? selected : normal;
    }
}
