using UnityEngine;
using Unity.MLAgents;

public class TrainingAutoStart : MonoBehaviour
{
    public ScoreManager score;   // �������� � ����������

    void Start()
    {
        // �������� ���������� ������ ��� ��������/����-������
        if (Academy.Instance.IsCommunicatorOn || Application.isBatchMode)
        {
            if (score != null)
                score.StartSession();
            else
                Debug.LogWarning("[AutoStart] ScoreManager �� �������� � ������ �� ��������.");
        }
    }
}
