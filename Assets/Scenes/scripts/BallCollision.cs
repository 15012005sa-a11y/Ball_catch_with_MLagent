using UnityEngine;

[DisallowMultipleComponent]
public class BallCollision : MonoBehaviour
{
    [Tooltip("ID шара для вычисления времени реакции")]
    public int BallId = -1;

    [Tooltip("Шар красный? (штраф вместо очка)")]
    public bool isRed = false;

    [Header("Destroy")]
    public bool destroyOnCatch = true;
    public float destroyDelay = 0.02f;

    [Header("FX")]
    [Tooltip("Префаб эффекта лопания (например BallPopEffect)")]
    public GameObject popEffectPrefab;
    public float popEffectLifetime = 1.2f;

    [Header("Detection")]
    [Tooltip("Слой(и) коллайдеров руки. Оставь пустым — будет работать по имени.")]
    public LayerMask handLayers;

    private bool _scored = false;

    private bool IsPlayerHand(Collider other)
    {
        if (other == null) return false;

        // 1) По слою (лучший вариант, тег не требуется)
        if ((handLayers.value & (1 << other.gameObject.layer)) != 0)
            return true;

        // 2) По имени коллайдера (подходит для Kinect-костей)
        string n = other.name.ToLowerInvariant();
        if (n.Contains("hand") || n.Contains("palm") || n.Contains("wrist"))
            return true;

        // 3) Разрешим ещё общий случай с тегом Player (стандартный тег существует)
        if (other.CompareTag("Player"))
            return true;

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_scored) return;
        if (!IsPlayerHand(other)) return;

        _scored = true;

        var sm = ScoreManager.Instance;
        if (sm != null)
        {
            // Точность по дистанции
            float dist = Vector3.Distance(other.ClosestPoint(transform.position), transform.position);
            sm.RecordAccuracy(dist);

            // Реакция по времени спавна
            if (sm.spawnTimes != null && sm.spawnTimes.TryGetValue(BallId, out float tSpawn))
                sm.RecordReactionTime(Time.time - tSpawn);

            // Счёт
            if (isRed) sm.RedBallTouched();   // штраф
            else sm.AddScore(1);        // +1
        }

        // FX: вспышка при лопании
        if (popEffectPrefab != null)
        {
            var fx = Instantiate(popEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, popEffectLifetime);
        }

        if (destroyOnCatch)
            Destroy(gameObject, destroyDelay);
    }
}
