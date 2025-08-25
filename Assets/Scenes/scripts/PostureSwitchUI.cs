using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PostureSwitchUI : MonoBehaviour
{
    [Header("Refs")]
    public ScoreManager scoreManager;
    public SpawnPointPlacer spawnPointPlacer;

    [Header("Layout")]
    public Vector2 offsetFromStart = new Vector2(180f, 0f);

    [Header("Behaviour")]
    public bool requireSelectionBeforeStart = true;

    private Button sittingButton;

    private void Start()
    {
        if (scoreManager == null || scoreManager.startButton == null)
        {
            Debug.LogWarning("[PostureSwitchUI] scoreManager/startButton is not assigned");
            return;
        }

        if (requireSelectionBeforeStart)
            scoreManager.SetShowStartButton(false);

        var startObj = scoreManager.startButton;
        var parent = startObj.transform.parent;
        var clone = Instantiate(startObj, parent);
        clone.name = "Button_Sit";

        var rt = clone.GetComponent<RectTransform>();
        var startRt = startObj.GetComponent<RectTransform>();
        if (rt && startRt) rt.anchoredPosition = startRt.anchoredPosition + offsetFromStart;

        var txt = clone.GetComponentInChildren<TMP_Text>();
        if (txt) txt.text = "Сидя";

        sittingButton = clone.GetComponent<Button>();
        if (sittingButton)
        {
            sittingButton.onClick.RemoveAllListeners();
            sittingButton.onClick.AddListener(SetSittingMode);
        }
    }

    public void SetSittingMode()
    {
        if (!spawnPointPlacer)
        {
            Debug.LogWarning("[PostureSwitchUI] spawnPointPlacer is not assigned");
            return;
        }

        // переходим в сидячий пресет и раскладываем точки
        spawnPointPlacer.posture = SpawnPointPlacer.Posture.Sitting;
        spawnPointPlacer.sittingTwoRows = true;     // опционально: два ряда для сидя
        spawnPointPlacer.PlaceSpawnPoints();

        if (requireSelectionBeforeStart)
            scoreManager.SetShowStartButton(true);

        if (sittingButton)
        {
            var t = sittingButton.GetComponentInChildren<TMP_Text>();
            if (t) t.text = "Сидя ✓";
            sittingButton.interactable = false;
        }
    }
}
