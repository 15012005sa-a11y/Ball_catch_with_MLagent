using UnityEngine;
using UnityEngine.UI;

public class AppShellNav : MonoBehaviour
{
    [Header("Groups")]
    public GameObject mainGroup;        // MainGroup
    public GameObject preferencesGroup; // PreferencesGroup

    [Header("Scroll")]
    public ScrollRect scrollRect;       // RootScroll (компонент ScrollRect)
    public PreferencesPanel preferencesPanel; // ссылка для явной перерисовки по нажатию

    void Start()
    {
        ShowMain();
    }

    public void OnPreferencesButton()
    {
        ShowPreferences();
    }

    public void OnBackButton()
    {
        ShowMain();
    }

    public void ShowMain()
    {
        mainGroup.SetActive(true);
        preferencesGroup.SetActive(false);
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f; // прокрутка к верху
    }

    public void ShowPreferences()
    {
        mainGroup.SetActive(false);
        preferencesGroup.SetActive(true);
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
        if (preferencesPanel != null && PatientManager.Instance != null)
            preferencesPanel.Render(PatientManager.Instance.Current);
    }
}
