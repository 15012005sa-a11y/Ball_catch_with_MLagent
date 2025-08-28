using UnityEngine;
using UnityEngine.UI;

public class AppShellUI : MonoBehaviour
{
    [Header("Refs")]
    public PanelRouter router;

    [Header("Buttons")]
    public Button btnPatients;
    public Button btnNewSession;
    public Button btnHistory;
    public Button btnSettings;

    void Awake()
    {
        btnPatients.onClick.AddListener(router.ShowPatients);
        btnNewSession.onClick.AddListener(router.ShowNewSession);
        btnHistory.onClick.AddListener(router.ShowHistory);
        btnSettings.onClick.AddListener(router.ShowSettings);
    }
}