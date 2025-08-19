using UnityEngine;
using TMPro;
using System.Collections;

public class AchievementManager : MonoBehaviour
{
    [Header("Consecutive Catch Settings")]
    public int firstMilestone = 5;   // первый порог
    public int secondMilestone = 10;  // второй порог, после которого сбрасываем счёт

    [Header("UI Elements")]
    public TMP_Text achievementText;    // ваш TMP-элемент
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
        // Спрячем текст и настроим стиль
        if (achievementText != null)
        {
            achievementText.gameObject.SetActive(false);
            achievementText.alpha = 0f;
            if (specialFont != null) achievementText.font = specialFont;
            if (specialMaterial != null) achievementText.fontMaterial = specialMaterial;
        }

        // Подпишемся на поимку шара
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

        // Первый порог
        if (consecutiveCount == firstMilestone)
        {
            ShowAchievement($"{firstMilestone} лов подряд!");
        }

        // Второй порог
        if (consecutiveCount == secondMilestone)
        {
            ShowAchievement($"{secondMilestone} лов подряд!");
            // сбрасываем счёт, чтобы начать новый цикл
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

        // начало анимации
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

        // финишим рост
        rt.localScale = Vector3.one;
        achievementText.color = endColor;

        // ждём пока текст повисит
        yield return new WaitForSeconds(displayDuration - scaleDuration);

        // плавно скрываем
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
