using UnityEngine;

[DefaultExecutionOrder(-5)]
public class ConfigApplier : MonoBehaviour
{
    public LevelDirector levelDirector;
    public BallSpawnerBallCatch spawner;
    public ScoreManager score;

    private void Awake()
    {
        if (!levelDirector) levelDirector = FindObjectOfType<LevelDirector>(true);
        if (!spawner) spawner = FindObjectOfType<BallSpawnerBallCatch>(true);
        if (!score) score = FindObjectOfType<ScoreManager>(true);
    }

    private void Start()
    {
        var pm = PatientManager.Instance;
        var p = pm != null ? pm.Current : null;

        if (p == null)
        {
            Debug.LogWarning("[ConfigApplier] Patient not selected — using defaults.");
            return;
        }

        // Применяем
        if (levelDirector)
        {
            levelDirector.level1Duration = p.settings.level1DurationSec;
            levelDirector.level2Duration = p.settings.level2DurationSec;
            levelDirector.restBetweenLevelsSeconds = p.settings.restTimeSec;
            levelDirector.redChance = p.settings.redChance;
        }
        if (spawner) spawner.redChance = p.settings.redChance;

        // Чтобы стартовый таймер на кнопке показывал верное время до запуска
        if (score) score.sessionDuration = p.settings.level1DurationSec;

        Debug.Log($"[ConfigApplier] Applied: L1={p.settings.level1DurationSec}s, L2={p.settings.level2DurationSec}s, Rest={p.settings.restTimeSec}s, Red={p.settings.redChance:0.##}");
    }
}
