using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Creates a "сидя" button next to the start button and
/// switches spawn points to the sitting configuration.
/// </summary>
public class PostureSwitchUI : MonoBehaviour
{
    [Tooltip("Reference to ScoreManager to clone its Start button")]
    public ScoreManager scoreManager;

    [Tooltip("SpawnPointPlacer used to reposition spawn points")]
    public SpawnPointPlacer spawnPointPlacer;

    [Tooltip("Vertical offset for the sitting button relative to Start button")]
    public Vector2 sittingButtonOffset = new Vector2(0f, -40f);

    private Button sittingButton;

    private void Start()
    {
        // Create the sitting button if references are assigned
        if (scoreManager != null && scoreManager.startButton != null)
        {
            var startObj = scoreManager.startButton;
            var parent = startObj.transform.parent;

            // Clone the Start button
            var clone = Instantiate(startObj, parent);
            clone.name = "Button_Sit";

            // Adjust position
            var rt = clone.GetComponent<RectTransform>();
            var startRt = startObj.GetComponent<RectTransform>();
            if (rt != null && startRt != null)
            {
                rt.anchoredPosition = startRt.anchoredPosition + sittingButtonOffset;
            }

            // Change displayed text to "сидя"
            var text = clone.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.text = "сидя";

            // Hook up click event
            sittingButton = clone.GetComponent<Button>();
            if (sittingButton != null)
                sittingButton.onClick.AddListener(SetSittingMode);
        }
        else
        {
            Debug.LogWarning("[PostureSwitchUI] scoreManager or startButton not assigned");
        }
    }

    /// <summary>
    /// Switch spawn points to the sitting preset.
    /// </summary>
    public void SetSittingMode()
    {
        if (spawnPointPlacer != null)
        {
            spawnPointPlacer.posture = SpawnPointPlacer.Posture.Sitting;
            spawnPointPlacer.PlaceSpawnPoints();
        }
        else
        {
            Debug.LogWarning("[PostureSwitchUI] spawnPointPlacer not assigned");
        }
    }
}
