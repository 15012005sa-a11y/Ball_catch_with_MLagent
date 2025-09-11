using System.Globalization;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PreferencesPanel : MonoBehaviour
{
    [Header("Selected label (optional)")]
    public TMP_Text selectedPatientLabel;

    // ---------- View mode (read-only) ----------
    [Header("Read-only labels (view mode)")]
    public TMP_Text VAL_Level1;
    public TMP_Text VAL_Level2;
    public TMP_Text VAL_Rest;
    public TMP_Text VAL_Chance;

    public TMP_Text VAL_SpawnInterval;   // NEW
    public TMP_Text VAL_BallSpeed;       // NEW
    public TMP_Text VAL_SpeedInc;        // NEW
    public TMP_Text VAL_SpeedDec;        // NEW

    // ---------- Edit mode (inputs) ----------
    [Header("Inputs (edit mode)")]
    public TMP_InputField IF_Level1;
    public TMP_InputField IF_Level2;
    public TMP_InputField IF_Rest;
    public TMP_InputField IF_Chance;

    public TMP_InputField IF_SpawnInterval; // NEW
    public TMP_InputField IF_BallSpeed;     // NEW
    public TMP_InputField IF_SpeedInc;      // NEW
    public TMP_InputField IF_SpeedDec;      // NEW

    // ---------- Button ----------
    [Header("Change/Save button")]
    public Button Change_button;
    public TMP_Text Change_buttonLabel;

    private bool editMode; // вспомогательный флаг, но решение теперь опирается на видимость инпутов
    private static readonly CultureInfo INV = CultureInfo.InvariantCulture;

    // алиасы имён (новые/наследие)
    private static readonly string[] N_L1 = { "level1DurationSec", "Level1Duration" };
    private static readonly string[] N_L2 = { "level2DurationSec", "Level2Duration" };
    private static readonly string[] N_REST = { "restTimeSec", "RestSeconds" };
    private static readonly string[] N_CH = { "redChance", "RedChance" };
    private static readonly string[] N_SPA = { "spawnInterval", "SpawnInterval" };
    private static readonly string[] N_SPD = { "ballSpeed", "BallSpeed" };
    private static readonly string[] N_INC = { "speedIncreaseFactor", "SpeedIncreaseFactor" };
    private static readonly string[] N_DEC = { "speedDecreaseFactor", "SpeedDecreaseFactor" };

    // ---------------- Unity ----------------
    private void Awake()
    {
        AutoBindByPrefix();
    }

    private bool _wired;
    private void OnEnable()
    {
        AutoBindByPrefix();
        if (Change_button && !_wired)
        {
            Change_button.onClick.AddListener(OnChangeClicked);
            _wired = true;
        }
    }


    private void OnDisable()
    {
        var pm = PatientManager.Instance;
        if (pm != null) pm.OnSelectedPatientChanged -= Render;
    }

    // ---------------- Binding helpers ----------------
    [ContextMenu("AutoBind (by prefixes)")]
    private void AutoBindByPrefix()
    {
        var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        VAL_Level1 ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", System.StringComparison.OrdinalIgnoreCase) && t.name.ToLower().Contains("level1"));
        VAL_Level2 ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", System.StringComparison.OrdinalIgnoreCase) && t.name.ToLower().Contains("level2"));
        VAL_Rest ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", System.StringComparison.OrdinalIgnoreCase) && t.name.ToLower().Contains("rest"));
        VAL_Chance ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", System.StringComparison.OrdinalIgnoreCase) && t.name.ToLower().Contains("chance"));

        VAL_SpawnInterval ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", System.StringComparison.OrdinalIgnoreCase) && t.name.ToLower().Contains("spawn"));
        VAL_BallSpeed ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", System.StringComparison.OrdinalIgnoreCase) && t.name.ToLower().Contains("ball"));
        VAL_SpeedInc ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", System.StringComparison.OrdinalIgnoreCase) && (t.name.ToLower().Contains("inc") || t.name.ToLower().Contains("increase")));
        VAL_SpeedDec ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", System.StringComparison.OrdinalIgnoreCase) && (t.name.ToLower().Contains("dec") || t.name.ToLower().Contains("decrease")));

        var inputs = GetComponentsInChildren<TMP_InputField>(true);
        IF_Level1 ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", System.StringComparison.OrdinalIgnoreCase) && i.name.ToLower().Contains("level1"));
        IF_Level2 ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", System.StringComparison.OrdinalIgnoreCase) && i.name.ToLower().Contains("level2"));
        IF_Rest ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", System.StringComparison.OrdinalIgnoreCase) && i.name.ToLower().Contains("rest"));
        IF_Chance ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", System.StringComparison.OrdinalIgnoreCase) && i.name.ToLower().Contains("chance"));

        IF_SpawnInterval ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", System.StringComparison.OrdinalIgnoreCase) && i.name.ToLower().Contains("spawn"));
        IF_BallSpeed ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", System.StringComparison.OrdinalIgnoreCase) && i.name.ToLower().Contains("ball"));
        IF_SpeedInc ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", System.StringComparison.OrdinalIgnoreCase) && (i.name.ToLower().Contains("inc") || i.name.ToLower().Contains("increase")));
        IF_SpeedDec ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", System.StringComparison.OrdinalIgnoreCase) && (i.name.ToLower().Contains("dec") || i.name.ToLower().Contains("decrease")));

        Change_button ??= GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name.ToLower().Contains("change"));
        if (!Change_buttonLabel && Change_button)
            Change_buttonLabel = Change_button.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    // ---------------- Public API ----------------
    public void Render(Patient p)
    {
        if (p == null || p.settings == null) return;
        var s = p.settings;
        InvokeIfExists(s, "EnsureDefaults");

        if (selectedPatientLabel) selectedPatientLabel.text = $"selected: {p.displayName}";

        if (VAL_Level1) VAL_Level1.text = GetInt(s, N_L1, 180) + " s";
        if (VAL_Level2) VAL_Level2.text = GetInt(s, N_L2, 120) + " s";
        if (VAL_Rest) VAL_Rest.text = GetInt(s, N_REST, 60) + " s";
        if (VAL_Chance) VAL_Chance.text = GetFloat(s, N_CH, 0.35f).ToString("0.##", INV);

        if (VAL_SpawnInterval) VAL_SpawnInterval.text = GetFloat(s, N_SPA, 1.5f).ToString("0.##", INV) + " s";
        if (VAL_BallSpeed) VAL_BallSpeed.text = GetFloat(s, N_SPD, 1.0f).ToString("0.##", INV);
        if (VAL_SpeedInc) VAL_SpeedInc.text = "×" + GetFloat(s, N_INC, 1.1f).ToString("0.##", INV);
        if (VAL_SpeedDec) VAL_SpeedDec.text = "×" + GetFloat(s, N_DEC, 0.6f).ToString("0.##", INV);
    }

    // === Сервис: гарантируем, что ссылки на IF_/VAL_ подхвачены, даже если объекты были Inactive
    // --- Жёсткий бинд: подтянуть ссылки даже у неактивных объектов
    private void ForceBindIfNeeded()
    {
        AutoBindByPrefix(); // он ищет и в Inactive
    }

    // --- Включение / выключение видимости с логом
    private void ToggleEditUI(bool showInputs)
    {
        ForceBindIfNeeded();

        // Read-only значения
        SetActiveSafe(VAL_Level1, !showInputs);
        SetActiveSafe(VAL_Level2, !showInputs);
        SetActiveSafe(VAL_Rest, !showInputs);
        SetActiveSafe(VAL_Chance, !showInputs);
        SetActiveSafe(VAL_SpawnInterval, !showInputs);
        SetActiveSafe(VAL_BallSpeed, !showInputs);
        SetActiveSafe(VAL_SpeedInc, !showInputs);
        SetActiveSafe(VAL_SpeedDec, !showInputs);

        // Инпуты
        SetActiveSafe(IF_Level1, showInputs);
        SetActiveSafe(IF_Level2, showInputs);
        SetActiveSafe(IF_Rest, showInputs);
        SetActiveSafe(IF_Chance, showInputs);
        SetActiveSafe(IF_SpawnInterval, showInputs);
        SetActiveSafe(IF_BallSpeed, showInputs);
        SetActiveSafe(IF_SpeedInc, showInputs);
        SetActiveSafe(IF_SpeedDec, showInputs);

        if (Change_buttonLabel) Change_buttonLabel.text = showInputs ? "Save" : "Change";

        editMode = showInputs;
        Debug.Log($"[PreferencesPanel] UI -> {(showInputs ? "EDIT" : "VIEW")} mode");
    }

    // --- КНОПКА Change/Save
    private int _lastClickFrame = -1;

    public void OnChangeClicked()
    {
        if (Time.frameCount == _lastClickFrame) return; // анти-дубль
        _lastClickFrame = Time.frameCount;

        // дальше ваш текущий код:
        var p = PatientManager.Instance?.Current;
        var s = p?.settings;
        if (s == null) return;

        if (!editMode)
        {
            InvokeIfExists(s, "EnsureDefaults");
            PrefillInputsFromSettings(s);
            ToggleEditUI(true);
            return;
        }

        if (TrySaveInputs(out var msg))
        {
            Debug.Log($"[PreferencesPanel] {msg}");
            ToggleEditUI(false);
            Render(p);
        }
    }


    // --- Заполнение инпутов текущими значениями (с проверками)
    private void PrefillInputsFromSettings(object s)
    {
        if (s == null) return;

        if (IF_Level1) IF_Level1.text = GetInt(s, N_L1, 180).ToString();
        if (IF_Level2) IF_Level2.text = GetInt(s, N_L2, 120).ToString();
        if (IF_Rest) IF_Rest.text = GetInt(s, N_REST, 60).ToString();
        if (IF_Chance) IF_Chance.text = GetFloat(s, N_CH, 0.35f).ToString(INV);

        if (IF_SpawnInterval) IF_SpawnInterval.text = GetFloat(s, N_SPA, 1.5f).ToString(INV);
        if (IF_BallSpeed) IF_BallSpeed.text = GetFloat(s, N_SPD, 1.0f).ToString(INV);
        if (IF_SpeedInc) IF_SpeedInc.text = GetFloat(s, N_INC, 1.1f).ToString(INV);
        if (IF_SpeedDec) IF_SpeedDec.text = GetFloat(s, N_DEC, 0.6f).ToString(INV);
    }

    // === Переключение режимов: показываем IF_*, скрываем VAL_* и меняем текст кнопки
    private void SetEditMode(bool on)
    {
        editMode = on;

        // Read-only
        SetActiveSafe(VAL_Level1, !on);
        SetActiveSafe(VAL_Level2, !on);
        SetActiveSafe(VAL_Rest, !on);
        SetActiveSafe(VAL_Chance, !on);
        SetActiveSafe(VAL_SpawnInterval, !on);
        SetActiveSafe(VAL_BallSpeed, !on);
        SetActiveSafe(VAL_SpeedInc, !on);
        SetActiveSafe(VAL_SpeedDec, !on);

        // Inputs
        SetActiveSafe(IF_Level1, on);
        SetActiveSafe(IF_Level2, on);
        SetActiveSafe(IF_Rest, on);
        SetActiveSafe(IF_Chance, on);
        SetActiveSafe(IF_SpawnInterval, on);
        SetActiveSafe(IF_BallSpeed, on);
        SetActiveSafe(IF_SpeedInc, on);
        SetActiveSafe(IF_SpeedDec, on);

        if (Change_buttonLabel) Change_buttonLabel.text = on ? "Save" : "Change";
    }

    private bool TrySaveInputs(out string message)
    {
        message = "";
        var p = PatientManager.Instance?.Current;
        var s = p?.settings;
        if (s == null) { message = "No selected patient/settings"; return false; }

        // ints
        if (IF_Level1 && !string.IsNullOrWhiteSpace(IF_Level1.text))
        {
            if (!int.TryParse(IF_Level1.text.Trim(), out var v) || v <= 0) { message = "Level 1 must be integer > 0"; return false; }
            SetInt(s, N_L1, Mathf.Clamp(v, 5, 3600));
        }
        if (IF_Level2 && !string.IsNullOrWhiteSpace(IF_Level2.text))
        {
            if (!int.TryParse(IF_Level2.text.Trim(), out var v) || v <= 0) { message = "Level 2 must be integer > 0"; return false; }
            SetInt(s, N_L2, Mathf.Clamp(v, 5, 3600));
        }
        if (IF_Rest && !string.IsNullOrWhiteSpace(IF_Rest.text))
        {
            if (!int.TryParse(IF_Rest.text.Trim(), out var v) || v < 0) { message = "Rest must be integer ≥ 0"; return false; }
            SetInt(s, N_REST, Mathf.Clamp(v, 0, 600));
        }

        // floats
        if (IF_Chance && !string.IsNullOrWhiteSpace(IF_Chance.text))
        {
            var t = IF_Chance.text.Trim().Replace(',', '.');
            if (!float.TryParse(t, NumberStyles.Float, INV, out var v)) { message = "Chance must be 0..1"; return false; }
            SetFloat(s, N_CH, Mathf.Clamp01(v));
        }

        if (IF_SpawnInterval && !string.IsNullOrWhiteSpace(IF_SpawnInterval.text))
        {
            var t = IF_SpawnInterval.text.Trim().Replace(',', '.');
            if (!float.TryParse(t, NumberStyles.Float, INV, out var v)) { message = "Spawn interval parse error"; return false; }
            SetFloat(s, N_SPA, Mathf.Clamp(v, 0.05f, 10f));
        }

        if (IF_BallSpeed && !string.IsNullOrWhiteSpace(IF_BallSpeed.text))
        {
            var t = IF_BallSpeed.text.Trim().Replace(',', '.');
            if (!float.TryParse(t, NumberStyles.Float, INV, out var v)) { message = "Ball speed parse error"; return false; }
            SetFloat(s, N_SPD, Mathf.Clamp(v, 0.1f, 50f));
        }

        if (IF_SpeedInc && !string.IsNullOrWhiteSpace(IF_SpeedInc.text))
        {
            var t = IF_SpeedInc.text.Trim().Replace(',', '.');
            if (!float.TryParse(t, NumberStyles.Float, INV, out var v)) { message = "Increase factor parse error"; return false; }
            SetFloat(s, N_INC, Mathf.Clamp(v, 0.5f, 3f));
        }

        if (IF_SpeedDec && !string.IsNullOrWhiteSpace(IF_SpeedDec.text))
        {
            var t = IF_SpeedDec.text.Trim().Replace(',', '.');
            if (!float.TryParse(t, NumberStyles.Float, INV, out var v)) { message = "Decrease factor parse error"; return false; }
            SetFloat(s, N_DEC, Mathf.Clamp(v, 0.1f, 1f));
        }

        message = $"Saved: L1={GetInt(s, N_L1, 0)}, L2={GetInt(s, N_L2, 0)}, Rest={GetInt(s, N_REST, 0)}, Chance={GetFloat(s, N_CH, 0):0.##}, " +
                  $"Spawn={GetFloat(s, N_SPA, 0):0.##}, Speed={GetFloat(s, N_SPD, 0):0.##}, Inc={GetFloat(s, N_INC, 0):0.##}, Dec={GetFloat(s, N_DEC, 0):0.##}";
        return true;
    }

    private static void SetActiveSafe(Component c, bool active)
    {
        if (!c) return;
        var go = c.gameObject;
        if (go.activeSelf != active) go.SetActive(active);
    }

    // ---------------- Reflection helpers ----------------
    private static BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static void InvokeIfExists(object obj, string methodName)
    {
        if (obj == null) return;
        var m = obj.GetType().GetMethod(methodName, BF, null, System.Type.EmptyTypes, null);
        if (m != null) { try { m.Invoke(obj, null); } catch { } }
    }

    private static bool TryGetMember(object o, string name, out object value)
    {
        value = null;
        if (o == null) return false;
        var t = o.GetType();

        var f = t.GetField(name, BF);
        if (f != null) { value = f.GetValue(o); return true; }

        var p = t.GetProperty(name, BF);
        if (p != null && p.CanRead) { value = p.GetValue(o, null); return true; }

        return false;
    }

    private static bool TrySetMember(object o, string name, object v)
    {
        if (o == null) return false;
        var t = o.GetType();

        var f = t.GetField(name, BF);
        if (f != null) { try { f.SetValue(o, System.Convert.ChangeType(v, f.FieldType, INV)); return true; } catch { } }

        var p = t.GetProperty(name, BF);
        if (p != null && p.CanWrite) { try { p.SetValue(o, System.Convert.ChangeType(v, p.PropertyType, INV), null); return true; } catch { } }

        return false;
    }

    private static int GetInt(object s, string[] names, int defVal)
    {
        foreach (var n in names)
            if (TryGetMember(s, n, out var v) && v != null && int.TryParse(v.ToString(), out var iv)) return iv;
        return defVal;
    }

    private static float GetFloat(object s, string[] names, float defVal)
    {
        foreach (var n in names)
            if (TryGetMember(s, n, out var v) && v != null && float.TryParse(v.ToString(), NumberStyles.Float, INV, out var fv)) return fv;
        return defVal;
    }

    private static void SetInt(object s, string[] names, int value)
    {
        foreach (var n in names) if (TrySetMember(s, n, value)) return;
    }

    private static void SetFloat(object s, string[] names, float value)
    {
        foreach (var n in names) if (TrySetMember(s, n, value)) return;
    }
}
