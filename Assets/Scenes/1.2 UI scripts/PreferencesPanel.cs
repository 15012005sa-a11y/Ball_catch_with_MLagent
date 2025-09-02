using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PreferencesPanel : MonoBehaviour
{
    [Header("Labels on the panel (view mode)")]
    [SerializeField] private TextMeshProUGUI selectedPatientLabel; // "selected: Patient 1" (опционально)
    [SerializeField] private TextMeshProUGUI level1Text;     // "180 s"
    [SerializeField] private TextMeshProUGUI level2Text;     // "120 s"
    [SerializeField] private TextMeshProUGUI restText;       // "60 s"
    [SerializeField] private TextMeshProUGUI chanceText;     // "0.35"

    [Header("Inputs (edit mode)")]
    [SerializeField] private TMP_InputField level1Input;     // IF_Level1
    [SerializeField] private TMP_InputField level2Input;     // IF_Level2
    [SerializeField] private TMP_InputField restInput;       // IF_Rest
    [SerializeField] private TMP_InputField chanceInput;     // IF_Chance

    [Header("Change / Save button")]
    [SerializeField] private Button changeButton;                // сам Button
    [SerializeField] private TextMeshProUGUI changeButtonLabel;  // текст на кнопке (можно не задавать)

    private bool isEditing;

    // ---------- Lifecycle ----------
    private void Awake()
    {
        // Попытаться автопривязать, если что-то не заполнено в инспекторе
        TryAutoBind();
    }

    private void OnEnable()
    {
        if (PatientManager.Instance != null)
        {
            PatientManager.Instance.OnSelectedPatientChanged += Render;
            Render(PatientManager.Instance.Current);
        }
        SetEditMode(false); // важно: после TryAutoBind
    }

    private void OnDisable()
    {
        if (PatientManager.Instance != null)
            PatientManager.Instance.OnSelectedPatientChanged -= Render;
    }

    // ---------- Public API ----------
    public void Render(Patient p)
    {
        if (p == null) return;

        if (selectedPatientLabel != null)
            selectedPatientLabel.text = $"selected: {p.displayName}";

        if (level1Text != null) level1Text.text = p.settings.level1DurationSec + " s";
        if (level2Text != null) level2Text.text = p.settings.level2DurationSec + " s";
        if (restText != null) restText.text = p.settings.restTimeSec + " s";
        if (chanceText != null) chanceText.text = p.settings.redChance.ToString("0.##", CultureInfo.InvariantCulture);
    }

    /// Назначьте этот метод на OnClick у кнопки Change.
    public void OnChangeClicked()
    {
        if (!isEditing)
        {
            // Войти в режим редактирования: очистить поля
            SetEditMode(true);
            if (level1Input) level1Input.text = "";
            if (level2Input) level2Input.text = "";
            if (restInput) restInput.text = "";
            if (chanceInput) chanceInput.text = "";
            if (level1Input) level1Input.ActivateInputField();
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

    // ---------- Internals ----------
    private void SetEditMode(bool edit)
    {
        isEditing = edit;

        // Безопасные переключатели (null-checks)
        SetActiveSafe(level1Text, !edit);
        SetActiveSafe(level2Text, !edit);
        SetActiveSafe(restText, !edit);
        SetActiveSafe(chanceText, !edit);

        SetActiveSafe(level1Input, edit);
        SetActiveSafe(level2Input, edit);
        SetActiveSafe(restInput, edit);
        SetActiveSafe(chanceInput, edit);

        // Подпись на кнопке
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

        // Если поле пустое — сохраняем прежнее значение
        if (level1Input && !string.IsNullOrWhiteSpace(level1Input.text))
        {
            if (!int.TryParse(level1Input.text.Trim(), NumberStyles.Integer, inv, out l1) &&
                !int.TryParse(level1Input.text.Trim(), NumberStyles.Integer, cur, out l1))
            { Debug.LogWarning("Level 1: введите целое число секунд"); return false; }
            if (l1 <= 0) { Debug.LogWarning("Level 1 должно быть > 0"); return false; }
        }

        if (level2Input && !string.IsNullOrWhiteSpace(level2Input.text))
        {
            if (!int.TryParse(level2Input.text.Trim(), NumberStyles.Integer, inv, out l2) &&
                !int.TryParse(level2Input.text.Trim(), NumberStyles.Integer, cur, out l2))
            { Debug.LogWarning("Level 2: введите целое число секунд"); return false; }
            if (l2 <= 0) { Debug.LogWarning("Level 2 должно быть > 0"); return false; }
        }

        if (restInput && !string.IsNullOrWhiteSpace(restInput.text))
        {
            if (!int.TryParse(restInput.text.Trim(), NumberStyles.Integer, inv, out rt) &&
                !int.TryParse(restInput.text.Trim(), NumberStyles.Integer, cur, out rt))
            { Debug.LogWarning("Rest time: введите целое число секунд"); return false; }
            if (rt < 0) { Debug.LogWarning("Rest time не может быть отрицательным"); return false; }
        }

        if (chanceInput && !string.IsNullOrWhiteSpace(chanceInput.text))
        {
            var txt = chanceInput.text.Trim().Replace(',', '.'); // 0,35 → 0.35
            if (!float.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out ch))
            { Debug.LogWarning("Red chance: введите число 0..1"); return false; }
            ch = Mathf.Clamp01(ch);
        }

        // Применяем и уведомляем остальной UI
        p.settings.level1DurationSec = l1;
        p.settings.level2DurationSec = l2;
        p.settings.restTimeSec = rt;
        p.settings.redChance = ch;

        pm.SelectByIndex(pm.selectedIndex); // триггернём перерисовку слушателей
        return true;
    }

    private static void SetActiveSafe(Component c, bool active)
    {
        if (c != null && c.gameObject.activeSelf != active)
            c.gameObject.SetActive(active);
    }

    // ---------- Auto-bind helpers ----------
    [ContextMenu("Auto Bind (find children)")]
    private void TryAutoBind()
    {
        // Тексты
        var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        selectedPatientLabel ??= tmps.FirstOrDefault(t => t.name.ToLower().Contains("selected"));
        level1Text ??= tmps.FirstOrDefault(t => t.name.ToLower().Contains("level1") || t.name.ToLower().Contains("l1"));
        level2Text ??= tmps.FirstOrDefault(t => t.name.ToLower().Contains("level2") || t.name.ToLower().Contains("l2"));
        restText ??= tmps.FirstOrDefault(t => t.name.ToLower().Contains("rest"));
        chanceText ??= tmps.FirstOrDefault(t => t.name.ToLower().Contains("chance"));

        // Инпуты
        var inputs = GetComponentsInChildren<TMP_InputField>(true);
        level1Input ??= inputs.FirstOrDefault(i => i.name.ToLower().Contains("level1") || i.name.ToLower().Contains("l1"));
        level2Input ??= inputs.FirstOrDefault(i => i.name.ToLower().Contains("level2") || i.name.ToLower().Contains("l2"));
        restInput ??= inputs.FirstOrDefault(i => i.name.ToLower().Contains("rest"));
        chanceInput ??= inputs.FirstOrDefault(i => i.name.ToLower().Contains("chance"));

        // Кнопка и её лейбл
        changeButton ??= GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name.ToLower().Contains("change"));
        if (!changeButtonLabel && changeButton)
            changeButtonLabel = changeButton.GetComponentInChildren<TextMeshProUGUI>(true);
    }
}
