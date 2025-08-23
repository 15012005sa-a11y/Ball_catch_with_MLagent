// Assets/Scenes/scripts/SpawnPointPlacer.cs
// Генератор точек спавна с авто‑привязкой к BallSpawnerBallCatch.
// Работает в редакторе. В рантайме ничего не делает.

using System;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;  // только в редакторе
#endif

[ExecuteAlways]
[AddComponentMenu("BallCatch/Spawn Point Placer")]
public class SpawnPointPlacer : MonoBehaviour
{
    [Header("Links")]
    public Transform playerTransform;                 // центр пациента (torso/chest)
    public BallSpawnerBallCatch spawner;              // можно оставить пустым — найдём автоматически

    public enum Posture { Standing, Sitting }
    public Posture posture = Posture.Standing;
    public bool twoRows = true;

    [Header("Standing preset")]
    public float standingBaseY = 1.40f;
    public float standingYJitter = 0.10f;
    public float standingRow1Z = 1.6f;
    public float standingRow2Z = 2.0f;
    public float[] standingX = { -0.60f, -0.40f, -0.20f, +0.20f, +0.40f, +0.60f };

    [Header("Sitting preset")]
    public float sittingBaseY = 1.15f;
    public float sittingYJitter = 0.10f;
    public float sittingRow1Z = 1.4f;
    public float sittingRow2Z = 1.6f;
    public float[] sittingX = { -0.45f, -0.30f, -0.15f, +0.15f, +0.30f, +0.45f };

    [Header("Naming")]
    public string pointPrefix = "SpawnPoint";

    // ---------------------- MAIN ACTION ----------------------
    [ContextMenu("Place Spawn Points + Auto-Assign")]
    public void PlaceSpawnPoints()
    {
        if (!playerTransform) playerTransform = transform;

        // выбрать пресет
        float baseY, jitter, z1, z2; float[] xs;
        if (posture == Posture.Standing)
        { baseY = standingBaseY; jitter = standingYJitter; z1 = standingRow1Z; z2 = standingRow2Z; xs = standingX; }
        else
        { baseY = sittingBaseY; jitter = sittingYJitter; z1 = sittingRow1Z; z2 = sittingRow2Z; xs = sittingX; }

        int rows = twoRows ? 2 : 1;
        int total = xs.Length * rows;

        // создать/расставить
        int idx = 0;
        for (int r = 0; r < rows; r++)
        {
            float z = (r == 0 ? z1 : z2);
            for (int i = 0; i < xs.Length; i++, idx++)
            {
                string name = $"{pointPrefix}{idx + 1}";
                Transform child = transform.Find(name);
                if (!child)
                {
                    var go = new GameObject(name);
                    child = go.transform;
                    child.SetParent(transform, false);
#if UNITY_EDITOR
                    Undo.RegisterCreatedObjectUndo(go, "Create SpawnPoint");
#endif
                }

                float sign = ((i + r) % 2 == 0) ? -1f : +1f;   // «шахматка» по высоте
                float y = baseY + sign * jitter;
                child.position = new Vector3(
                    playerTransform.position.x + xs[i],
                    y,
                    playerTransform.position.z + z
                );
                child.rotation = Quaternion.identity;
            }
        }

        // удалить «лишние» старые точки с этим префиксом
        var excess = transform.Cast<Transform>()
            .Where(t => t.name.StartsWith(pointPrefix))
            .OrderBy(t => t.name, System.StringComparer.Ordinal)
            .Skip(total)
            .ToList();
#if UNITY_EDITOR
        foreach (var t in excess) Undo.DestroyObjectImmediate(t.gameObject);
#else
        foreach (var t in excess) DestroyImmediate(t.gameObject);
#endif

        AutoAssignToSpawner();
        Debug.Log($"[SpawnPointPlacer] Placed {total} points ({posture}, rows={rows}) and auto‑assigned.");
    }

    // авто‑привязка к BallSpawnerBallCatch
    void AutoAssignToSpawner()
    {
        if (!spawner)
        {
            spawner = GetComponentInParent<BallSpawnerBallCatch>();
            if (!spawner) spawner = UnityEngine.Object.FindObjectOfType<BallSpawnerBallCatch>();
        }
        if (!spawner) { Debug.LogWarning("[SpawnPointPlacer] Spawner not found."); return; }

        var points = transform.Cast<Transform>()
            .Where(t => t.name.StartsWith(pointPrefix))
            .OrderBy(t => t.name, System.StringComparer.Ordinal)
            .ToArray();

#if UNITY_EDITOR
        Undo.RecordObject(spawner, "Assign spawn points");
#endif

        spawner.spawnPoints = points;
        if (!spawner.playerTransform && playerTransform)
            spawner.playerTransform = playerTransform;

#if UNITY_EDITOR
        EditorUtility.SetDirty(spawner);
#endif
    }

    [ContextMenu("Place (Standing, 12 pts)")]
    void PlaceStanding() { posture = Posture.Standing; twoRows = true; PlaceSpawnPoints(); }

    [ContextMenu("Place (Sitting, 12 pts)")]
    void PlaceSitting() { posture = Posture.Sitting; twoRows = true; PlaceSpawnPoints(); }

    // визуализация
    private void OnDrawGizmosSelected()
    {
        if (!playerTransform) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(playerTransform.position, 0.05f);
        foreach (Transform t in transform)
        {
            if (!t.name.StartsWith(pointPrefix)) continue;
            Gizmos.color = Color.yellow; Gizmos.DrawSphere(t.position, 0.04f);
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawLine(t.position, new Vector3(t.position.x, t.position.y, playerTransform.position.z));
        }
    }
}
