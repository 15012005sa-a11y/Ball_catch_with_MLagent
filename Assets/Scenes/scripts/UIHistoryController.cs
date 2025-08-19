using UnityEngine;

public class UIHistoryController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject gamePanel;    // Canvas/GamePanel
    [SerializeField] private GameObject historyPanel; // Canvas/HistoryPanel

    [Header("History logic (optional)")]
    // �������� ���� ������, �� ������� ����� HistoryController
    [SerializeField] private GameObject historyControllerObject;

    private void Start()
    {
        if (gamePanel) gamePanel.SetActive(true);
        if (historyPanel) historyPanel.SetActive(false);
    }

    // ������ "History" (OnClick)
    public void ShowHistory()
    {
        if (gamePanel) gamePanel.SetActive(false);
        if (historyPanel) historyPanel.SetActive(true);

        // ����� ������� ������ �� HistoryController, ���� ��� ����
        if (historyControllerObject)
        {
            historyControllerObject.SendMessage("ReloadFromDisk", SendMessageOptions.DontRequireReceiver);
            historyControllerObject.SendMessage("Redraw", SendMessageOptions.DontRequireReceiver);
        }
    }

    // ������ "Back" (OnClick)
    public void CloseHistory()
    {
        if (historyPanel) historyPanel.SetActive(false);
        if (gamePanel) gamePanel.SetActive(true);
    }

    private void Update()
    {
        // ESC ��������� �������
        if (historyPanel && historyPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            CloseHistory();
    }
}
