using UnityEngine;

public enum BallType { Blue, Red }

/// <summary>
/// “ип шара дл€ правил уровн€ 2. Ѕезопасно дл€ уровн€ 1 (если не используетс€, просто игнор).
/// </summary>
public class BallColor : MonoBehaviour
{
    public BallType type = BallType.Blue;
}