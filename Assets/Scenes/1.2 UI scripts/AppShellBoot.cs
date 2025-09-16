// AppShellBoot.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class AppShellBoot : MonoBehaviour
{
    void Awake()
    {
        Time.timeScale = 1f;

        // ���� �� �����-�� ������� ScoreManager �� ��� ���� � DontDestroyOnLoad � ����� ���
        if (ScoreManager.Instance && ScoreManager.Instance.gameObject.scene.buildIndex == -1)
            Destroy(ScoreManager.Instance.gameObject);

        // �� ������ ������: ���������, ��� � ����� ����� ���� EventSystem
        var ev = FindObjectsOfType<EventSystem>();
        for (int i = 1; i < ev.Length; i++) Destroy(ev[i].gameObject);
    }
}
