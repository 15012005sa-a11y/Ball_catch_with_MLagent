using UnityEngine;
using UnityEngine.SceneManagement;

public enum AppState { Home, InGame }

public class AppManager : MonoBehaviour
{
    public static AppManager I;
    public AppState State { get; private set; }

    void Awake()
    {
        if (I == null) { I = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void GoHome() { State = AppState.Home; SceneManager.LoadScene("AppShell"); }
    public void StartGame() { State = AppState.InGame; SceneManager.LoadScene("GameScene"); }
}