// AddPatientPanel.cs
using TMPro;
using UnityEngine;

public class AddPatientPanel : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField ifName, ifAge, ifStarted, ifL1, ifL2, ifRest, ifChance;

    [Header("Nav")]
    public AppShellNav nav;

    public void OnSave()
    {
        var pm = PatientManager.Instance; if (pm == null) return;

        string name = string.IsNullOrWhiteSpace(ifName?.text) ? "New patient" : ifName.text.Trim();
        int age = TryInt(ifAge?.text, 0);
        string started = string.IsNullOrWhiteSpace(ifStarted?.text) ? System.DateTime.Now.ToString("dd.MM.yy") : ifStarted.text.Trim();
        int l1 = Mathf.Max(1, TryInt(ifL1?.text, 180));
        int l2 = Mathf.Max(1, TryInt(ifL2?.text, 120));
        int rest = Mathf.Max(0, TryInt(ifRest?.text, 60));
        float chance = Mathf.Clamp01(TryFloat(ifChance?.text, 0.35f));

        var p = new Patient
        {
            id = pm.GetNextId(),
            displayName = name,
            age = age,
            startedRehab = started,
            settings = new GameSettings
            {
                level1DurationSec = l1,
                level2DurationSec = l2,
                restTimeSec = rest,
                redChance = chance
            }
        };

        pm.AddPatient(p);                 // добавили и выбрали
        nav?.CloseAddPatient();           // закрыли модалку
        Clear();
    }

    public void OnCancel() { nav?.CloseAddPatient(); Clear(); }

    void Clear()
    {
        ifName?.SetTextWithoutNotify("");
        ifAge?.SetTextWithoutNotify("");
        ifStarted?.SetTextWithoutNotify("");
        ifL1?.SetTextWithoutNotify("");
        ifL2?.SetTextWithoutNotify("");
        ifRest?.SetTextWithoutNotify("");
        ifChance?.SetTextWithoutNotify("");
    }

    int TryInt(string s, int def) => int.TryParse((s ?? "").Trim(), out var v) ? v : def;
    float TryFloat(string s, float def)
    {
        if (string.IsNullOrWhiteSpace(s)) return def;
        s = s.Trim().Replace(',', '.');
        return float.TryParse(s, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : def;
    }
}
