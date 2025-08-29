using UnityEngine;
using TMPro;

public class NavigationController : MonoBehaviour
{
    public RectTransform PatientsView;
    public RectTransform NewSessionView;
    public RectTransform SettingsView;

    // <- сюда AppShellBootstrapper передаст ссылку на заголовок
    public TextMeshProUGUI HeaderTitle;

    public void ShowPatients() { Show(PatientsView); SetHeader("Patients"); }
    public void ShowNewSession() { Show(NewSessionView); SetHeader("New session"); }
    public void ShowSettings() { Show(SettingsView); SetHeader("Settings"); }

    private void Show(RectTransform active)
    {
        if (PatientsView) PatientsView.gameObject.SetActive(active == PatientsView);
        if (NewSessionView) NewSessionView.gameObject.SetActive(active == NewSessionView);
        if (SettingsView) SettingsView.gameObject.SetActive(active == SettingsView);
    }

    private void SetHeader(string text)
    {
        if (HeaderTitle != null) HeaderTitle.text = text;
    }
}
