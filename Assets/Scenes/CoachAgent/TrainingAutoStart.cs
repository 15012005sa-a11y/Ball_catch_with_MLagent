using UnityEngine;
using Unity.MLAgents;

public class TrainingAutoStart : MonoBehaviour
{
    public ScoreManager score;   // перетащи в инспекторе

    void Start()
    {
        if (Academy.Instance.IsCommunicatorOn || Application.isBatchMode)
            score?.StartSession();  // тот же метод, что вызывает кнопка «Начать игру»
    }
}
