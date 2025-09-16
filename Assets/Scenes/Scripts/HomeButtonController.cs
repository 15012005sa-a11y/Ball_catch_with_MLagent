using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class HomeButtonController : MonoBehaviour
{
    [SerializeField] private string appShellSceneName = "AppShell";

    private Button _btn;

    private void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick.RemoveAllListeners();
        _btn.onClick.AddListener(GoHome);
    }

    public void GoHome()
    {
        // ������� �����
        Time.timeScale = 1f;

        // ��������� ���������� ��, ��� ����� ��������
        try { FindObjectOfType<BallSpawnerBallCatch>()?.StopSpawning(); } catch { }
        try { FindObjectOfType<MotionLogger>()?.StopLogging("ReturnHome"); } catch { }

        // ���� ����� ���� ������ �������� � DontDestroyOnLoad � ������
        if (ScoreManager.Instance && ScoreManager.Instance.gameObject.scene.buildIndex == -1)
            Destroy(ScoreManager.Instance.gameObject);

        // ��������� AppShell
        SceneManager.LoadScene(appShellSceneName, LoadSceneMode.Single);
    }
}
