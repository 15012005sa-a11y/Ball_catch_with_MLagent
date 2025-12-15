using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayerSimulatorLite
/// 
/// Исправления под твою сцену:
/// 1) Не используем world-Z (он ломается, если персонаж развернут). Все расчёты делаем в ЛОКАЛЬНЫХ координатах torso.
/// 2) Выбор руки не по "лево/право", а по ближайшей руке к точке перехвата + фиксируем выбранную руку на весь мяч.
/// 3) Rest-поза хранится в локале torso, чтобы не съезжало при поворотах.
/// 
/// ВАЖНО в Unity:
/// - RightArmIK и LeftArmIK должны быть в одном Rig (Rig component) и этот Rig должен быть добавлен в RigBuilder.
/// - Weight у Rig = 1, Weight у обеих TwoBoneIKConstraint = 1.
/// - На LeftArmIK_target и RightArmIK_target (или на 11_Hand_*) должны быть Collider (SphereCollider) для ловли.
/// </summary>
public class PlayerSimulatorLite : MonoBehaviour
{
    [Header("References")]
    public Transform leftTarget;      // LeftArmIK_target (или 11_Hand_Left, если IK Target = 11_Hand_Left)
    public Transform rightTarget;     // RightArmIK_target (или 11_Hand_Right)
    public Transform torso;           // U_CharacterBack (центр)
    public BallSpawnerBallCatch spawner;

    [Header("Human Limitations (Skill)")]
    [SerializeField] private float handSpeed = 6.5f;      // м/с
    [SerializeField] private float reactionTime = 0.2f;   // сек
    [SerializeField] private float clumsyProbability = 0f;
    [SerializeField] private float movementNoise = 0.01f;

    [Header("Catch geometry (local to torso)")]
    [Tooltip("На каком расстоянии ВПЕРЕД от torso (в локальных координатах) мы ловим мяч.")]
    [SerializeField] private float catchForward = 0.45f; // local +Z от torso

    [Tooltip("Минимальная высота мяча (в мировых координатах), чтобы не гоняться за упавшими.")]
    [SerializeField] private float minBallWorldY = 0.6f;

    [Tooltip("Если мяч уже за игроком (локальный z меньше этого), игнорируем.")]
    [SerializeField] private float minBallLocalZ = -0.10f;

    [Header("Rest pose (local offsets)")]
    [SerializeField] private Vector3 leftRestLocal = new Vector3(-0.25f, 1.05f, 0.35f);
    [SerializeField] private Vector3 rightRestLocal = new Vector3(+0.25f, 1.05f, 0.35f);

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = false;

    private readonly List<Transform> _activeBalls = new List<Transform>();
    private Transform _currentBall;
    private float _noticeTime;
    private Vector2 _missOffsetXY;

    // фиксируем, какой рукой ловим текущий мяч (чтобы не "переключалось" каждый кадр)
    private bool _useLeftHand;

    private bool _warned;
    private Vector3 _lastDesiredWorld;

    private void Awake()
    {
        if (torso == null) torso = transform;
        if (spawner == null) spawner = FindObjectOfType<BallSpawnerBallCatch>(true);

        // Если targets уже стоят в сцене, можно взять текущую позу за rest (лучше выглядит)
        if (leftTarget != null) leftRestLocal = torso.InverseTransformPoint(leftTarget.position);
        if (rightTarget != null) rightRestLocal = torso.InverseTransformPoint(rightTarget.position);
    }

    private void LateUpdate()
    {
        // LateUpdate лучше для Animation Rigging (он применяет позы в LateUpdate)
        if (!EnsureReady()) return;

        ScanForBalls();
        ChooseTarget();
        MoveHands();
    }

    private bool EnsureReady()
    {
        if (torso == null) torso = transform;

        if (leftTarget == null || rightTarget == null)
        {
            if (!_warned)
            {
                _warned = true;
                Debug.LogWarning("[PlayerSimulatorLite] Assign Left Target and Right Target in Inspector.", this);
            }
            return false;
        }

        return true;
    }

