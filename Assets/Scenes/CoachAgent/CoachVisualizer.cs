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
    [SerializeField] private Image panelBackground;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0, 0, 0, 0.5f);
    [SerializeField] private Color harderColor = new Color(1, 0, 0, 0.3f); 
    [SerializeField] private Color easierColor = new Color(0, 1, 0, 0.3f); 

    public void UpdateDashboard(float speed, float interval, float bias, float reward, float speedDelta, float intervalDelta)
    {
        // --- ЗАЩИТА ОТ ОШИБОК ---
        // Если объект выключен или уничтожен, просто выходим и не делаем ошибок
        if (this == null || !gameObject.activeInHierarchy) return;

        // Проверяем каждое поле перед записью
        if (speedText != null) speedText.text = $"Скорость: {speed:F1}";
        if (spawnText != null) spawnText.text = $"Спавн: {interval:F2}s";
        if (biasText != null) biasText.text = $"Смещение: {bias:F2}";
        if (rewardText != null) rewardText.text = $"Награда: {reward:F3}";

        // Логика цвета
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
