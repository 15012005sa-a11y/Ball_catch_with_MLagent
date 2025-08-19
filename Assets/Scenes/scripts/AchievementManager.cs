using UnityEngine;
using TMPro;
using System.Collections;

public class AchievementManager : MonoBehaviour
{
    [Header("Consecutive Catch Settings")]
    public int firstMilestone = 5;   // ������ �����
    public int secondMilestone = 10;  // ������ �����, ����� �������� ���������� ����

    [Header("UI Elements")]
    public TMP_Text achievementText;    // ��� TMP-�������
    public float scaleDuration = 0.5f;
    public float displayDuration = 2f;

    [Header("Style Settings")]
    public TMP_FontAsset specialFont;
    public Material specialMaterial;
    public Color startColor = Color.yellow;
    public Color endColor = new Color(1f, 0.5f, 0f);

    private int consecutiveCount = 0;
    private Coroutine displayCoroutine;

    private void Start()
    {
        // ������� ����� � �������� �����
        if (achievementText != null)
        {
            achievementText.gameObject.SetActive(false);
            achievementText.alpha = 0f;
            if (specialFont != null) achievementText.font = specialFont;
            if (specialMaterial != null) achievementText.fontMaterial = specialMaterial;
        }

        // ���������� �� ������ ����
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnBallCaught += HandleCatch;
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnBallCaught -= HandleCatch;
    }

    private void HandleCatch()
    {
        consecutiveCount++;

        // ������ �����
        if (consecutiveCount == firstMilestone)
        {
            ShowAchievement($"{firstMilestone} ��� ������!");
        }

        // ������ �����
        if (consecutiveCount == secondMilestone)
        {
            ShowAchievement($"{secondMilestone} ��� ������!");
            // ���������� ����, ����� ������ ����� ����
            consecutiveCount = 0;
        }
    }

    private void ShowAchievement(string message)
    {
        if (achievementText == null) return;
        if (displayCoroutine != null) StopCoroutine(displayCoroutine);
        displayCoroutine = StartCoroutine(AnimateAndDisplay(message));
    }

    private IEnumerator AnimateAndDisplay(string message)
    {
        achievementText.text = message;
        achievementText.gameObject.SetActive(true);

        // ������ ��������
        var rt = achievementText.rectTransform;
        rt.localScale = Vector3.zero;
        achievementText.color = startColor;
        achievementText.alpha = 1f;

        float elapsed = 0f;
        while (elapsed < scaleDuration)
        {
            float t = elapsed / scaleDuration;
            rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            achievementText.color = Color.Lerp(startColor, endColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // ������� ����
        rt.localScale = Vector3.one;
        achievementText.color = endColor;

        // ��� ���� ����� �������
        yield return new WaitForSeconds(displayDuration - scaleDuration);

        // ������ ��������
        float fadeTime = 0.5f;
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            achievementText.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        achievementText.gameObject.SetActive(false);
    }
}
