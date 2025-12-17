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
    // 1. Добавляем новое поле
    [SerializeField] private TextMeshProUGUI successRateText; 

    [Header("Visual Feedback")]
    [SerializeField] private Image panelBackground;
    [SerializeField] private Color normalColor = new Color(0, 0, 0, 0.5f);
    
    // 2. Обновляем метод: добавляем аргумент currentSuccessRate
    public void UpdateDashboard(float speed, float interval, float bias, float reward, float currentSuccessRate, float speedDelta, float intervalDelta)
    {
        if (this == null || !gameObject.activeInHierarchy) return;

        if (speedText != null) speedText.text = $"Speed: {speed:F1}";
        if (spawnText != null) spawnText.text = $"Interval: {interval:F2}s";
        if (biasText != null) biasText.text = $"Bias: {bias:F2}";
        if (rewardText != null) rewardText.text = $"Reward: {reward:F3}";

        // 3. Отображаем Success Rate
        if (successRateText != null)
        {
            float percent = currentSuccessRate * 100f;
            successRateText.text = $"Success: {percent:F0}%";

            // Визуальная подсказка (Поток: 70% - 80%)
            if (currentSuccessRate >= 0.70f && currentSuccessRate <= 0.80f)
                successRateText.color = Color.green; // Идеальная зона
            else if (currentSuccessRate < 0.50f)
                successRateText.color = Color.red;   // Слишком сложно
            else
                successRateText.color = Color.white; // Обычное состояние
        }
    }
}
