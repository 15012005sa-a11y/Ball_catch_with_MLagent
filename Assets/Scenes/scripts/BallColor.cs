using UnityEngine;

public enum BallType { Blue, Red }

/// <summary>
/// ��� ���� ��� ������ ������ 2. ��������� ��� ������ 1 (���� �� ������������, ������ �����).
/// </summary>
public class BallColor : MonoBehaviour
{
    public BallType type = BallType.Blue;
}