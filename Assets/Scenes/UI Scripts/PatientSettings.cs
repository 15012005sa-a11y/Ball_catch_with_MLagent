using System;
using UnityEngine;

[Serializable]
public class PatientSettings
{
    public float Level1Duration = 60f;
    public float Level2Duration = 40f;
    public float RestSeconds = 10f;
    public float RedChance = 0.35f;

    public bool StopKinectOnGameEnd = false;
    public bool StopBetweenLevels = false;

    public override string ToString()
    {
        return $"L1={Level1Duration}, L2={Level2Duration}, Rest={RestSeconds}, Red={RedChance}, " +
               $"StopKinect={StopKinectOnGameEnd}, StopBetween={StopBetweenLevels}";
    }
}
