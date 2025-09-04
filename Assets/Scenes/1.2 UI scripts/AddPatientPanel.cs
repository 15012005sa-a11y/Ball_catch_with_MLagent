using TMPro;
using UnityEngine;

public class AddPatientPanel : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField ifName;
    public TMP_InputField ifAge;
    public TMP_InputField ifStarted;
    public TMP_InputField ifL1;
    public TMP_InputField ifL2;
    public TMP_InputField ifRest;
    public TMP_InputField ifChance;

    [Header("Nav")]
    public AppShellNav nav; // чтобы закрывать модалку

    public void OnSave()
    {
        var pm = PatientManager.Instance; if (pm == null) return;

        string name = string.IsNullOrWhiteSpace(ifName?.text) ? "New patient" : ifName.text.Trim();
        int age = ParseInt(ifAge?.text, 0);
        string started = string.IsNullOrWhiteSpace(ifStarted?.text) ? System.DateTime.Now.ToString("dd.MM.yy") : ifStarted.text.Trim();
        int l1 = Mathf.Max(1, ParseInt(ifL1?.text, 180));
        int l2 = Mathf.Max(1, ParseInt(ifL2?.text, 120));
        int rest = Mathf.Max(0, ParseInt(ifRest?.text, 60));
        float chance = Mathf.Clamp01(ParseFloat(ifChance?.text, 0.35f));

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
        Debug.Log($"[AddPatientPanel] Before add: {PatientManager.Instance.patients.Length}");
        pm.AddPatient(p);     // добавили + выделили
        nav?.CloseAddPatient();
        Clear();              // очистить на будущее
    }

    public void OnCancel() { nav?.CloseAddPatient(); }

    int ParseInt(string s, int def) => int.TryParse((s ?? "").Trim(), out var v) ? v : def;
    float ParseFloat(string s, float def)
    {
        if (string.IsNullOrWhiteSpace(s)) return def;
        s = s.Trim().Replace(',', '.');
        return float.TryParse(s, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : def;
    }

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
}