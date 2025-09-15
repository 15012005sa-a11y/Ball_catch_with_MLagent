using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Показывает кнопку HomeButton, когда LevelDirector завершил ВСЕ уровни.
/// Безопасно работает поверх любых других контроллеров, принудительно включает
/// GameObject и CanvasGroup, поднимает объект наверх и даёт подробные логи.
/// </summary>
[DefaultExecutionOrder(100)]
public class ShowHomeButtonOnFinish : MonoBehaviour
{
    [Header("Refs")]
    public LevelDirector levelDirector;       // перетащите ваш LevelDirector (из сцены)
    public CanvasGroup homeButtonGroup;       // CanvasGroup на объекте HomeButton
    public GameObject homeButtonObject;       // сам GameObject HomeButton (если оставить пустым — возьмём из homeButtonGroup)

    [Header("Behaviour")]
    public bool hideAtStart = true;           // скрыть кнопку при старте сцены
    public bool bringToFront = true;          // переместить в конец sibling-ов (поверх всего)

    private void Reset()
    {
        if (!levelDirector) levelDirector = FindObjectOfType<LevelDirector>(true);
        if (!homeButtonGroup) homeButtonGroup = GetComponent<CanvasGroup>();
        if (!homeButtonObject && homeButtonGroup) homeButtonObject = homeButtonGroup.gameObject;
    }

    private void Awake()
    {
        if (!levelDirector) levelDirector = FindObjectOfType<LevelDirector>(true);
        if (!homeButtonGroup) homeButtonGroup = GetComponent<CanvasGroup>();
        if (!homeButtonObject && homeButtonGroup) homeButtonObject = homeButtonGroup.gameObject;
    }

    private void OnEnable()
    {
        if (!levelDirector) levelDirector = FindObjectOfType<LevelDirector>(true);
        if (levelDirector != null) levelDirector.OnGameFinished += HandleFinished;
        else Debug.LogWarning("[ShowHomeButtonOnFinish] LevelDirector not found.");

        if (hideAtStart) Hide();
    }

    private void OnDisable()
    {
        if (levelDirector != null) levelDirector.OnGameFinished -= HandleFinished;
    }

    private void HandleFinished()
    {
        Debug.Log("[ShowHomeButtonOnFinish] Game finished → showing HomeButton");
        Show();
    }

    public void Show()
    {
        var go = homeButtonObject ? homeButtonObject : (homeButtonGroup ? homeButtonGroup.gameObject : null);
        if (!go)
        {
            Debug.LogWarning("[ShowHomeButtonOnFinish] HomeButton reference is not set.");
            return;
        }

        // Убедимся, что на сцене есть EventSystem
        if (FindObjectOfType<EventSystem>() == null)
        {
            Debug.LogWarning("[ShowHomeButtonOnFinish] No EventSystem found in scene — кнопка не будет получать клики.");
        }

        go.SetActive(true);
        if (homeButtonGroup)
        {
            homeButtonGroup.alpha = 1f;
            homeButtonGroup.interactable = true;
            homeButtonGroup.blocksRaycasts = true;
        }

        if (bringToFront)
        {
            go.transform.SetAsLastSibling();
        }

        // гарантия, что есть GraphicRaycaster на Canvas
        var canvas = go.GetComponentInParent<Canvas>();
        if (canvas && !canvas.TryGetComponent<GraphicRaycaster>(out _))
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    public void Hide()
    {
        var go = homeButtonObject ? homeButtonObject : (homeButtonGroup ? homeButtonGroup.gameObject : null);
        if (!go) return;

        if (homeButtonGroup)
        {
            homeButtonGroup.alpha = 0f;
            homeButtonGroup.interactable = false;
            homeButtonGroup.blocksRaycasts = false;
        }
        // Не отключаем GameObject полностью, чтобы можно было плавно появиться альфой
        // Если хотите полностью прятать объект — раскомментируйте строку ниже
        // go.SetActive(false);
    }

    // Удобные пункты контекстного меню для тестов в playmode
    [ContextMenu("Test/Show Now")] private void CtxShowNow() => Show();
    [ContextMenu("Test/Hide Now")] private void CtxHideNow() => Hide();
}
