using System;
using UnityEngine;

[Serializable]
public class SessionRecord
{
    // Когда проходили сеанс
    public DateTime dateUtc = DateTime.UtcNow;

    // Итоги (подгони под свои поля из ScoreManager/экспортера)
    public int totalScore;
    public float successRate;   // 0..1
    public float playTimeSec;

    // Доп.метрики (по желанию — можно убрать, если не нужны)
    public float avgReactionSec;
    public float avgRightAngle;
    public float avgLeftAngle;

    // Для удобного лога
    public override string ToString()
        => $"[{dateUtc:u}] score={totalScore}, success={(successRate * 100f):0.0}%, time={playTimeSec:0.0}s";
}
