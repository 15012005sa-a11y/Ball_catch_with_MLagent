using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class AchievementManager : MonoBehaviour
{
    [Header("Consecutive Catch Settings")]
    public List<int> milestones = new List<int> { 5, 10, 20 };

    [Header("UI Elements")]
    public TMP_Text achievementText;
    public float scaleDuration = 0.5f;
    public float displayDuration = 2f;

    [Header("Style Settings")]
    public TMP_FontAsset specialFont;
    public Material specialMaterial;
    public Color startColor = Color.yellow;
    public Color endColor = new Color(1f, 0.5f, 0f);

    [Header("Audio")]
    [Tooltip("Активный AudioSource под Canvas (Play On Awake = off, Loop = off, Spatial Blend = 0)")]
    public AudioSource sfxSource;
    public AudioClip sfx5;
    public AudioClip sfx10;
    public AudioClip sfx20;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    int consecutiveCount = 0;
    int nextMilestoneIndex = 0;
    Coroutine displayCoroutine;
    bool subscribed = false;

    void Start()
    {
        if (achievementText != null)
        {
            achievementText.gameObject.SetActive(false);
            achievementText.alpha = 0f;
            if (specialFont) achievementText.font = specialFont;
            if (specialMaterial) achievementText.fontMaterial = specialMaterial;
        }
        StartCoroutine(EnsureSubscribed());
    }

    void OnEnable() { TrySubscribe(); }
    void OnDisable() { TryUnsubscribe(); }

    IEnumerator EnsureSubscribed()
    {
        while (ScoreManager.Instance == null) yield return null;
        TrySubscribe();
    }

    void TrySubscribe()
    {
        if (subscribed) return;
        var s = ScoreManager.Instance;
        if (s == null) return;

        s.OnGoodCatch += HandleGoodCatch; // +1 к серии
        s.OnRedTouched += ResetStreak;    // красный = сброс
        s.OnMissed += ResetStreak;    // промах = сброс

        subscribed = true;
    }

    void TryUnsubscribe()
    {
        if (!subscribed) return;
        var s = ScoreManager.Instance;
        if (s != null)
        {
            s.OnGoodCatch -= HandleGoodCatch;
            s.OnRedTouched -= ResetStreak;
            s.OnMissed -= ResetStreak;
        }
        subscribed = false;
    }

    void HandleGoodCatch()
    {
        consecutiveCount++;

        if (nextMilestoneIndex < milestones.Count &&
            consecutiveCount == milestones[nextMilestoneIndex])
        {
            int value = milestones[nextMilestoneIndex];

            // звук для конкретного порога
            PlayMilestoneSfx(value);

            // текст/анимация
            ShowAchievement($"{value} лов подряд!");

            // не сбрасываем серию — копим дальше к следующему порогу
            nextMilestoneIndex = Mathf.Min(nextMilestoneIndex + 1, milestones.Count);
        }
    }

    void ResetStreak()
    {
        consecutiveCount = 0;
        nextMilestoneIndex = 0;
    }

    void PlayMilestoneSfx(int value)
    {
        if (sfxSource == null) return;

        AudioClip clip = null;
        if (value == 5) clip = sfx5;
        else if (value == 10) clip = sfx10;
        else if (value == 20) clip = sfx20;

        if (clip != null) sfxSource.PlayOneShot(clip, sfxVolume);
    }

    void ShowAchievement(string message)
    {
        if (!achievementText) return;
        achievementText.transform.SetAsLastSibling(); // поверх UI

        if (displayCoroutine != null) StopCoroutine(displayCoroutine);
        displayCoroutine = StartCoroutine(AnimateAndDisplay(message));
    }

    IEnumerator AnimateAndDisplay(string message)
    {
        achievementText.text = message;
        achievementText.gameObject.SetActive(true);

        var rt = achievementText.rectTransform;
        rt.localScale = Vector3.zero;
        achievementText.color = startColor;
        achievementText.alpha = 1f;

        float t = 0f;
        while (t < scaleDuration)
        {
            float k = t / scaleDuration;
            rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, k);
            achievementText.color = Color.Lerp(startColor, endColor, k);
            t += Time.deltaTime;
            yield return null;
        }
        rt.localScale = Vector3.one;
        achievementText.color = endColor;

        float hold = Mathf.Max(0f, displayDuration - scaleDuration);
        if (hold > 0f) yield return new WaitForSeconds(hold);

        const float fade = 0.5f;
        t = 0f;
        while (t < fade)
        {
            achievementText.alpha = Mathf.Lerp(1f, 0f, t / fade);
            t += Time.deltaTime;
            yield return null;
        }
        achievementText.gameObject.SetActive(false);
    }
}
