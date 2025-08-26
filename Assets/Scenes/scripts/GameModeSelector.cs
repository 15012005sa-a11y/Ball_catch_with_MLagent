using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameModeSelector : MonoBehaviour
{
    public enum GamePosture { None = -1, Standing = 0, Sitting = 1 }

    [Header("Refs")]
    public TMP_Dropdown gameModeDropdown;     // Dropdown "Режим игры"
    public ScoreManager scoreManager;         // содержит startButton и SetShowStartButton
    public SpawnPointPlacer spawnPointPlacer; // ваш SpawnPointPlacer

    [Header("Layout")]
    public bool moveDropdownNearStart = true;
    public Vector2 offsetFromStart = new Vector2(220f, 0f);

    [Header("Behaviour")]
    public bool requireSelectionBeforeStart = true;  // скрывать Start до выбора режима
    public bool rememberChoice = true;               // запоминать выбор между запусками

    [Header("Progression")]
    [Tooltip("Сколько сессий/уровней нужно завершить, чтобы снова показать список")]
    [Min(1)] public int sessionsToUnlockDropdown = 2;

    private int finishedSessions = 0;
    const string PrefKey = "GamePosture";

    void Awake()
    {
        if (!gameModeDropdown) gameModeDropdown = GetComponentInChildren<TMP_Dropdown>();
    }

    void OnEnable()
    {
        // показать список только после завершения всех уровней
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnSessionFinished.AddListener(HandleSessionFinished);

        // прятать дропдаун при старте игры
        if (scoreManager && scoreManager.startButton)
        {
            var btn = scoreManager.startButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(HideForGameplay);
                btn.onClick.AddListener(HideForGameplay);
            }
        }
    }

    void OnDisable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnSessionFinished.RemoveListener(HandleSessionFinished);

        if (scoreManager && scoreManager.startButton)
        {
            var btn = scoreManager.startButton.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveListener(HideForGameplay);
        }
    }

    void Start()
    {
        if (!scoreManager || !gameModeDropdown || !spawnPointPlacer)
        {
            Debug.LogWarning("[GameModeSelector] Assign gameModeDropdown, scoreManager, spawnPointPlacer in Inspector.");
            return;
        }

        // расположить рядом с кнопкой "Начать"
        if (moveDropdownNearStart && scoreManager.startButton)
        {
            var drt = gameModeDropdown.GetComponent<RectTransform>();
            var srt = scoreManager.startButton.GetComponent<RectTransform>();
            if (drt && srt) drt.anchoredPosition = srt.anchoredPosition + offsetFromStart;
        }

        // опции
        gameModeDropdown.ClearOptions();
        gameModeDropdown.AddOptions(new System.Collections.Generic.List<TMP_Dropdown.OptionData> {
            new TMP_Dropdown.OptionData("Выберите режим"),
            new TMP_Dropdown.OptionData("Стоя"),
            new TMP_Dropdown.OptionData("Сидя")
        });

        if (requireSelectionBeforeStart) scoreManager.SetShowStartButton(false);

        // обработчик выбора
        gameModeDropdown.onValueChanged.RemoveAllListeners();
        gameModeDropdown.onValueChanged.AddListener(OnDropdownChanged);

        // восстановление выбора
        if (rememberChoice && PlayerPrefs.HasKey(PrefKey))
        {
            var saved = (GamePosture)PlayerPrefs.GetInt(PrefKey, (int)GamePosture.None);
            int idx = PostureToIndex(saved);
            gameModeDropdown.SetValueWithoutNotify(idx);
            ApplySelection(saved, revealStart: true);
        }
        else
        {
            gameModeDropdown.SetValueWithoutNotify(0);
        }

        // в стартовом меню список виден
        ShowDropdown(true);
        finishedSessions = 0;
    }

    void OnDropdownChanged(int index) =>
        ApplySelection(IndexToPosture(index), revealStart: true);

    void ApplySelection(GamePosture posture, bool revealStart)
    {
        if (posture == GamePosture.None)
        {
            if (requireSelectionBeforeStart) scoreManager.SetShowStartButton(false);
            return;
        }

        // переключаем пресет в SpawnPointPlacer и раскладываем точки
        spawnPointPlacer.posture =
            (posture == GamePosture.Standing) ? SpawnPointPlacer.Posture.Standing
                                              : SpawnPointPlacer.Posture.Sitting;
        spawnPointPlacer.PlaceSpawnPoints();

        if (revealStart && requireSelectionBeforeStart)
            scoreManager.SetShowStartButton(true);

        if (rememberChoice)
        {
            PlayerPrefs.SetInt(PrefKey, (int)posture);
            PlayerPrefs.Save();
        }
    }

    // нажали "Начать" — уходим в игру и прячем список
    void HideForGameplay()
    {
        ShowDropdown(false);
        finishedSessions = 0; // старт нового прохождения
    }

    // вызывается ПОСЛЕ КАЖДОЙ сессии/уровня
    void HandleSessionFinished()
    {
        finishedSessions++;
        if (finishedSessions >= Mathf.Max(1, sessionsToUnlockDropdown))
        {
            ShowDropdown(true);    // показать только когда закончили все уровни
            finishedSessions = 0;  // готово к новому циклу
        }
        else
        {
            ShowDropdown(false);   // между уровнями не показывать
        }
    }

    void ShowDropdown(bool show) =>
        gameModeDropdown.gameObject.SetActive(show);

    static GamePosture IndexToPosture(int idx) =>
        idx == 1 ? GamePosture.Standing :
        idx == 2 ? GamePosture.Sitting : GamePosture.None;

    static int PostureToIndex(GamePosture p) =>
        p == GamePosture.Standing ? 1 :
        p == GamePosture.Sitting ? 2 : 0;
}
