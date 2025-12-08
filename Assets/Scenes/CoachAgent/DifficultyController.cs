using UnityEngine;

/// <summary>
/// DifficultyController: ������������ ��������� ��������� ��� Ball Catch
/// � ������ ���������� ���������� ��������� (spawnInterval, ballSpeed, targetRadius, spawnRadius)
/// � ��������� ��������� ������ �� ���������, ������� ������������ ��� ������� (BallSpawnerBallCatch)
/// � ��� ������������� ��������� (0..1) ��� CoachAgent
/// � �������� �� ��������� �������� ���� � ������� ���������� ���������
/// � ������ ������������� (Awake) + DefaultExecutionOrder, ����� �������� ���� ����������� �� ������ ��������
/// </summary>
[DefaultExecutionOrder(-100)]
public class DifficultyController : MonoBehaviour
{
    // ===== ��� �������� � ����� ������� =====
    [Header("Game refs")]
    [SerializeField] private BallSpawnerBallCatch spawner; // �������� ���� ��������� � Inspector

    // ===== ��������� (������ �����) =====
    [Header("Hard clamps (game units)")]
    public Vector2 spawnIntervalSecRange = new(0.50f, 3.00f);
    public Vector2 ballSpeedRange = new(0.20f, 5.00f);
    public Vector2 targetRadiusRange = new(0.05f, 0.20f);   // ������������ ������ �� ���������� ���������
    public Vector2 spawnRadiusRange = new(0.20f, 1.50f);   // ������������ ������ �� ���������� ���������

    // ===== ��������� ����� ����-�������� =====
    [Header("Smoothing")]
    [Range(0f, 1f)] public float lerpRate = 0.30f;
    public float BallSpeed = 5f;       
    public float SpawnInterval = 2f;

    // ===== ������� ���������� ��������� ��������� (������� �������) =====
    private float _spawnInterval;
    private float _ballSpeed;
    private float _targetRadius;
    private float _spawnRadius;

    // ��� ������ � ������ �� ������ ���������
    public float LastRoundChangeMagnitude01 { get; private set; }

    // ===== ������������� ��������� ��� CoachAgent =====
    public struct State01
    {
        public float spawnInterval01, ballSpeed01, targetRadius01, spawnRadius01, reserve01;
        public static State01 Zero => new State01 { spawnInterval01 = 0f, ballSpeed01 = 0f, targetRadius01 = 0f, spawnRadius01 = 0f, reserve01 = 0f };
    }

    private void Awake()
    {
        // ������ �������������: ����� ������ � ����� ��������� � �������
        ResetToDefault();
    }

    private void OnValidate()
    {
        // ������ ���������� �� ������������ �������� � ����������
        if (spawnIntervalSecRange.y < spawnIntervalSecRange.x) spawnIntervalSecRange.y = spawnIntervalSecRange.x + 0.01f;
        if (ballSpeedRange.y < ballSpeedRange.x) ballSpeedRange.y = ballSpeedRange.x + 0.01f;
        if (targetRadiusRange.y < targetRadiusRange.x) targetRadiusRange.y = targetRadiusRange.x + 0.001f;
        if (spawnRadiusRange.y < spawnRadiusRange.x) spawnRadiusRange.y = spawnRadiusRange.x + 0.001f;

        // �������� ���������� ��������, ���� ������������� � PlayMode
        _spawnInterval = Mathf.Clamp(_spawnInterval, spawnIntervalSecRange.x, spawnIntervalSecRange.y);
        _ballSpeed = Mathf.Clamp(_ballSpeed, ballSpeedRange.x, ballSpeedRange.y);
        _targetRadius = Mathf.Clamp(_targetRadius, targetRadiusRange.x, targetRadiusRange.y);
        _spawnRadius = Mathf.Clamp(_spawnRadius, spawnRadiusRange.x, spawnRadiusRange.y);

        ApplyToGame();
    }

    /// <summary>
    /// ����� � ����������� ��������� �� ��������� � ���������� � ����.
    /// </summary>
    public void ResetToDefault()
    {
        _spawnInterval = Mathf.Lerp(spawnIntervalSecRange.x, spawnIntervalSecRange.y, 0.60f);
        _ballSpeed = Mathf.Lerp(ballSpeedRange.x, ballSpeedRange.y, 0.40f);
        _targetRadius = Mathf.Lerp(targetRadiusRange.x, targetRadiusRange.y, 0.50f);
        _spawnRadius = Mathf.Lerp(spawnRadiusRange.x, spawnRadiusRange.y, 0.50f);
        LastRoundChangeMagnitude01 = 0f;
        ApplyToGame();
    }

