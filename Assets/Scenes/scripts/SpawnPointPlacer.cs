// SpawnPointPlacer.cs — Standing: вертикальные колонки (как на скрине)
// Sitting: прежняя логика (один ряд по Z, X-массив + jitter).
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[AddComponentMenu("BallCatch/Spawn Point Placer")]
public class SpawnPointPlacer : MonoBehaviour
{
    [Header("Links")]
    public Transform playerTransform;
    public BallSpawnerBallCatch spawner;

    public enum Posture { Standing, Sitting }
    public Posture posture = Posture.Standing;

    [Header("Standing (columns layout)")]
    public float standingBaseY = 1.40f;               // высота плеча
    public float standingZ = 1.60f;                   // все точки на одной дистанции
    // X-колонки (лево/право). По скрину хороши +/-0.55
    public float[] standingColumnsX = { -0.55f, +0.55f };
    // Уровни по высоте относительно baseY (сверху -> вниз)
    public float[] standingYLevels = { +0.30f, +0.15f, 0f, -0.15f, -0.30f, -0.45f };

    [Header("Sitting (legacy row layout)")]
    public bool sittingTwoRows = true;
    public float sittingBaseY = 1.15f;
    public float sittingYJitter = 0.10f;
    public float sittingRow1Z = 1.4f;
    public float sittingRow2Z = 1.6f;
    public float[] sittingX = { -0.45f, -0.30f, -0.15f, +0.15f, +0.30f, +0.45f };

    [Header("Naming")]
    public string pointPrefix = "SpawnPoint";

    [ContextMenu("Place Spawn Points + Auto-Assign")]
    public void PlaceSpawnPoints()
    {
        if (!playerTransform) playerTransform = transform;

        // Очистим/создадим нужное кол-во точек по выбранной позе
        int total = 0;
        if (posture == Posture.Standing)
            total = PlaceStandingColumns();
        else
            total = PlaceSittingLegacy();

        // Удалим лишние
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
        Debug.Log($"[SpawnPointPlacer] Placed {total} points ({posture}).");
    }

    int PlaceStandingColumns()
    {
        // Генерируем 2 колонки: лево и право; на каждой по 6 высот (сверху-вниз)
        // Имена: SpawnPoint1..12, порядок: лев.колонка (сверху->вниз), затем прав.колонка (сверху->вниз)
        int idx = 0;
        // гарантируем порядок: сначала левая (меньший X), потом правая
        var cols = standingColumnsX.OrderBy(x => x).ToArray();
        foreach (var x in cols)
        {
            foreach (var yOff in standingYLevels)   // сверху -> вниз
            {
                string name = $"{pointPrefix}{++idx}";
                var child = EnsureChild(name);
                child.position = new Vector3(
                    playerTransform.position.x + x,
                    standingBaseY + yOff,
                    playerTransform.position.z + standingZ
                );
                child.rotation = Quaternion.identity;
            }
        }
        return idx;
    }

    int PlaceSittingLegacy()
    {
        int rows = sittingTwoRows ? 2 : 1;
        int total = sittingX.Length * rows;
        int idx = 0;
        for (int r = 0; r < rows; r++)
        {
            float z = (r == 0 ? sittingRow1Z : sittingRow2Z);
            for (int i = 0; i < sittingX.Length; i++)
            {
                string name = $"{pointPrefix}{++idx}";
                var child = EnsureChild(name);
                float sign = ((i + r) % 2 == 0) ? -1f : +1f; // лёгкая «шахматка»
                float y = sittingBaseY + sign * sittingYJitter;
                child.position = new Vector3(
                    playerTransform.position.x + sittingX[i],
                    y,
                    playerTransform.position.z + z
                );
                child.rotation = Quaternion.identity;
            }
        }
        return idx;
    }

    Transform EnsureChild(string name)
    {
        var tr = transform.Find(name);
        if (!tr)
        {
            var go = new GameObject(name);
            tr = go.transform;
            tr.SetParent(transform, false);
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(go, "Create SpawnPoint");
#endif
        }
        return tr;
    }

    void AutoAssignToSpawner()
    {
        if (!spawner)
        {
            spawner = GetComponentInParent<BallSpawnerBallCatch>();
            if (!spawner) spawner = Object.FindObjectOfType<BallSpawnerBallCatch>();
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
