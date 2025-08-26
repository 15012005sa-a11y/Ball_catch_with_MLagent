using UnityEngine;
using TMPro;
using System.Collections;

public class CountdownOverlay : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup group;          // CanvasGroup оверлея (для fade)
    public TMP_Text headerText;        // строка над цифрой (напр. "1 уровень" + "Приготовьтесь")
    public TMP_Text numberText;        // крупная цифра в центре

    [Header("Audio")]
    public AudioSource audioSource;    // любой AudioSource
    public AudioClip beepClip;         // обычный «бип»
    public AudioClip finalBeepClip;    // финальный «бип» на GO!

    [Header("Animation")]
    public float digitPopScale = 1.3f; // «поп» масштаб цифры
    public float popTime = 0.15f;      // длительность «попа»
    public float fadeOutTime = 0.35f;

    void Awake()
    {
        if (group) { group.alpha = 0f; }
        if (headerText) headerText.text = "";
        if (numberText) numberText.text = "";
        gameObject.SetActive(false);
    }

    /// <summary>Показывает отсчёт (seconds -> ... -> GO!) и скрывает себя.</summary>
    public IEnumerator Run(string header, int seconds)
    {
        if (seconds < 1) seconds = 1;

        gameObject.SetActive(true);
        if (group) group.alpha = 1f;

        if (headerText) headerText.text = header;

        for (int s = seconds; s > 0; s--)
        {
            if (numberText)
            {
                numberText.text = s.ToString();
                // pop-анимация
                var t = numberText.rectTransform;
                t.localScale = Vector3.one * digitPopScale;
                float t0 = 0f;
                while (t0 < popTime)
                {
                    t0 += Time.deltaTime;
                    float k = 1f - Mathf.Clamp01(t0 / popTime);
                    t.localScale = Vector3.one * (1f + (digitPopScale - 1f) * k);
                    yield return null;
                }
                t.localScale = Vector3.one;
            }

            // бип
            if (audioSource && beepClip) audioSource.PlayOneShot(beepClip);

            yield return new WaitForSeconds(1f);
        }

        // GO!
        if (numberText) numberText.text = "GO!";
        if (audioSource && finalBeepClip) audioSource.PlayOneShot(finalBeepClip);

        // короткая пауза и fade out
        yield return new WaitForSeconds(0.2f);
        if (group)
        {
            float a0 = group.alpha, t = 0f;
            while (t < fadeOutTime)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Lerp(a0, 0f, t / fadeOutTime);
                yield return null;
            }
            group.alpha = 0f;
        }

        gameObject.SetActive(false);
    }
}