    private void ScanForBalls()
    {
        _activeBalls.RemoveAll(b => b == null || !b.gameObject.activeInHierarchy);

        // Надёжно: BallCollision точно на мяче
        var balls = FindObjectsOfType<BallCollision>();
        foreach (var b in balls)
        {
            if (b != null && b.gameObject.activeInHierarchy)
            {
                var t = b.transform;
                if (!_activeBalls.Contains(t)) _activeBalls.Add(t);
            }
        }
    }

    private void ChooseTarget()
    {
        // если текущий мяч пропал
        if (_currentBall != null && !_currentBall.gameObject.activeInHierarchy)
            _currentBall = null;

        if (_currentBall != null) return;
        if (_activeBalls.Count == 0) return;

        // выбираем мяч, который ближе всего к плоскости перехвата (в локальном z)
        float best = float.MaxValue;
        Transform bestBall = null;

        for (int i = 0; i < _activeBalls.Count; i++)
        {
            var b = _activeBalls[i];
            if (b == null) continue;
            if (b.position.y < minBallWorldY) continue;

            Vector3 local = torso.InverseTransformPoint(b.position);
            if (local.z < minBallLocalZ) continue; // уже "за" игроком

            float dz = Mathf.Abs(local.z - catchForward);
            if (dz < best)
            {
                best = dz;
                bestBall = b;
            }
        }

        if (bestBall == null) return;

        _currentBall = bestBall;
        _noticeTime = Time.time + reactionTime;

        // промах (опционально)
        if (Random.value < clumsyProbability)
        {
            Vector2 r = Random.insideUnitCircle.normalized * 0.25f;
            _missOffsetXY = r;
        }
        else
        {
            _missOffsetXY = Vector2.zero;
        }

        // заранее выбираем руку: какая ближе к точке перехвата
        Vector3 desiredWorld = ComputeDesiredWorld(_currentBall);
        _useLeftHand = (leftTarget.position - desiredWorld).sqrMagnitude <= (rightTarget.position - desiredWorld).sqrMagnitude;
    }

    private Vector3 ComputeDesiredWorld(Transform ball)
    {
        // точка перехвата: берём X/Y мяча (в локале torso), но фиксируем z=catchForward
        Vector3 local = torso.InverseTransformPoint(ball.position);

        Vector3 desiredLocal = new Vector3(
            local.x + _missOffsetXY.x,
            local.y + _missOffsetXY.y,
            catchForward
        );

        Vector3 desiredWorld = torso.TransformPoint(desiredLocal);
        desiredWorld += Random.insideUnitSphere * movementNoise;

        return desiredWorld;
    }

    private void MoveHands()
    {
        if (_currentBall == null || Time.time < _noticeTime)
        {
            // rest
            MoveSingleHand(leftTarget, torso.TransformPoint(leftRestLocal));
            MoveSingleHand(rightTarget, torso.TransformPoint(rightRestLocal));
            return;
        }

        _lastDesiredWorld = ComputeDesiredWorld(_currentBall);

        if (_useLeftHand)
        {
            MoveSingleHand(leftTarget, _lastDesiredWorld);
            MoveSingleHand(rightTarget, torso.TransformPoint(rightRestLocal));
        }
        else
        {
            MoveSingleHand(rightTarget, _lastDesiredWorld);
            MoveSingleHand(leftTarget, torso.TransformPoint(leftRestLocal));
        }
    }

    private void MoveSingleHand(Transform handTarget, Vector3 destination)
    {
        if (handTarget == null) return;
        handTarget.position = Vector3.MoveTowards(handTarget.position, destination, handSpeed * Time.deltaTime);
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos || torso == null) return;

        Gizmos.color = Color.yellow;
        // плоскость перехвата (точка впереди torso)
        Gizmos.DrawSphere(torso.TransformPoint(new Vector3(0f, 1.0f, catchForward)), 0.03f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(_lastDesiredWorld, 0.04f);
    }
}
