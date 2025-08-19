using UnityEngine;

public class UIHistoryController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject gamePanel;    // Canvas/GamePanel
    [SerializeField] private GameObject historyPanel; // Canvas/HistoryPanel

    [Header("History logic (optional)")]
    // ѕеретащи сюда объект, на котором висит HistoryController
    [SerializeField] private GameObject historyControllerObject;

    private void Start()
    {
        if (gamePanel) gamePanel.SetActive(true);
        if (historyPanel) historyPanel.SetActive(false);
    }

    //  нопка "History" (OnClick)
    public void ShowHistory()
    {
        if (gamePanel) gamePanel.SetActive(false);
        if (historyPanel) historyPanel.SetActive(true);

        // ћ€гко дергаем методы на HistoryController, если они есть
        if (historyControllerObject)
        {
            historyControllerObject.SendMessage("ReloadFromDisk", SendMessageOptions.DontRequireReceiver);
            historyControllerObject.SendMessage("Redraw", SendMessageOptions.DontRequireReceiver);
        }
    }

    //  нопка "Back" (OnClick)
    public void CloseHistory()
    {
        if (historyPanel) historyPanel.SetActive(false);
        if (gamePanel) gamePanel.SetActive(true);
    }

    private void Update()
    {
        // ESC закрывает историю
        if (historyPanel && historyPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            CloseHistory();
    }
}
