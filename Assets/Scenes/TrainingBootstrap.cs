// TrainingBootstrap.cs (�������� �� ������ GameObject)
using UnityEngine;
public class TrainingBootstrap : MonoBehaviour
{
    void Start()
    {
        var dir = FindObjectOfType<LevelDirector>(true);
        dir?.StartGameplay();                       // ���������� L1 -> (�����) -> L2

        var s = FindObjectOfType<ScoreManager>(true);
        if (s)
        {
            s.SetShowStartButton(false);
            s.SetShowGraphButton(false);
        }
    }
}
