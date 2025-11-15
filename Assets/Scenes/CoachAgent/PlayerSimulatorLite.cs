using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives IK targets for both hands so the avatar "тянется" к мячам во время обучения
/// (без Kinect). Работает с Animation Rigging (TwoBoneIK): двигаем только Target/Hints.
/// </summary>
public class PlayerSimulatorLite : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("IK target левой руки (TwoBoneIK → Target)")] public Transform leftTarget;
    [Tooltip("IK hint левой руки (TwoBoneIK → Hint)")] public Transform leftHint;
    [Tooltip("IK target правой руки (TwoBoneIK → Target)")] public Transform rightTarget;
    [Tooltip("IK hint правой руки (TwoBoneIK → Hint)")] public Transform rightHint;
    [Tooltip("Опорный трансформ тела (например, U_CharacterBack)")] public Transform torso;
    [Tooltip("Спавнер мячей, который шлёт OnBallSpawned(GameObject)")] public BallSpawnerBallCatch spawner;

    [Header("Motion")]
    [Tooltip("Скорость подведения цели кисти (м/с)")] public float moveSpeed = 12f;         // 8–15
    [Tooltip("Комфортная высота рук относительно шара (м)")] public float followHeight = 1.4f; // 1.2–1.8
    [Tooltip("Заброс цели вперёд по траектории шара (м)")] public float catchOffsetZ = 0.7f;  // 0.5–1.0
    [Tooltip("Горизонтальный разнос рук от центральной линии (м)")] public float lateralOffset = 0.25f; // 0.2–0.35
    [Tooltip("Задержка реакции перед пересчётом цели (с)")] public float reacDelay = 0.08f;   // 0.05–0.10

    [Header("Safety clamps (м в локальных осях торса)")]
    [Tooltip("Минимальная подача цели ВПЕРЁД от торса")] public float minForward = 0.15f;     // 0.12–0.22
    [Tooltip("Нижняя граница высоты цели (грудь)")] public float chestOffset = 0.10f;
    [Tooltip("Верхняя граница высоты цели (голова)")] public float headOffset = 0.55f;
    [Tooltip("Максимальная досягаемость от торса (радиус руки)")] public float maxReach = 0.75f; // подгоняется под риг

    [Header("Ball Search Fallback")]
    [Tooltip("Если спавнер не указан — ищем по тегу")] public string ballTag = "Ball";

    // Runtime state
    private readonly List<Transform> liveBalls = new();
    private Transform currentBall;
    private float lastAimTime = -999f;

    // Буфер целевых позиций
    private Vector3 leftAimPos, rightAimPos;

    #region Lifecycle
    void OnEnable()
    {
        if (spawner != null) spawner.OnBallSpawned += OnBallSpawned;
    }
    void OnDisable()
    {
        if (spawner != null) spawner.OnBallSpawned -= OnBallSpawned;
    }
    #endregion

    void Update()
    {
        CleanupDeadBalls();

        // Обновляем цель: ближайший/последний шар
        if (currentBall == null)
        {
            if (liveBalls.Count == 0) TryFillBallsByTag();
            currentBall = FindClosestBall();
        }

        var (fwd, up, right) = GetBasis();
        var anchor = GetAnchor();

        if (currentBall == null)
        {
            // Режим ожидания: держим руки перед торсом
            Vector3 idle = anchor + fwd * 0.5f + up * followHeight;
            leftAimPos = idle - right * Mathf.Abs(lateralOffset);
            rightAimPos = idle + right * Mathf.Abs(lateralOffset);
            MoveTargetsSmooth();
            PlaceHints();
            return;
        }

        // Пересчитываем цель не чаще, чем через reacDelay (уменьшаем дрожание)
        if (Time.time - lastAimTime >= reacDelay)
        {
            lastAimTime = Time.time;

            // Предсказание по скорости, если есть Rigidbody
            var rb = currentBall.GetComponent<Rigidbody>();
            Vector3 v = rb ? rb.velocity : Vector3.zero;

            // Если скорости мало — идём по направлению от торса к шару
            Vector3 fallbackDir = (currentBall.position - anchor).normalized;
            if (fallbackDir.sqrMagnitude < 1e-4f) fallbackDir = fwd;

            Vector3 leadDir = (v.sqrMagnitude > 0.01f) ? v.normalized : fallbackDir;
            Vector3 lead = leadDir * catchOffsetZ;

            Vector3 common = currentBall.position + lead + up * followHeight;
            leftAimPos = common - right * Mathf.Abs(lateralOffset);
            rightAimPos = common + right * Mathf.Abs(lateralOffset);

            // --- SAFETY CLAMPS: не уходим за спину и вне досягаемости ---
            leftAimPos = ClampAim(leftAimPos, anchor, fwd, up);
            rightAimPos = ClampAim(rightAimPos, anchor, fwd, up);
        }

        MoveTargetsSmooth();
        PlaceHints();
    }

    #region Aiming/Clamps
    private Vector3 ClampAim(Vector3 targetPos, Vector3 anchor, Vector3 fwd, Vector3 up)
    {
        // Компонента вдоль вперёд-направления торса
        float forwardDist = Vector3.Dot(targetPos - anchor, fwd);
        if (forwardDist < minForward)
            targetPos += fwd * (minForward - forwardDist);

        // Высота в локальной оси up торса
        float upDist = Vector3.Dot(targetPos - anchor, up);
        // даём рукам подниматься выше головы (1.8 × headOffset)
        upDist = Mathf.Clamp(upDist, chestOffset, headOffset * 1.8f);
        // Пересобираем точку: якорь + проекция по up + поперечная часть (орт-база устойчивая)
        Vector3 lateral = targetPos - anchor - up * Vector3.Dot(targetPos - anchor, up);
        targetPos = anchor + lateral + up * upDist;

        // Ограничение досягаемости
        Vector3 fromAnchor = targetPos - anchor;
        float dist = fromAnchor.magnitude;
        if (dist > maxReach)
            targetPos = anchor + fromAnchor.normalized * maxReach;

        return targetPos;
    }

    private void MoveTargetsSmooth()
    {
        if (leftTarget)
            leftTarget.position = Vector3.MoveTowards(leftTarget.position, leftAimPos, moveSpeed * Time.deltaTime);
        if (rightTarget)
            rightTarget.position = Vector3.MoveTowards(rightTarget.position, rightAimPos, moveSpeed * Time.deltaTime);
    }

    private void PlaceHints()
    {
        // Хинты стабилизируют локоть: немного вперёд и чуть в сторону
        var (fwd, up, right) = GetBasis();
        if (leftHint && leftTarget)
            leftHint.position = leftTarget.position - right * 0.25f + fwd * 0.15f + up * 0.05f;
        if (rightHint && rightTarget)
            rightHint.position = rightTarget.position + right * 0.25f + fwd * 0.15f + up * 0.05f;
    }
    #endregion

    #region Spawner / Balls
    private void OnBallSpawned(GameObject ball)
    {
        if (!ball) return;
        liveBalls.Add(ball.transform);
        CancelInvoke(nameof(AcquireLatestBall));
        Invoke(nameof(AcquireLatestBall), Mathf.Max(0f, reacDelay));
    }

    private void AcquireLatestBall()
    {
        CleanupDeadBalls();
        if (liveBalls.Count > 0)
        {
            currentBall = liveBalls[liveBalls.Count - 1];
            lastAimTime = -999f; // форсируем мгновенный пересчёт
        }
    }

    private void TryFillBallsByTag()
    {
        if (string.IsNullOrEmpty(ballTag)) return;
        var objs = GameObject.FindGameObjectsWithTag(ballTag);
        foreach (var go in objs)
        {
            if (go && !liveBalls.Exists(t => t == go.transform))
                liveBalls.Add(go.transform);
        }
    }

    private Transform FindClosestBall()
    {
        float best = float.MaxValue; Transform bestT = null;
        Vector3 from = GetAnchor();
        for (int i = liveBalls.Count - 1; i >= 0; i--)
        {
            var t = liveBalls[i];
            if (!t) { liveBalls.RemoveAt(i); continue; }
            float d = (t.position - from).sqrMagnitude;
            if (d < best) { best = d; bestT = t; }
        }
        return bestT;
    }

    private void CleanupDeadBalls()
    {
        for (int i = liveBalls.Count - 1; i >= 0; i--)
            if (!liveBalls[i]) liveBalls.RemoveAt(i);
        if (currentBall && !currentBall.gameObject)
            currentBall = null;
    }
    #endregion

    #region Basis helpers
    private Vector3 GetAnchor()
    {
        if (torso) return torso.position;
        if (rightTarget) return rightTarget.position;
        if (leftTarget) return leftTarget.position;
        return transform.position;
    }

    private (Vector3 fwd, Vector3 up, Vector3 right) GetBasis()
    {
        // Всегда используем мировой up = ось Y
        Vector3 up = Vector3.up;

        // Берём forward в плоскости XZ, чтобы он был по дорожке
        Vector3 fwd;
        if (torso != null)
        {
            fwd = Vector3.ProjectOnPlane(torso.forward, up);
            if (fwd.sqrMagnitude < 1e-4f)
                fwd = Vector3.forward;
        }
        else
        {
            fwd = Vector3.forward;
        }

        fwd.Normalize();
        Vector3 right = Vector3.Cross(up, fwd);

        return (fwd, up, right);
    }

    #endregion
}
