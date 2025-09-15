using UnityEngine;

public class ApplySessionConfig : MonoBehaviour
{
    void Awake()
    {
        var cfg = AppState.Config;

        var dir = FindObjectOfType<LevelDirector>();
        if (dir != null)
        {
            // cfg.Level1Duration/Level2Duration Ч float, а у LevelDirector пол€ int
            dir.level1Duration = Mathf.RoundToInt(cfg.Level1Duration);
            dir.level2Duration = Mathf.RoundToInt(cfg.Level2Duration);
            dir.redChance = cfg.RedChance;

            try
            {
                var f = dir.GetType().GetField("stopBetweenLevels");
                if (f != null) f.SetValue(dir, cfg.StopBetweenLevels);
            }
            catch { }
        }

        var score = FindObjectOfType<ScoreManager>();
        if (score != null)
        {
            try
            {
                var f = score.GetType().GetField("stopKinectOnGameEnd");
                if (f != null) f.SetValue(score, cfg.StopKinectOnGameEnd);
            }
            catch { }
        }

        // ≈сли есть PatientManager Ч выставим выбранного пациента
        var pm = FindObjectOfType<PatientManager>();
        if (pm != null && cfg.SelectedPatientId >= 0)
        {
            try
            {
                var f = pm.GetType().GetField("CurrentPatientID");
                if (f != null) f.SetValue(pm, cfg.SelectedPatientId);
            }
            catch { }
        }
    }
}
