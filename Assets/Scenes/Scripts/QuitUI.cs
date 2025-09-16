using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class QuitUI : MonoBehaviour
{
    [Header("UI refs")]
    public Button quitButton;           // ������ ������ (�������������, ����� �������� ���� � ��� ��)
    public CanvasGroup confirmPanel;    // ������ ������������� (��/���)
    public Button yesButton;            // ���
    public Button noButton;             // ����

    [Header("Behaviour")]
    public KeyCode hotkey = KeyCode.F10;  // ������ �������� ������
    public bool forceTop = true;          // ������� ������ ������ ���� Canvas
    public int sortingOrder = 6000;       // ������� ���������� ��� �������

    void Awake()
    {
        if (quitButton)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(ShowConfirm);
        }

        if (yesButton)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(QuitApp);
        }

        if (noButton)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(HideConfirm);
        }

        HideConfirm();

        if (forceTop && confirmPanel)
            EnsureTopmost(confirmPanel.gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(hotkey)) ShowConfirm();

        // Esc ��������� ������, ���� �� ������
        if (confirmPanel && confirmPanel.interactable && Input.GetKeyDown(KeyCode.Escape))
            HideConfirm();
    }

    public void ShowConfirm()
    {
        if (!confirmPanel) return;

        if (forceTop) EnsureTopmost(confirmPanel.gameObject);
        confirmPanel.transform.SetAsLastSibling();

        confirmPanel.gameObject.SetActive(true);
        confirmPanel.alpha = 1f;
        confirmPanel.interactable = true;
        confirmPanel.blocksRaycasts = true;
    }

    public void HideConfirm()
    {
        if (!confirmPanel) return;

        confirmPanel.gameObject.SetActive(true); // �� ��������� ������, ����� �� ������ ������
        confirmPanel.alpha = 0f;
        confirmPanel.interactable = false;
        confirmPanel.blocksRaycasts = false;
    }

    public void QuitApp()
    {
        // �� ������ ������ ������� ����� � ��������� ���������� ����������
        Time.timeScale = 1f;

        try { FindObjectOfType<BallSpawnerBallCatch>()?.StopSpawning(); } catch { }
        try { FindObjectOfType<MotionLogger>()?.StopLogging("Quit"); } catch { }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ������ ������ ������ ���� UI
    private void EnsureTopmost(GameObject go)
    {
        var cv = go.GetComponent<Canvas>();
        if (!cv) cv = go.AddComponent<Canvas>();
        cv.overrideSorting = true;
        cv.sortingOrder = sortingOrder;

        if (!go.TryGetComponent<GraphicRaycaster>(out _))
            go.AddComponent<GraphicRaycaster>();
    }
}
