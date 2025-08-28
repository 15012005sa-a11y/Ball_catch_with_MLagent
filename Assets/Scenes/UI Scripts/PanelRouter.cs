using UnityEngine;

public class PanelRouter : MonoBehaviour
{
    public GameObject panelPatients;
    public GameObject panelNewSession;
    public GameObject panelHistory;
    public GameObject panelSettings;

    GameObject _current;

    void Awake()
    {
        // включённая по умолчанию панель
        if (panelPatients != null) { Show(panelPatients); }
    }

    public void ShowPatients() { Show(panelPatients); }
    public void ShowNewSession() { Show(panelNewSession); }
    public void ShowHistory() { Show(panelHistory); }
    public void ShowSettings() { Show(panelSettings); }

    void Show(GameObject target)
    {
        if (target == null) return;
        if (_current != null) _current.SetActive(false);
        _current = target;
        _current.SetActive(true);
    }
}