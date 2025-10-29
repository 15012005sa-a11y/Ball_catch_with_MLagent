// TrainingBootstrap.cs (повесьте на пустой GameObject)
using UnityEngine;
public class TrainingBootstrap : MonoBehaviour
{
    void Start()
    {
        var dir = FindObjectOfType<LevelDirector>(true);
        dir?.StartGameplay();                       // автозапуск L1 -> (отдых) -> L2

        var s = FindObjectOfType<ScoreManager>(true);
        if (s)
        {
            s.SetShowStartButton(false);
            s.SetShowGraphButton(false);
        }
    }
}
