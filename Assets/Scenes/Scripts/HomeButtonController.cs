using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class HomeButtonController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button homeButton;                   // сам Button
    [SerializeField] private string appShellSceneName = "AppShell";
    [SerializeField] private LevelDirector levelDirector;         // можно не заполнять — найдём автоматически

    private CanvasGroup cg;

    private void Reset()
    {
        if (!homeButton) homeButton = GetComponent<Button>();
        if (!levelDirector) levelDirector = FindObjectOfType<LevelDirector>();
        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (!homeButton) homeButton = GetComponent<Button>();
        if (!levelDirector) levelDirector = FindObjectOfType<LevelDirector>();
        cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (homeButton) homeButton.onClick.AddListener(GoHome);
    }

    private void OnEnable()
    {
        // Подписываемся на события (важно: объект остаётся активным всегда!)
        if (!levelDirector) levelDirector = FindObjectOfType<LevelDirector>();
        if (levelDirector != null)
        {
            levelDirector.OnGameStarted += Hide;
            levelDirector.OnGameFinished += Show;
        }

        // До старта — показываем
        Show();
    }

    private void OnDisable()
    {
        if (levelDirector != null)
        {
            levelDirector.OnGameStarted -= Hide;
            levelDirector.OnGameFinished -= Show;
        }
    }

    public void Show()
    {
        if (!cg) return;
        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        if (homeButton) homeButton.interactable = true;
        // ВАЖНО: НЕ выключаем gameObject!
        // gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (!cg) return;
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        if (homeButton) homeButton.interactable = false;
        // ВАЖНО: НЕ делаем SetActive(false) — иначе отписка от событий и кнопка не вернётся.
    }

    public void GoHome()
    {
        // Здесь можно выключить трекинг/аудио при возврате
        // FindObjectOfType<KinectManager>()?.StopKinect();

        SceneManager.LoadScene(appShellSceneName, LoadSceneMode.Single);
    }

    // На случай, если не хотите ссылку на LevelDirector — можно повесить эти методы на UnityEvent в UI
    public void OnGameStartedHandler() => Hide();
    public void OnGameFinishedHandler() => Show();
}
