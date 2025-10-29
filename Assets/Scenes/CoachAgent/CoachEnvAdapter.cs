using UnityEngine;

public class CoachEnvAdapter : MonoBehaviour
{
    public PerformanceWindow perf;
    public ScoreManager score; // подставь свою реализацию и подпишись на правильное событие

    void OnEnable()
    {
        if (score != null)
        {
            // Предполагаем событие: OnBallResult(bool success, float reactionSec, float rom01)
            score.OnBallResult += OnBallResult;
        }
    }

    void OnDisable()
    {
        if (score != null)
            score.OnBallResult -= OnBallResult;
    }

    void Handle(bool success, float reactionSec, float rom01)
    {
        perf.Report(success, reactionSec, rom01);
    }

    private void OnBallResult(bool success, float reactionSec, float rom01)
    {
        if (perf != null)
            perf.Report(success, reactionSec, rom01);
    }
}
