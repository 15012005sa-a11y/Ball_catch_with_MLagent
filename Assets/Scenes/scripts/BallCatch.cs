using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BallCatch : MonoBehaviour
{
    [Tooltip("Сколько очков даётся за этот шарик")]
    public int points = 1;

    private void Reset()
    {
        // При добавлении компонента — автоматически настроим Collider как триггер
        var col = GetComponent<Collider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        // Ещё раз убеждаемся, что Collider есть и это Trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Убедитесь, что ваша «рука» имеет тег Hand
        if (!other.CompareTag("Hand"))
            return;

        // Добавляем очки
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(points);

        // Здесь можно проиграть звук лопанья, анимацию и т.п.

        // Уничтожаем этот шарик сразу при касании
        Destroy(gameObject);
    }
}
