using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class QuitUI : MonoBehaviour
{
    [Header("UI refs")]
    public Button quitButton;           // Кнопка «Выйти» (необязательно, можем показать окно и без неё)
    public CanvasGroup confirmPanel;    // Панель подтверждения (Да/Нет)
    public Button yesButton;            // «Да»
    public Button noButton;             // «Нет»

    [Header("Behaviour")]
    public KeyCode hotkey = KeyCode.F10;  // хоткей показать диалог
    public bool forceTop = true;          // сделать диалог поверх всех Canvas
    public int sortingOrder = 6000;       // порядок сортировки для диалога

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

        // Esc закрывает диалог, если он открыт
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

        confirmPanel.gameObject.SetActive(true); // не выключаем совсем, чтобы не терять ссылки
        confirmPanel.alpha = 0f;
        confirmPanel.interactable = false;
        confirmPanel.blocksRaycasts = false;
    }

    public void QuitApp()
    {
        // На всякий случай вернуть время и аккуратно остановить подсистемы
        Time.timeScale = 1f;

        try { FindObjectOfType<BallSpawnerBallCatch>()?.StopSpawning(); } catch { }
        try { FindObjectOfType<MotionLogger>()?.StopLogging("Quit"); } catch { }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Диалог всегда поверх всех UI
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
