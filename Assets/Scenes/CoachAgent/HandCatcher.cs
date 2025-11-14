using UnityEngine;

/// <summary>
/// Put this on the palm/hand collider (isTrigger=ON).
/// Requires: a Rigidbody on the same object (IsKinematic = ON recommended)
/// Ball objects must have tag = Ball and a Collider + Rigidbody.
/// When a ball enters the trigger, we award score and destroy the ball.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class HandCatcher : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Tag of the balls to catch")] public string ballTag = "Ball";
    [Tooltip("Reference to ScoreManager (assign in Inspector)")] public ScoreManager score;

    [Header("Effects (optional)")]
    [Tooltip("Play on catch")] public AudioSource sfxOnCatch;
    [Tooltip("Spawn this prefab at hit point")] public GameObject popVfxPrefab;

    [Header("Debug")] public bool logEvents = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // ensure trigger
        // suggest kinematic rb for reliable trigger events
        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other || !other.gameObject) return;
        if (!other.CompareTag(ballTag)) return;

        // scoring
        if (score) score.ReportCatch(true, 1);   // поймано
                                                 // или
        if (score) score.ReportMiss();          // промах (если используешь)

        // vfx/sfx
        if (sfxOnCatch) sfxOnCatch.Play();
        if (popVfxPrefab)
        {
            var vfx = Instantiate(popVfxPrefab, other.transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        // destroy ball
        Destroy(other.gameObject);

        if (logEvents) Debug.Log($"[HandCatcher] Caught {other.name}");
    }

    private void OnTriggerExit(Collider other)
    {
        // optional: you can use Stay/Exit for alternative rules
    }
}
