using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BallFlight : MonoBehaviour
{
    Vector3 dir;
    float speed;
    float lifeTime = 10f;
    float t0;

    Collider col;
    Rigidbody rb; // держим, но отключаем физику — нужен только для триггеров

    void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;                 // ловим через триггеры

        rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;                // полностью вырубаем физику
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.interpolation = RigidbodyInterpolation.None;

        // на всякий случай
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.rotation = Quaternion.identity;
    }

    /// <summary>Задать цель и скорость (вызывается спавнером сразу после Instantiate).</summary>
    public void Init(Transform target, float flySpeed)
    {
        speed = flySpeed;
        Vector3 to = target ? target.position : transform.position + Vector3.forward * 5f;
        dir = (to - transform.position).normalized;
        t0 = Time.time;
    }

    void Update()
    {
        // идеальная прямая: s = v * t
        transform.position += dir * speed * Time.deltaTime;

        // авто-удаление, если что-то пошло не так
        if (Time.time - t0 > lifeTime) Destroy(gameObject);
    }
}
