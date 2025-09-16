using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Панель паузы: показывает/скрывает PausePanel, умеет «Главная» и «Продолжить»,
/// появляется при паузе и по завершении игры. Делает панель поверх всех UI,
/// чтобы кнопки не перекрывались экраном обратного отсчёта и др. оверлеями.
/// </summary>
public class PauseUI : MonoBehaviour
{
    [Header("Refs (опционально — найдём автоматически)")]
    public LevelDirector levelDirector;     // чтобы реагировать на Start/Finish
    public ScoreManager score;              // чтобы ловить OnSessionFinished

    [Header("UI")]
    public Button pauseButton;              // HUD-кнопка «Пауза»
    public CanvasGroup pausePanel;          // оверлей-панель (должен быть CanvasGroup)
    public Button resumeButton;             // кнопка «Продолжить» внутри панели
    public GameObject homeButton;           // кнопка «Главная» внутри панели (с HomeButtonController)

    [Header("Gameplay")]
    public BallSpawnerBallCatch spawner;    // если спавнер работает по UnscaledTime — выключим его вручную
    public KeyCode toggleKey = KeyCode.Escape;

    [Header("Behaviour")]
    public bool showHomeOnPause = true;     // показывать «Главная» в режиме паузы
    public bool showPanelAtGameEnd = true;  // показать эту же панель в конце игры
    public bool showResumeOnGameEnd = false;// в конце игры «Продолжить» обычно не нужно

    [Header("Sorting (чтобы панель была поверх всего)")]
    [SerializeField] private bool forceOverlayTop = true;
    [SerializeField] private int overlaySortingOrder = 5000; // выше любого другого Canvas в сцене

    private bool _paused;

    private void Reset()
    {
        levelDirector = FindObjectOfType<LevelDirector>(true);
        score = FindObjectOfType<ScoreManager>(true);
        spawner = FindObjectOfType<BallSpawnerBallCatch>(true);
        if (!pausePanel) pausePanel = GetComponentInChildren<CanvasGroup>(true);
        if (!pauseButton) pauseButton = GetComponentInChildren<Button>(true);
        if (!resumeButton) resumeButton = pausePanel ? pausePanel.GetComponentInChildren<Button>(true) : null;
        if (!homeButton && pausePanel) homeButton = pausePanel.transform.Find("HomeButton")?.gameObject;
    }

    private void Awake()
    {
        Time.timeScale = 1f;
        if (!levelDirector) levelDirector = FindObjectOfType<LevelDirector>(true);
        if (!score) score = FindObjectOfType<ScoreManager>(true);
        if (!spawner) spawner = FindObjectOfType<BallSpawnerBallCatch>(true);

        HidePanel();
        SetPauseButtonVisible(false);

        if (pauseButton)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(Pause);
        }
        if (resumeButton)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(Resume);
        }

        // Гарантируем, что панель сможет перехватывать клики поверх любых экранов («Приготовьтесь…» и т.д.)
        if (forceOverlayTop && pausePanel)
            EnsureTopmost(pausePanel.gameObject);
    }

    private void OnEnable()
    {
        if (!levelDirector) levelDirector = FindObjectOfType<LevelDirector>(true);
        if (!score) score = FindObjectOfType<ScoreManager>(true);

        if (levelDirector != null)
        {
            levelDirector.OnGameStarted += OnGameStarted;
            levelDirector.OnGameFinished += OnGameFinished;
        }
        if (score != null)
        {
            score.OnSessionFinished.AddListener(OnGameFinished);
        }
    }

    private void OnDisable()
    {
        if (levelDirector != null)
        {
            levelDirector.OnGameStarted -= OnGameStarted;
            levelDirector.OnGameFinished -= OnGameFinished;
        }
        if (score != null)
        {
            score.OnSessionFinished.RemoveListener(OnGameFinished);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (_paused) Resume();
            else Pause();
        }
    }

    // ===================== ПАУЗА / ПРОДОЛЖИТЬ =====================
    public void Pause()
    {
        if (_paused) return;
        _paused = true;

        Time.timeScale = 0f;
        if (!spawner) spawner = FindObjectOfType<BallSpawnerBallCatch>(true);
        if (spawner) spawner.enabled = false; // на случай UnscaledTime

        ShowPanel();
        if (resumeButton) resumeButton.gameObject.SetActive(true);
        if (homeButton) homeButton.SetActive(showHomeOnPause);

        SetPauseButtonVisible(false);
    }

    public void Resume()
    {
        if (!_paused)
        {
            Time.timeScale = 1f;
            return;
        }
        _paused = false;

        Time.timeScale = 1f;
        if (!spawner) spawner = FindObjectOfType<BallSpawnerBallCatch>(true);
        if (spawner) spawner.enabled = true;

        HidePanel();
        SetPauseButtonVisible(true);
    }

    // ===================== РЕАКЦИИ НА СОБЫТИЯ ИГРЫ =====================
    private void OnGameStarted()
    {
        _paused = false;
        Time.timeScale = 1f;
        HidePanel();
        SetPauseButtonVisible(true);
        if (resumeButton) resumeButton.gameObject.SetActive(true);
    }

    private void OnGameFinished()
    {
        _paused = false;
        Time.timeScale = 1f;
        SetPauseButtonVisible(false);

        if (showPanelAtGameEnd && pausePanel)
        {
            ShowPanel();
            if (homeButton) homeButton.SetActive(true);
            if (resumeButton) resumeButton.gameObject.SetActive(showResumeOnGameEnd);
        }
        else
        {
            HidePanel();
        }
    }

    // ===================== ВСПОМОГАТЕЛЬНЫЕ =====================
    private void ShowPanel()
    {
        if (!pausePanel) return;

        // гарантированно поверх всех UI
        if (forceOverlayTop)
            EnsureTopmost(pausePanel.gameObject);
        pausePanel.transform.SetAsLastSibling();

        pausePanel.gameObject.SetActive(true);
        pausePanel.alpha = 1f;
        pausePanel.interactable = true;
        pausePanel.blocksRaycasts = true;
    }

    private void HidePanel()
    {
        if (!pausePanel) return;
        pausePanel.gameObject.SetActive(true); // не выключаем объект, чтобы не терять ссылки/подписки
        pausePanel.alpha = 0f;
        pausePanel.interactable = false;
        pausePanel.blocksRaycasts = false;
    }

    private void SetPauseButtonVisible(bool visible)
    {
        if (pauseButton) pauseButton.gameObject.SetActive(visible);
    }

    private void EnsureTopmost(GameObject go)
    {
        // у панели должен быть собственный Canvas и GraphicRaycaster,
        // и высокий порядок сортировки — тогда она перехватывает клики поверх любого оверлея
        var cv = go.GetComponent<Canvas>();
        if (!cv) cv = go.AddComponent<Canvas>();
        cv.overrideSorting = true;
        cv.sortingOrder = overlaySortingOrder;

        if (!go.TryGetComponent<UnityEngine.UI.GraphicRaycaster>(out _))
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    }
}
