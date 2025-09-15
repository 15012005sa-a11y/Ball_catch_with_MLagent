using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Показывает HomeButton ТОЛЬКО после окончательного завершения сессии
/// (т.е. после 2-го уровня, потому что после 1-го ScoreManager ставит
/// suppressMenuOnEndOnce = true, и мы пропускаем показ).
/// Работает напрямую от ScoreManager.OnSessionFinished, даже если LevelDirector
/// не прислал свой OnGameFinished.
/// </summary>
[DefaultExecutionOrder(120)]
public class HomeButtonAfterFinalSession : MonoBehaviour
{
    [Header("Refs")]
    public ScoreManager score;               // перетащите ScoreManager из сцены
    public CanvasGroup homeButtonGroup;      // CanvasGroup на объекте HomeButton
    public GameObject homeButtonObject;      // сам HomeButton (если пусто — возьмём из CanvasGroup)

    [Header("Behaviour")] public bool hideAtStart = true; public bool bringToFront = true;

    private void Reset()
    {
        if (!score) score = FindObjectOfType<ScoreManager>(true);
        if (!homeButtonGroup) homeButtonGroup = GetComponent<CanvasGroup>();
        if (!homeButtonObject && homeButtonGroup) homeButtonObject = homeButtonGroup.gameObject;
    }

    private void Awake()
    {
        if (!score) score = FindObjectOfType<ScoreManager>(true);
        if (!homeButtonGroup) homeButtonGroup = GetComponent<CanvasGroup>();
        if (!homeButtonObject && homeButtonGroup) homeButtonObject = homeButtonGroup.gameObject;
    }

    private void OnEnable()
    {
        if (!score) score = FindObjectOfType<ScoreManager>(true);
        if (score) score.OnSessionFinished.AddListener(OnSessionFinished);
        else Debug.LogWarning("[HomeButtonAfterFinalSession] ScoreManager not found.");

        if (hideAtStart) Hide();
    }

    private void OnDisable()
    {
        if (score) score.OnSessionFinished.RemoveListener(OnSessionFinished);
    }

    private void OnSessionFinished()
    {
        // ждём кадр, чтобы ScoreManager успел сбросить suppressMenuOnEndOnce в false
        StartCoroutine(ShowIfFinal());
    }

    private IEnumerator ShowIfFinal()
    {
        yield return null; // один кадр
        if (score != null && !score.suppressMenuOnEndOnce)
        {
            Show();
        }
        else
        {
            Debug.Log("[HomeButtonAfterFinalSession] Final not reached yet (between levels)");
        }
    }

    public void Show()
    {
        var go = homeButtonObject ? homeButtonObject : (homeButtonGroup ? homeButtonGroup.gameObject : null);
        if (!go)
        {
            Debug.LogWarning("[HomeButtonAfterFinalSession] HomeButton reference is not set.");
            return;
        }

        // Убедимся, что есть EventSystem и GraphicRaycaster
        if (FindObjectOfType<EventSystem>() == null)
            Debug.LogWarning("[HomeButtonAfterFinalSession] No EventSystem found in scene.");

        go.SetActive(true);
        if (homeButtonGroup)
        {
            homeButtonGroup.alpha = 1f;
            homeButtonGroup.interactable = true;
            homeButtonGroup.blocksRaycasts = true;
        }
        if (bringToFront) go.transform.SetAsLastSibling();

        var canvas = go.GetComponentInParent<Canvas>();
        if (canvas && !canvas.TryGetComponent<GraphicRaycaster>(out _))
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        Debug.Log("[HomeButtonAfterFinalSession] HomeButton shown");
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
        // Можно не отключать GameObject — так появление будет мгновенным через alpha.
    }
}
