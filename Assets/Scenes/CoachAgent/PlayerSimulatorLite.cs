using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves IK Targets (left & right) instead of bones so that Animator is not overridden.
/// Works both during normal play and ML-Agents training.
/// Assign: LeftHandTarget / RightHandTarget that were created by Animation Rigging (Auto Setup).
/// Optionally assign a spawner which invokes OnBallSpawned(GameObject).
/// </summary>
public class PlayerSimulatorLite : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("IK target of the LEFT hand (created by TwoBoneIK → Auto Setup)")] public Transform leftTarget;
    [Tooltip("IK target of the RIGHT hand (created by TwoBoneIK → Auto Setup)")] public Transform rightTarget;
    [Tooltip("(Optional) Ball spawner that raises OnBallSpawned(GameObject) events")] public BallSpawnerBallCatch spawner;

    [Header("Motion")]
    [Tooltip("Hand chase speed (m/s)")] public float moveSpeed = 5f;
    [Tooltip("Convenient hand height while following balls")] public float followHeight = 1.2f;
    [Tooltip("Small forward offset towards the ball (Z world)")] public float catchOffsetZ = 0.10f;
    [Tooltip("Hands horizontal separation from the center line (X world)")] public float lateralOffset = 0.25f;
    [Tooltip("Reaction delay before committing to a freshly spawned ball")] public float reacDelay = 0.15f;

    [Header("Ball Search Fallback")]
    [Tooltip("If spawner is not assigned, we look for balls by tag each frame until one appears")]
    public string ballTag = "Ball";

    private readonly List<Transform> _liveBalls = new();
    private Transform _currentBall;

    private Transform ReferenceAnchor
    {
        get { return rightTarget != null ? rightTarget : (leftTarget != null ? leftTarget : transform); }
    }

    private void OnEnable()
    {
        if (spawner != null) spawner.OnBallSpawned += OnBallSpawned;
    }

    private void OnDisable()
    {
        if (spawner != null) spawner.OnBallSpawned -= OnBallSpawned;
    }

    private void Update()
    {
        // 1) maintain balls list
        CleanupDeadBalls();
        if (_currentBall == null)
        {
            if (_liveBalls.Count == 0) TryFillBallsByTag();
            _currentBall = FindClosestBall();
        }

        // 2) compute base target position (center line)
        Vector3 baseTarget;
        if (_currentBall != null)
        {
            Vector3 p = _currentBall.position;
            baseTarget = new Vector3(p.x, followHeight, p.z + catchOffsetZ);
        }
        else
        {
            // idle pose in front of avatar
            Vector3 anchor = ReferenceAnchor.position;
            baseTarget = new Vector3(anchor.x, followHeight, anchor.z + 0.50f);
        }

        // 3) move IK targets symmetrically
        if (rightTarget != null)
        {
            Vector3 to = baseTarget + Vector3.right * Mathf.Abs(lateralOffset);
            rightTarget.position = Vector3.MoveTowards(rightTarget.position, to, moveSpeed * Time.deltaTime);
        }
        if (leftTarget != null)
        {
            Vector3 to = baseTarget + Vector3.left * Mathf.Abs(lateralOffset);
            leftTarget.position = Vector3.MoveTowards(leftTarget.position, to, moveSpeed * Time.deltaTime);
        }
    }

    // === Spawner callbacks ===
    private void OnBallSpawned(GameObject ball)
    {
        if (ball == null) return;
        _liveBalls.Add(ball.transform);
        CancelInvoke(nameof(AcquireLatestBall));
        Invoke(nameof(AcquireLatestBall), Mathf.Max(0f, reacDelay));
    }

    private void AcquireLatestBall()
    {
        CleanupDeadBalls();
        if (_liveBalls.Count > 0)
            _currentBall = _liveBalls[_liveBalls.Count - 1];
    }

    // === Helpers ===
    private void TryFillBallsByTag()
    {
        if (string.IsNullOrEmpty(ballTag)) return;
        var objs = GameObject.FindGameObjectsWithTag(ballTag);
        foreach (var go in objs)
        {
            if (go != null && !_liveBalls.Exists(t => t == go.transform))
                _liveBalls.Add(go.transform);
        }
    }

    private Transform FindClosestBall()
    {
        float best = float.MaxValue; Transform bestT = null;
        Transform from = ReferenceAnchor;
        for (int i = _liveBalls.Count - 1; i >= 0; i--)
        {
            var t = _liveBalls[i];
            if (t == null) { _liveBalls.RemoveAt(i); continue; }
            float d = (t.position - from.position).sqrMagnitude;
            if (d < best) { best = d; bestT = t; }
        }
        return bestT;
    }

    private void CleanupDeadBalls()
    {
        for (int i = _liveBalls.Count - 1; i >= 0; i--)
        {
            if (_liveBalls[i] == null) _liveBalls.RemoveAt(i);
        }
        if (_currentBall == null) return;
        if (_currentBall.gameObject == null) _currentBall = null;
    }
}
