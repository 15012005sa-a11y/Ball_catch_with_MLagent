using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmOverlayPanel : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup cg;           // CanvasGroup �� ConfirmOverlay
    public TMP_Text title;
    public TMP_Text body;
    public Button yesBtn;
    public Button noBtn;

    int pendingPatientId = -1;

    void Awake()
    {
        Hide(true);                   // �� ������ ������ �������� � ���������
        if (yesBtn) yesBtn.onClick.AddListener(OnYes);
        if (noBtn) noBtn.onClick.AddListener(OnNo);
    }

    void OnDestroy()
    {
        if (yesBtn) yesBtn.onClick.RemoveListener(OnYes);
        if (noBtn) noBtn.onClick.RemoveListener(OnNo);
    }

    public void Show(int patientId, string titleText = "������� ��������",
                     string bodyText = "�� ����� ������ �������?")
    {
        pendingPatientId = patientId;
        if (title) title.text = titleText;
        if (body) body.text = bodyText;

        gameObject.SetActive(true);
        if (cg)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true; // ����� �� ������� � ����
        }
    }

    public void Hide(bool immediate = false)
    {
        if (cg)
        {
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
        pendingPatientId = -1;
    }

    void OnYes()
    {
        if (pendingPatientId >= 0 && PatientManager.Instance != null)
            PatientManager.Instance.RemovePatient(pendingPatientId);
        Hide();
    }

    void OnNo() => Hide();
}
