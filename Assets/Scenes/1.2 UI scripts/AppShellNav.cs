using UnityEngine;
using UnityEngine.UI;

public class AppShellNav : MonoBehaviour
{
    [Header("Groups (основные экраны)")]
    [SerializeField] GameObject mainGroup;         // Content/MainGroup
    [SerializeField] GameObject preferencesGroup;  // Content/PreferencesGroup

    [Header("Modal overlay (поверх)")]
    [SerializeField] CanvasGroup addPatientOverlay; // CanvasGroup на AddPatientOverlay

    [Header("Scroll / other")]
    [SerializeField] ScrollRect scrollRect;        // Canvas/RootScroll
    [SerializeField] PreferencesPanel preferencesPanel;

    void Awake()
    {
        // Автопоиск AddPatientOverlay, если не назначен
        if (!addPatientOverlay)
        {
            var go = GameObject.Find("AddPatientOverlay");
            if (go)
            {
                // гарантируем нужные компоненты
                var cg = go.GetComponent<CanvasGroup>();
                if (!cg) cg = go.AddComponent<CanvasGroup>();
                cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true;

                var cv = go.GetComponent<Canvas>();
                if (!cv) cv = go.AddComponent<Canvas>();
                cv.overrideSorting = true; cv.sortingOrder = 50;

                addPatientOverlay = cg;
            }
            else
            {
                Debug.LogWarning("[AppShellNav] Не найден объект 'AddPatientOverlay' под Canvas. Создайте его или назначьте ссылку.");
            }
        }
    }

    void Start()
    {
        HideAllOverlays();
        ShowMain();
    }

    // ----- кнопки -----
    public void OnPreferencesButton() => ShowPreferences();
    public void OnBackButton() => ShowMain();
    public void OnAddPatientButton() => OpenAddPatient();
    public void OnAddPatientCancel() => CloseAddPatient();

    // ----- экраны -----
    public void ShowMain()
    {
        if (mainGroup) mainGroup.SetActive(true);
        if (preferencesGroup) preferencesGroup.SetActive(false);
        HideAllOverlays();
        ResetScrollToTop();
    }

    public void ShowPreferences()
    {
        if (mainGroup) mainGroup.SetActive(false);
        if (preferencesGroup) preferencesGroup.SetActive(true);
        HideAllOverlays();
        ResetScrollToTop();
        if (preferencesPanel && PatientManager.Instance != null)
            preferencesPanel.Render(PatientManager.Instance.Current);
    }

    // ----- модалка -----
    public void OpenAddPatient()
    {
        if (addPatientOverlay)
        {
            var rt = addPatientOverlay.GetComponent<RectTransform>();
            if (rt)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.anchoredPosition3D = Vector3.zero;
            }
        }
        SetOverlay(addPatientOverlay, true);
        if (scrollRect) scrollRect.vertical = false;
    }


    public void CloseAddPatient()
    {
        SetOverlay(addPatientOverlay, false);
        if (scrollRect) scrollRect.vertical = true;
        Debug.Log("[AppShellNav] AddPatientOverlay: CLOSE");
    }

    // ----- helpers -----
    void HideAllOverlays() => SetOverlay(addPatientOverlay, false);

    void SetOverlay(CanvasGroup cg, bool on)
    {
        if (!cg) return;
        cg.alpha = on ? 1f : 0f;
        cg.interactable = on;
        cg.blocksRaycasts = on;
        cg.gameObject.SetActive(on);
    }

    void ResetScrollToTop()
    {
        if (scrollRect) scrollRect.verticalNormalizedPosition = 1f;
    }
}
