using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PreferencesPanel : MonoBehaviour
{
    [Header("View (values on the right)")]
    [SerializeField] private TextMeshProUGUI level1Text;   // VAL_Level1
    [SerializeField] private TextMeshProUGUI level2Text;   // VAL_Level2
    [SerializeField] private TextMeshProUGUI restText;     // VAL_Rest
    [SerializeField] private TextMeshProUGUI chanceText;   // VAL_Chance

    [Header("Edit (input fields)")]
    [SerializeField] private TMP_InputField level1Input;   // IF_Level1
    [SerializeField] private TMP_InputField level2Input;   // IF_Level2
    [SerializeField] private TMP_InputField restInput;     // IF_Rest
    [SerializeField] private TMP_InputField chanceInput;   // IF_Chance

    [Header("Selected label (optional)")]
    [SerializeField] private TextMeshProUGUI selectedPatientLabel;

    [Header("Change / Save button")]
    [SerializeField] private Button changeButton;               // Change_button
    [SerializeField] private TextMeshProUGUI changeButtonLabel; // TMP внутри кнопки

    private bool isEditing;

    // ------------ Unity ------------
    private void Awake()
    {
        AutoBindByPrefix();
    }

    private void OnEnable()
    {
        var pm = PatientManager.Instance;
        if (pm != null)
        {
            pm.OnSelectedPatientChanged += Render;
            Render(pm.Current);
        }
        SetEditMode(false);
    }

    private void OnDisable()
    {
        var pm = PatientManager.Instance;
        if (pm != null) pm.OnSelectedPatientChanged -= Render;
    }

    // ------------ Binding Helpers ------------
    [ContextMenu("AutoBind (by prefixes)")]
    private void AutoBindByPrefix()
    {
        // Ищем ТОЛЬКО значения с префиксом VAL_
        var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        level1Text ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", System.StringComparison.OrdinalIgnoreCase) && t.name.ToLower().Contains("level1"));
        level2Text ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", System.StringComparison.OrdinalIgnoreCase) && t.name.ToLower().Contains("level2"));
        restText ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", System.StringComparison.OrdinalIgnoreCase) && t.name.ToLower().Contains("rest"));
        chanceText ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", System.StringComparison.OrdinalIgnoreCase) && t.name.ToLower().Contains("chance"));

        // Ищем инпуты с префиксом IF_
        var inputs = GetComponentsInChildren<TMP_InputField>(true);
        level1Input ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", System.StringComparison.OrdinalIgnoreCase) && i.name.ToLower().Contains("level1"));
        level2Input ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", System.StringComparison.OrdinalIgnoreCase) && i.name.ToLower().Contains("level2"));
        restInput ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", System.StringComparison.OrdinalIgnoreCase) && i.name.ToLower().Contains("rest"));
        chanceInput ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", System.StringComparison.OrdinalIgnoreCase) && i.name.ToLower().Contains("chance"));

        // Кнопка и её лейбл
        changeButton ??= GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name.ToLower().Contains("change"));
        if (!changeButtonLabel && changeButton)
            changeButtonLabel = changeButton.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    // ------------ Public API ------------
    public void Render(Patient p)
    {
        if (p == null) return;

        if (selectedPatientLabel)
            selectedPatientLabel.text = $"selected: {p.displayName}";

        if (level1Text) level1Text.text = p.settings.level1DurationSec + " s";
        if (level2Text) level2Text.text = p.settings.level2DurationSec + " s";
        if (restText) restText.text = p.settings.restTimeSec + " s";
        if (chanceText) chanceText.text = p.settings.redChance.ToString("0.##", CultureInfo.InvariantCulture);
    }

    // Повесьте на кнопку Change_button → OnClick
    public void OnChangeClicked()
    {
        if (!isEditing)
        {
            EnterEditModeClear();
        }
        else
        {
            if (TrySaveInputs())
            {
                SetEditMode(false);
                Render(PatientManager.Instance?.Current);
            }
        }
    }

    // ------------ Internals ------------
    private void EnterEditModeClear()
    {
        SetEditMode(true);

        if (level1Input) level1Input.text = "";
        if (level2Input) level2Input.text = "";
        if (restInput) restInput.text = "";
        if (chanceInput) chanceInput.text = "";

        if (level1Input) level1Input.ActivateInputField();
    }

    private void SetEditMode(bool edit)
    {
        isEditing = edit;

        // переключаем ТОЛЬКО значения/инпуты (левые лейблы не трогаем)
        SetActiveSafe(level1Text, !edit);
        SetActiveSafe(level2Text, !edit);
        SetActiveSafe(restText, !edit);
        SetActiveSafe(chanceText, !edit);

        SetActiveSafe(level1Input, edit);
        SetActiveSafe(level2Input, edit);
        SetActiveSafe(restInput, edit);
        SetActiveSafe(chanceInput, edit);

        if (!changeButtonLabel && changeButton)
            changeButtonLabel = changeButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (changeButtonLabel)
            changeButtonLabel.text = edit ? "Save" : "Change";
    }

    private bool TrySaveInputs()
    {
        var pm = PatientManager.Instance;
        var p = pm != null ? pm.Current : null;
        if (p == null) return false;

        int l1 = p.settings.level1DurationSec;
        int l2 = p.settings.level2DurationSec;
        int rt = p.settings.restTimeSec;
        float ch = p.settings.redChance;

        var inv = CultureInfo.InvariantCulture;
        var cur = CultureInfo.CurrentCulture;

        if (level1Input && !string.IsNullOrWhiteSpace(level1Input.text))
        {
            if (!int.TryParse(level1Input.text.Trim(), System.Globalization.NumberStyles.Integer, inv, out l1) &&
                !int.TryParse(level1Input.text.Trim(), System.Globalization.NumberStyles.Integer, cur, out l1))
            { Debug.LogWarning("Level 1: введите целое число секунд"); return false; }
            if (l1 <= 0) { Debug.LogWarning("Level 1 должно быть > 0"); return false; }
        }

        if (level2Input && !string.IsNullOrWhiteSpace(level2Input.text))
        {
            if (!int.TryParse(level2Input.text.Trim(), System.Globalization.NumberStyles.Integer, inv, out l2) &&
                !int.TryParse(level2Input.text.Trim(), System.Globalization.NumberStyles.Integer, cur, out l2))
            { Debug.LogWarning("Level 2: введите целое число секунд"); return false; }
            if (l2 <= 0) { Debug.LogWarning("Level 2 должно быть > 0"); return false; }
        }

        if (restInput && !string.IsNullOrWhiteSpace(restInput.text))
        {
            if (!int.TryParse(restInput.text.Trim(), System.Globalization.NumberStyles.Integer, inv, out rt) &&
                !int.TryParse(restInput.text.Trim(), System.Globalization.NumberStyles.Integer, cur, out rt))
            { Debug.LogWarning("Rest time: введите целое число секунд"); return false; }
            if (rt < 0) { Debug.LogWarning("Rest time не может быть отрицательным"); return false; }
        }

        if (chanceInput && !string.IsNullOrWhiteSpace(chanceInput.text))
        {
            var txt = chanceInput.text.Trim().Replace(',', '.');
            if (!float.TryParse(txt, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out ch))
            { Debug.LogWarning("Red chance: введите число 0..1"); return false; }
            ch = Mathf.Clamp01(ch);
        }

        p.settings.level1DurationSec = l1;
        p.settings.level2DurationSec = l2;
        p.settings.restTimeSec = rt;
        p.settings.redChance = ch;

        pm.SelectByIndex(pm.selectedIndex);
        return true;
    }

    // >>> Здесь главное изменение: уточняем тип
    private static void SetActiveSafe(UnityEngine.Component c, bool active)
    {
        if (!c) return;
        var go = c.gameObject;
        if (go.activeSelf != active) go.SetActive(active);
    }
}
