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
        // вернуть врем€
        Time.timeScale = 1f;

        // аккуратно остановить то, что могло остатьс€
        try { FindObjectOfType<BallSpawnerBallCatch>()?.StopSpawning(); } catch { }
        try { FindObjectOfType<MotionLogger>()?.StopLogging("ReturnHome"); } catch { }

        // если вдруг есть Ђживойї менеджер в DontDestroyOnLoad Ч удалим
        if (ScoreManager.Instance && ScoreManager.Instance.gameObject.scene.buildIndex == -1)
            Destroy(ScoreManager.Instance.gameObject);

        // загрузить AppShell
        SceneManager.LoadScene(appShellSceneName, LoadSceneMode.Single);
    }
}
