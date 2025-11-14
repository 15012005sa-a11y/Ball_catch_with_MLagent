using UnityEngine;
using Unity.MLAgents;

public class TrainingAutoStart : MonoBehaviour
{
    public ScoreManager score;   // перетащи в инспекторе

    void Start()
    {
        // ¬ключаем автозапуск только при обучении/батч-режиме
        if (Academy.Instance.IsCommunicatorOn || Application.isBatchMode)
        {
            if (score != null)
                score.StartSession();
            else
                Debug.LogWarning("[AutoStart] ScoreManager не назначен Ч сесси€ не запущена.");
        }
    }
}
