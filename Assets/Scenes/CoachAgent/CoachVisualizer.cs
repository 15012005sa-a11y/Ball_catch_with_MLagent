using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CoachVisualizer : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI spawnText;
    [SerializeField] private TextMeshProUGUI biasText;
    [SerializeField] private TextMeshProUGUI rewardText;

    // NEW: SuccessRate text
    [SerializeField] private TextMeshProUGUI successRateText;

    [SerializeField] private Image panelBackground;

    [Header("Colors (Panel)")]
    [SerializeField] private Color normalColor = new Color(0, 0, 0, 0.5f);
    [SerializeField] private Color harderColor = new Color(1, 0, 0, 0.3f);
    [SerializeField] private Color easierColor = new Color(0, 1, 0, 0.3f);

    public void UpdateDashboard(
        float speed,
        float interval,
        float bias,
        float reward,
        float successRate01,   // NEW (0..1)
        float speedDelta,
        float intervalDelta)
    {
        if (this == null || !gameObject.activeInHierarchy) return;

        if (speedText != null)  speedText.text  = $"Скорость: {speed:F1}";
        if (spawnText != null)  spawnText.text  = $"Спавн: {interval:F2}s";
        if (biasText != null)   biasText.text   = $"Смещение: {bias:F2}";
        if (rewardText != null) rewardText.text = $"Награда: {reward:F3}";

        // SuccessRate: 0..1 -> 0..100%
        if (successRateText != null)
        {
            float sr01 = Mathf.Clamp01(successRate01);
            float percent = sr01 * 100f;
            successRateText.text = $"SuccessRate: {percent:F0}%";

            // "Поток" 70..80% (зелёный), слишком сложно <50% (красный), иначе белый
            if (sr01 >= 0.70f && sr01 <= 0.80f) successRateText.color = Color.green;
            else if (sr01 < 0.50f) successRateText.color = Color.red;
            else successRateText.color = Color.white;
        }

        // Цвет панели по направлению изменения сложности
        if (panelBackground != null)
        {
            bool gettingHarder = speedDelta > 0.05f || intervalDelta < -0.05f;
            bool gettingEasier = speedDelta < -0.05f || intervalDelta > 0.05f;

            Color targetColor = normalColor;
            if (gettingHarder) targetColor = harderColor;
            else if (gettingEasier) targetColor = easierColor;

            panelBackground.color = Color.Lerp(panelBackground.color, targetColor, Time.deltaTime * 5f);
        }
    }
}
