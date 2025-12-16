using System;
using UnityEngine;

/// <summary>
/// Лёгкий симулятор "виртуального пациента" для обучения CoachAgent.
/// Двигает IK-таргеты рук к шарам, но НЕ идеально: реагирует с задержкой,
/// иногда выбирает неправильную руку/пропускает шар/целится мимо и устает.
/// 
/// Важно: этот скрипт должен быть повешен ОДИН раз на один объект (обычно на Simulator).
/// На IK-таргетах (LeftArmIK_target/RightArmIK_target) должны быть SphereCollider (IsTrigger=true),
/// а у шаров должен быть Rigidbody + BallCollision (OnTriggerEnter).
/// </summary>
[DisallowMultipleComponent]
public class PlayerSimulatorLite : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform leftTarget;
    [SerializeField] private Transform rightTarget;
    [SerializeField] private Transform torso;
    [SerializeField] private BallSpawnerBallCatch spawner; // optional, но желательно (для зависимости ошибок от сложности)

    [Header("Handedness / coordinate fixes")]
    [Tooltip("Инвертирует ось X в локальных координатах торса. Включай, если персонаж 'смотрит' в другую сторону и руки путаются лево/право.")]
    [SerializeField] private bool mirrorLocalX = false;

    [Tooltip("Жёстко меняет местами левую/правую руку (если у модели перепутаны кости или таргеты).")]
    [SerializeField] private bool swapHands = false;

    [Header("Base human limitations (skill)")]
    [Tooltip("Максимальная скорость движения IK-таргета (м/с).")]
    [SerializeField] private float handSpeed = 6f;

    [Tooltip("Базовая задержка реакции на новый шар (сек).")]
    [SerializeField] private float reactionTime = 0.20f;

    [Tooltip("Базовая вероятность 'ошибки' (0..1). Используется как общий усилитель промахов/путаницы.")]
    [Range(0f, 1f)]
    [SerializeField] private float clumsyProbability = 0.10f;

    [Tooltip("Базовый шум движения таргета (метры).")]
    [SerializeField] private float movementNoise = 0.01f;

    [Header("Difficulty sensitivity (makes performance depend on speed/spawn)")]
    [SerializeField] private bool scaleErrorsWithDifficulty = true;

    [Tooltip("Диапазон скорости шара (для нормализации 0..1). Должен соответствовать clamping в спавнере.")]
    [SerializeField] private Vector2 ballSpeedRange = new Vector2(0.20f, 5.00f);

    [Tooltip("Диапазон spawnInterval (для нормализации 0..1).")]
    [SerializeField] private Vector2 spawnIntervalRange = new Vector2(0.20f, 3.00f);

    [Tooltip("Сколько добавить к reactionTime при максимальной сложности.")]
    [SerializeField] private float reactionAddAtMaxDifficulty = 0.35f;

    [Tooltip("Во сколько раз умножить handSpeed при максимальной сложности (например 0.65 => руки медленнее).")]
    [SerializeField] private float handSpeedMultiplierAtMaxDifficulty = 0.65f;

    [Tooltip("Сколько добавить к clumsyProbability при максимальной сложности.")]
    [SerializeField] private float clumsyAddAtMaxDifficulty = 0.35f;

    [Tooltip("Макс. добавочный промах (метры) при высокой сложности.")]
    [SerializeField] private float missOffsetAtMaxDifficulty = 0.25f;

    [Header("Mistakes")]
    [Tooltip("Вероятность вообще НЕ реагировать на шар (даже если он лёгкий).")]
    [Range(0f, 1f)]
    [SerializeField] private float baseSkipBallChance = 0.05f;

    [Tooltip("Вероятность выбрать НЕ ту руку (путается).")]
    [Range(0f, 1f)]
    [SerializeField] private float baseWrongHandChance = 0.05f;

    [Tooltip("Вероятность добавить дополнительную задержку реакции (поздно начинает движение).")]
    [Range(0f, 1f)]
    [SerializeField] private float baseLateReactionChance = 0.06f;

    [Tooltip("Доп. задержка при late-reaction (сек) — масштабируется сложностью.")]
    [SerializeField] private float lateReactionExtraSeconds = 0.25f;

    [Header("Fatigue (optional)")]
    [SerializeField] private bool enableFatigue = true;

    [Tooltip("Скорость накопления усталости (0..1 в секунду).")]
    [SerializeField] private float fatigueBuildPerSecond = 0.012f;

    [Tooltip("Восстановление усталости, если нет цели (0..1 в секунду).")]
    [SerializeField] private float fatigueRecoverPerSecond = 0.020f;

    [Tooltip("Доп. clumsy от усталости при fatigue=1.")]
    [SerializeField] private float fatigueClumsyAdd = 0.20f;

    [Tooltip("Множитель скорости рук при fatigue=1 (например 0.8 => медленнее на 20%).")]
    [SerializeField] private float fatigueHandSpeedMultiplier = 0.80f;

    [Tooltip("Сколько добавить к reactionTime при fatigue=1.")]
    [SerializeField] private float fatigueReactionAdd = 0.20f;

    [Header("Catch geometry (relative to torso)")]
    [Tooltip("Небольшой сдвиг руки вперед (в локальных координатах торса).")]
    [SerializeField] private float catchForward = 0.45f;

    [Tooltip("Минимальная высота шара (world Y), ниже которой не пытаемся ловить.")]
    [SerializeField] private float minBallWorldY = 0.60f;

    [Tooltip("Минимальный local Z шара относительно торса (чтобы не ловить то, что уже позади/слишком близко по Z).")]
    [SerializeField] private float minBallLocalZ = -0.10f;

    [Header("Rest pose (local offsets)")]
    [SerializeField] private Vector3 leftRestLocal = new Vector3(-0.25f, 1.05f, 0.35f);
    [SerializeField] private Vector3 rightRestLocal = new Vector3(0.25f, 1.05f, 0.35f);

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = false;
    [SerializeField] private bool logBallSide = false;

    // ===== Runtime =====
    private float _fatigue01;

    private GameObject _currentBall;
    private int _currentBallId = int.MinValue;
    private float _ballSeenTime;

    private bool _decisionMade;
    private bool _willPursue;
    private bool _useRightHand;
    private Vector3 _plannedWorldOffset;
    private float _plannedExtraReaction;

    private Vector3 _lastAimPoint;

    private void Reset()
    {
        // Попробуем автозаполнить torso
        if (torso == null) torso = transform;
    }

    private void Awake()
    {
        if (torso == null) torso = transform;
    }

    /// <summary>Вызывай из TrainingScenario/LevelDirector при старте нового эпизода.</summary>
    public void ResetSimulator()
    {
        _fatigue01 = 0f;
        _currentBall = null;
        _currentBallId = int.MinValue;
        _decisionMade = false;
        _willPursue = false;
        _plannedWorldOffset = Vector3.zero;
        _plannedExtraReaction = 0f;
    }

    private void Update()
    {
        if (!ValidateRefs()) return;

        GameObject nextBall = FindBestBall();

        if (nextBall == null)
        {
            UpdateFatigue(hasTarget: false);
            MoveHandsToRest(GetEffectiveHandSpeed());
            return;
        }

        // Если шар сменился — заново делаем "человеческое решение"
        if (nextBall != _currentBall || GetBallId(nextBall) != _currentBallId)
        {
            _currentBall = nextBall;
            _currentBallId = GetBallId(nextBall);
            _ballSeenTime = Time.time;
            _decisionMade = false;
            _willPursue = false;
            _plannedWorldOffset = Vector3.zero;
            _plannedExtraReaction = 0f;

            if (logBallSide)
            {
                var local = torso.InverseTransformPoint(_currentBall.transform.position);
                if (mirrorLocalX) local.x = -local.x;
                Debug.Log($"[SIM] New ball id={_currentBallId}, localX={local.x:F3} (mirrorX={mirrorLocalX}, swapHands={swapHands})");
            }
        }

        UpdateFatigue(hasTarget: true);

        // Реакция (задержка) + возможно дополнительная "late" задержка
        float effectiveReaction = GetEffectiveReactionTime();
        if (_decisionMade && _plannedExtraReaction > 0f)
            effectiveReaction += _plannedExtraReaction;

        // Пока не прошло время реакции — руки на отдых
        if (Time.time - _ballSeenTime < effectiveReaction)
        {
            MoveHandsToRest(GetEffectiveHandSpeed());
            return;
        }

        // В момент, когда "реакция" закончилась — фиксируем решение (один раз на шар)
        if (!_decisionMade)
            MakeHumanDecisionForCurrentBall();

        if (!_willPursue)
        {
            MoveHandsToRest(GetEffectiveHandSpeed());
            return;
        }

        // ===== Целимся в шар (не идеально) =====
        Vector3 aimPoint = ComputeAimPoint(_currentBall);
        _lastAimPoint = aimPoint;

        float speed = GetEffectiveHandSpeed();

        // Активная рука идет к цели, пассивная — остаётся в нейтрали
        Transform active = _useRightHand ? GetRightTarget() : GetLeftTarget();
        Transform passive = _useRightHand ? GetLeftTarget() : GetRightTarget();

        MoveTarget(active, aimPoint, speed);
        MoveTarget(passive, GetRestWorldPosFor(passive == GetLeftTarget()), speed * 0.9f);
    }

    private bool ValidateRefs()
    {
        if (leftTarget == null || rightTarget == null || torso == null)
        {
            // не спамим каждый кадр
            return false;
        }
        return true;
    }

    private Transform GetLeftTarget() => swapHands ? rightTarget : leftTarget;
    private Transform GetRightTarget() => swapHands ? leftTarget : rightTarget;

    private void UpdateFatigue(bool hasTarget)
    {
        if (!enableFatigue) { _fatigue01 = 0f; return; }

        float dt = Time.deltaTime;
        if (hasTarget)
            _fatigue01 = Mathf.Clamp01(_fatigue01 + fatigueBuildPerSecond * dt);
        else
            _fatigue01 = Mathf.Clamp01(_fatigue01 - fatigueRecoverPerSecond * dt);
    }

    private float ComputeDifficulty01()
    {
        if (!scaleErrorsWithDifficulty || spawner == null) return 0f;

        float speed01 = Mathf.InverseLerp(ballSpeedRange.x, ballSpeedRange.y, spawner.ballSpeed);
        float spawn01 = Mathf.InverseLerp(spawnIntervalRange.x, spawnIntervalRange.y, spawner.spawnInterval);

        // spawn01: 0 = очень часто (сложно), 1 = редко (легко)
        float frequent01 = 1f - spawn01;

        // Итоговая сложность: быстрее + чаще => сложнее
        return Mathf.Clamp01(0.6f * speed01 + 0.4f * frequent01);
    }

    private float GetEffectiveClumsy()
    {
        float d = ComputeDifficulty01();
        float c = clumsyProbability;
        if (scaleErrorsWithDifficulty) c += clumsyAddAtMaxDifficulty * d;
        if (enableFatigue) c += fatigueClumsyAdd * _fatigue01;
        return Mathf.Clamp01(c);
    }

    private float GetEffectiveReactionTime()
    {
        float d = ComputeDifficulty01();
        float r = reactionTime;
        if (scaleErrorsWithDifficulty) r += reactionAddAtMaxDifficulty * d;
        if (enableFatigue) r += fatigueReactionAdd * _fatigue01;

        // маленький случайный джиттер реакции
        float jitter = UnityEngine.Random.Range(-0.02f, 0.05f);
        return Mathf.Max(0f, r + jitter);
    }

    private float GetEffectiveHandSpeed()
    {
        float d = ComputeDifficulty01();
        float s = handSpeed;

        if (scaleErrorsWithDifficulty)
            s *= Mathf.Lerp(1f, handSpeedMultiplierAtMaxDifficulty, d);

        if (enableFatigue)
            s *= Mathf.Lerp(1f, fatigueHandSpeedMultiplier, _fatigue01);

        return Mathf.Max(0.1f, s);
    }

    private void MakeHumanDecisionForCurrentBall()
    {
        _decisionMade = true;

        float d = ComputeDifficulty01();
        float clumsy = GetEffectiveClumsy();

        // итоговые вероятности
        float skipChance = Mathf.Clamp01(baseSkipBallChance + clumsy * 0.25f + d * 0.15f);
        float wrongHandChance = Mathf.Clamp01(baseWrongHandChance + clumsy * 0.25f + d * 0.10f);
        float lateChance = Mathf.Clamp01(baseLateReactionChance + clumsy * 0.20f + d * 0.10f);

        _willPursue = UnityEngine.Random.value > skipChance;

        // поздняя реакция
        if (UnityEngine.Random.value < lateChance)
            _plannedExtraReaction = lateReactionExtraSeconds * Mathf.Lerp(0.75f, 1.35f, d);

        // какой рукой ПО ИДЕЕ ловить
        bool shouldUseRight = ChooseHandByBallSide(_currentBall);

        // иногда путаем руку
        if (UnityEngine.Random.value < wrongHandChance)
            shouldUseRight = !shouldUseRight;

        _useRightHand = shouldUseRight;

        // промах по точке (offset) — усиливается сложностью
        float missMag = Mathf.Lerp(0.02f, missOffsetAtMaxDifficulty, d);
        missMag *= Mathf.Lerp(1f, 1.35f, clumsy); // неуклюжий => промах сильнее

        // не всегда промах — только иногда
        float offsetChance = Mathf.Clamp01(clumsy * 0.55f + d * 0.15f);
        if (UnityEngine.Random.value < offsetChance)
        {
            // локальный оффсет вокруг торса, затем в world
            Vector3 local = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-0.7f, 0.7f),
                UnityEngine.Random.Range(-0.4f, 0.4f)
            ).normalized * missMag;

            _plannedWorldOffset = torso.TransformVector(local);
        }
        else
        {
            _plannedWorldOffset = Vector3.zero;
        }
    }

    private bool ChooseHandByBallSide(GameObject ball)
    {
        if (ball == null) return true;

        Vector3 local = torso.InverseTransformPoint(ball.transform.position);
        if (mirrorLocalX) local.x = -local.x;

        // local.x < 0 => слева от торса => левая рука
        bool useRight = local.x >= 0f;

        // swapHands меняет ТАРГЕТЫ местами, но "выбор" стороны оставляем по смыслу.
        // Поэтому тут ничего не меняем.

        return useRight;
    }

    private Vector3 ComputeAimPoint(GameObject ball)
    {
        Vector3 p = ball.transform.position;

        // Небольшой упреждающий сдвиг вперёд относительно торса (чтобы ловить чуть перед телом)
        Vector3 local = torso.InverseTransformPoint(p);
        local.z = Mathf.Max(local.z, minBallLocalZ);
        local.z += catchForward;
        Vector3 aim = torso.TransformPoint(local);

        // + шум движения
        float d = ComputeDifficulty01();
        float noise = movementNoise * Mathf.Lerp(1f, 1.8f, d);
        aim += new Vector3(
            UnityEngine.Random.Range(-noise, noise),
            UnityEngine.Random.Range(-noise, noise),
            UnityEngine.Random.Range(-noise, noise)
        );

        // + заранее запланированный промах
        aim += _plannedWorldOffset;

        return aim;
    }

    private void MoveHandsToRest(float speed)
    {
        MoveTarget(GetLeftTarget(), GetRestWorldPosFor(isLeft: true), speed);
        MoveTarget(GetRightTarget(), GetRestWorldPosFor(isLeft: false), speed);
    }

    private Vector3 GetRestWorldPosFor(bool isLeft)
    {
        Vector3 local = isLeft ? leftRestLocal : rightRestLocal;
        return torso.TransformPoint(local);
    }

    private void MoveTarget(Transform t, Vector3 worldTarget, float speed)
    {
        if (t == null) return;
        float step = speed * Time.deltaTime;
        t.position = Vector3.MoveTowards(t.position, worldTarget, step);
    }

    private GameObject FindBestBall()
    {
        // Быстро и достаточно: выбрать ближайший по Z/дистанции из видимых
        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        if (balls == null || balls.Length == 0) return null;

        GameObject best = null;
        float bestScore = float.PositiveInfinity;

        for (int i = 0; i < balls.Length; i++)
        {
            var b = balls[i];
            if (b == null) continue;

            Vector3 p = b.transform.position;
            if (p.y < minBallWorldY) continue;

            Vector3 local = torso.InverseTransformPoint(p);
            if (local.z < minBallLocalZ) continue;

            // Старайся ловить то, что ближе к центру по Z и не слишком далеко по X
            float score = Mathf.Abs(local.z) * 1.25f + Mathf.Abs(local.x) * 0.60f + Mathf.Abs(local.y - 1.0f) * 0.30f;
            if (score < bestScore)
            {
                bestScore = score;
                best = b;
            }
        }

        return best;
    }

    private int GetBallId(GameObject ball)
    {
        if (ball == null) return int.MinValue;
        var bc = ball.GetComponent<BallCollision>();
        if (bc != null) return bc.BallId;
        return ball.GetInstanceID();
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos || torso == null) return;

        Gizmos.color = Color.white;
        Gizmos.DrawLine(torso.position, torso.position + torso.forward * 0.6f);

        if (_currentBall != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_currentBall.transform.position, 0.06f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_lastAimPoint, 0.06f);
        }
    }
}
