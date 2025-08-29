using UnityEngine;

public class SessionConfig
{
    public float Level1Duration = 60f;
    public float Level2Duration = 40f;
    public float RestSeconds = 10f;
    public float RedChance = 0.35f;

    public bool StopKinectOnGameEnd = false;
    public bool StopBetweenLevels = false;

    // NEW: выбор пациента
    public int SelectedPatientId = -1;
    public string SelectedPatientName = "";
}
