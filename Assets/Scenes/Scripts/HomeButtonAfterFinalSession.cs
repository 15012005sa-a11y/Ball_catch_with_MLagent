using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(120)]
public class HomeButtonAfterFinalSession : MonoBehaviour
{
    [Header("Refs")]
    public ScoreManager score;               // ���������� ScoreManager �� �����
    public CanvasGroup homeButtonGroup;      // CanvasGroup �� HomeButton
    public GameObject homeButtonObject;      // ��� HomeButton (���� ����� � ������ �� CanvasGroup)

    [Header("Behaviour")]
    public bool hideAtStart = true;
    public bool bringToFront = true;

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
        if (hideAtStart) Hide();
    }

    private void OnDisable()
    {
        if (score) score.OnSessionFinished.RemoveListener(OnSessionFinished);
    }

    private void OnSessionFinished()
    {
        // ��� 1 ����, ����� ScoreManager ����� ���������� LastSessionWasBetweenLevels
        StartCoroutine(ShowIfFinal());
    }

    private IEnumerator ShowIfFinal()
    {
        yield return null;
        if (score != null && !score.LastSessionWasBetweenLevels)
        {
            Show();
        }
        else
        {
            // ������������ ����� � ������ �� ������
        }
    }

    public void Show()
    {
        var go = homeButtonObject ? homeButtonObject : (homeButtonGroup ? homeButtonGroup.gameObject : null);
        if (!go) return;

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
    }
}
