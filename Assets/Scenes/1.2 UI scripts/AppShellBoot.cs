// AppShellBoot.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class AppShellBoot : MonoBehaviour
{
    void Awake()
    {
        Time.timeScale = 1f;

        // Если по какой-то причине ScoreManager всё ещё живёт в DontDestroyOnLoad — снесём его
        if (ScoreManager.Instance && ScoreManager.Instance.gameObject.scene.buildIndex == -1)
            Destroy(ScoreManager.Instance.gameObject);

        // На всякий случай: убедиться, что в сцене ровно один EventSystem
        var ev = FindObjectsOfType<EventSystem>();
        for (int i = 1; i < ev.Length; i++) Destroy(ev[i].gameObject);
    }
}
