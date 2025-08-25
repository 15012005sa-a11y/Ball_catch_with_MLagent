using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameModeSelector : MonoBehaviour
{
    public enum GamePosture { None = -1, Standing = 0, Sitting = 1 }

    [Header("Refs")]
    public TMP_Dropdown gameModeDropdown;
    public ScoreManager scoreManager;
    public SpawnPointPlacer spawnPointPlacer;

    [Header("Layout")]
    public bool moveDropdownNearStart = true;
    public Vector2 offsetFromStart = new Vector2(220f, 0f);

    [Header("Behaviour")]
    public bool requireSelectionBeforeStart = true;
    public bool rememberChoice = true;

    const string PrefKey = "GamePosture";

    void Awake()
    {
        if (!gameModeDropdown) gameModeDropdown = GetComponentInChildren<TMP_Dropdown>();
    }

    void Start()
    {
        if (!scoreManager || !gameModeDropdown || !spawnPointPlacer)
        {
            Debug.LogWarning("[GameModeSelector] Assign gameModeDropdown, scoreManager, spawnPointPlacer in Inspector.");
            return;
        }

        if (moveDropdownNearStart && scoreManager.startButton)
        {
            var drt = gameModeDropdown.GetComponent<RectTransform>();
            var srt = scoreManager.startButton.GetComponent<RectTransform>();
            if (drt && srt) drt.anchoredPosition = srt.anchoredPosition + offsetFromStart;
        }

        gameModeDropdown.ClearOptions();
        gameModeDropdown.AddOptions(new System.Collections.Generic.List<TMP_Dropdown.OptionData> {
            new TMP_Dropdown.OptionData("Выберите режим"),
            new TMP_Dropdown.OptionData("Стоя"),
            new TMP_Dropdown.OptionData("Сидя")
        });

        if (requireSelectionBeforeStart) scoreManager.SetShowStartButton(false);

        gameModeDropdown.onValueChanged.RemoveAllListeners();
        gameModeDropdown.onValueChanged.AddListener(OnDropdownChanged);

        if (rememberChoice && PlayerPrefs.HasKey(PrefKey))
        {
            var saved = (GamePosture)PlayerPrefs.GetInt(PrefKey, (int)GamePosture.None);
            int idx = PostureToIndex(saved);
            gameModeDropdown.SetValueWithoutNotify(idx);
            ApplySelection(saved, true);
        }
        else
        {
            gameModeDropdown.SetValueWithoutNotify(0);
        }
    }

    void OnDropdownChanged(int index)
    {
        ApplySelection(IndexToPosture(index), true);
    }

    void ApplySelection(GamePosture posture, bool revealStart)
    {
        if (posture == GamePosture.None)
        {
            if (requireSelectionBeforeStart) scoreManager.SetShowStartButton(false);
            return;
        }

        // Настраиваем SpawnPointPlacer под выбранный режим
        if (posture == GamePosture.Standing)
        {
            spawnPointPlacer.posture = SpawnPointPlacer.Posture.Standing;
        }
        else
        {
            spawnPointPlacer.posture = SpawnPointPlacer.Posture.Sitting;
            spawnPointPlacer.sittingTwoRows = true;  // опционально: два ряда
        }
        spawnPointPlacer.PlaceSpawnPoints();         // переставит точки и авто-привяжет спавнеру

        if (revealStart && requireSelectionBeforeStart)
            scoreManager.SetShowStartButton(true);

        if (rememberChoice)
        {
            PlayerPrefs.SetInt(PrefKey, (int)posture);
            PlayerPrefs.Save();
        }
    }

    static GamePosture IndexToPosture(int idx)
    {
        return idx == 1 ? GamePosture.Standing :
               idx == 2 ? GamePosture.Sitting :
               GamePosture.None;
    }

    static int PostureToIndex(GamePosture p)
    {
        return p == GamePosture.Standing ? 1 :
               p == GamePosture.Sitting ? 2 : 0;
    }
}
