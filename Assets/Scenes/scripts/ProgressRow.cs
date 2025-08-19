using System;

[Serializable]
public struct ProgressRow
{
    public int PatientID;
    public DateTime Date;
    public int Score;
    public float SuccessRate;
    public float Reaction;
    public float RightHand;
    public float LeftHand;
}
