using UnityEngine;

public class ApplySessionConfig : MonoBehaviour
{
    void Awake()
    {
        var cfg = AppState.Config;

        var dir = FindObjectOfType<LevelDirector>();
        if (dir != null)
        {
            dir.level1Duration = cfg.Level1Duration;
            dir.level2Duration = cfg.Level2Duration;
            dir.redChance = cfg.RedChance;
            try { var f = dir.GetType().GetField("stopBetweenLevels"); if (f != null) f.SetValue(dir, cfg.StopBetweenLevels); } catch { }
        }

        var score = FindObjectOfType<ScoreManager>();
        if (score != null)
        {
            try { var f = score.GetType().GetField("stopKinectOnGameEnd"); if (f != null) f.SetValue(score, cfg.StopKinectOnGameEnd); } catch { }
        }

        // NEW: если есть PatientManager Ч выставим выбранного
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