    /// <summary>
    /// ���������� ������������� ��������� � [0..1] ��� ���������� ������.
    /// </summary>
    public State01 GetState01()
    {
        return new State01
        {
            spawnInterval01 = Mathf.InverseLerp(spawnIntervalSecRange.x, spawnIntervalSecRange.y, _spawnInterval),
            ballSpeed01 = Mathf.InverseLerp(ballSpeedRange.x, ballSpeedRange.y, _ballSpeed),
            targetRadius01 = Mathf.InverseLerp(targetRadiusRange.x, targetRadiusRange.y, _targetRadius),
            spawnRadius01 = Mathf.InverseLerp(spawnRadiusRange.x, spawnRadiusRange.y, _spawnRadius),
            reserve01 = 0f
        };
    }

    /// <summary>
    /// ��������� ���������� ��������� (������ �����, ��������� � ��������� �������).
    /// </summary>
    public void ApplyDeltas(float dSpawn, float dSpeed, float dRadius, float dSpawnRad)
    {
        float prevS = _spawnInterval;
        float prevV = _ballSpeed;
        float prevR = _targetRadius;
        float prevSR = _spawnRadius;
        
        // === ИСПРАВЛЕНИЕ ЗДЕСЬ ===
        // Используем dSpeed (он есть в аргументах) и dSpawn (вместо несуществующего dInterval)
        BallSpeed = Mathf.Clamp(BallSpeed + dSpeed, 1f, 15f); 
        SpawnInterval = Mathf.Clamp(SpawnInterval + dSpawn, 0.5f, 5f); // Было dInterval -> стало dSpawn
        // =========================

        // 1) Применяем изменения к внутренним переменным
        _spawnInterval = Mathf.Clamp(_spawnInterval + dSpawn, spawnIntervalSecRange.x, spawnIntervalSecRange.y);
        _ballSpeed = Mathf.Clamp(_ballSpeed + dSpeed, ballSpeedRange.x, ballSpeedRange.y);
        _targetRadius = Mathf.Clamp(_targetRadius + dRadius, targetRadiusRange.x, targetRadiusRange.y);
        _spawnRadius = Mathf.Clamp(_spawnRadius + dSpawnRad, spawnRadiusRange.x, spawnRadiusRange.y);

        // 2) Сглаживание (Lerp), чтобы изменения не были слишком резкими
        _spawnInterval = Mathf.Lerp(prevS, _spawnInterval, lerpRate);
        _ballSpeed = Mathf.Lerp(prevV, _ballSpeed, lerpRate);
        _targetRadius = Mathf.Lerp(prevR, _targetRadius, lerpRate);
        _spawnRadius = Mathf.Lerp(prevSR, _spawnRadius, lerpRate);

        // Синхронизируем публичные переменные с внутренними (чтобы они не расходились)
        BallSpeed = _ballSpeed;
        SpawnInterval = _spawnInterval;

        // 3) Расчет магнитуды изменений (для агента)
        var st = GetState01();
        float ps = Mathf.InverseLerp(spawnIntervalSecRange.x, spawnIntervalSecRange.y, prevS);
        float pv = Mathf.InverseLerp(ballSpeedRange.x, ballSpeedRange.y, prevV);
        float pr = Mathf.InverseLerp(targetRadiusRange.x, targetRadiusRange.y, prevR);
        float psr = Mathf.InverseLerp(spawnRadiusRange.x, spawnRadiusRange.y, prevSR);
        LastRoundChangeMagnitude01 = Mathf.Sqrt(
            (st.spawnInterval01 - ps) * (st.spawnInterval01 - ps) +
            (st.ballSpeed01 - pv) * (st.ballSpeed01 - pv) +
            (st.targetRadius01 - pr) * (st.targetRadius01 - pr) +
            (st.spawnRadius01 - psr) * (st.spawnRadius01 - psr)
        );

        // 4) Применение к игре
        ApplyToGame();

        Debug.Log($"[DIFF] speed={_ballSpeed:F2}, spawnInt={_spawnInterval:F2}, " +
          $"targetR={_targetRadius:F2}, spawnR={_spawnRadius:F2}");
    }

    /// <summary>
    /// ������� �������������� ��������� � �������. ������� �� ��� 0.
    /// </summary>
    private void ApplyToGame()
    {
        if (!spawner) return;

        spawner.spawnInterval = Mathf.Max(0.05f, _spawnInterval);
        spawner.ballSpeed = Mathf.Max(0.05f, _ballSpeed);

        // --- �����: �������� Spawn Radius (Spread) ---
        // ����������� ��������, ����� �������� 0..1 � �������
        float normRadius = Mathf.InverseLerp(spawnRadiusRange.x, spawnRadiusRange.y, _spawnRadius);
        spawner.SetSpawnRadius(normRadius);
    }
}
