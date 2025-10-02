using System;                      // Convert
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.ComponentModel;

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

    public TMP_Text VAL_SpawnInterval;
    public TMP_Text VAL_BallSpeed;
    public TMP_Text VAL_SpeedInc;
    public TMP_Text VAL_SpeedDec;

    // ---------- Edit mode (inputs) ----------
    [Header("Inputs (edit mode)")]
    public TMP_InputField IF_Level1;
    public TMP_InputField IF_Level2;
    public TMP_InputField IF_Rest;
    public TMP_InputField IF_Chance;

    public TMP_InputField IF_SpawnInterval;
    public TMP_InputField IF_BallSpeed;
    public TMP_InputField IF_SpeedInc;
    public TMP_InputField IF_SpeedDec;

    // ---------- Button ----------
    [Header("Change/Save button")]
    public Button Change_button;
    public TMP_Text Change_buttonLabel;

    private bool editMode;
    private static readonly CultureInfo INV = CultureInfo.InvariantCulture;

    // Алиасы имён (поддержка старых/новых полей)
    private static readonly string[] N_L1 = { "level1DurationSec", "Level1Duration" };
    private static readonly string[] N_L2 = { "level2DurationSec", "Level2Duration" };
    private static readonly string[] N_REST = { "restTimeSec", "RestSeconds" };
    private static readonly string[] N_CH = { "redChance", "RedChance" };

    // В settings этих полей может НЕ быть — поэтому отдельно сохраним как spawner.json
    private static readonly string[] N_SPA = { "spawnInterval", "SpawnInterval" };
    private static readonly string[] N_SPD = { "ballSpeed", "BallSpeed" };
    private static readonly string[] N_INC = { "speedIncreaseFactor", "SpeedIncreaseFactor" };
    private static readonly string[] N_DEC = { "speedDecreaseFactor", "SpeedDecreaseFactor" };

    // --------- модель для spawner.json ---------
    [Serializable]
    public class SpawnerTuning
    {
        public float spawnInterval = 1.5f;
        public float ballSpeed = 1f;
        public float speedIncreaseFactor = 1.1f;
        public float speedDecreaseFactor = 0.6f;
    }

    private void Awake() => AutoBindByPrefix();

    private bool _wired;
    private void OnEnable()
    {
        AutoBindByPrefix();

        if (Change_button && !_wired)
        {
            Change_button.onClick.AddListener(OnChangeClicked);
            _wired = true;
        }

        var pm = PatientManager.Instance;
        if (pm != null)
        {
            pm.OnSelectedPatientChanged += Render;
            if (pm.Current != null) Render(pm.Current);
        }

        ToggleEditUI(false);
    }

    private void OnDisable()
    {
        var pm = PatientManager.Instance;
        if (pm != null) pm.OnSelectedPatientChanged -= Render;
    }

    // ---------- Автопривязка по имени ----------
    [ContextMenu("AutoBind (by prefixes)")]
    private void AutoBindByPrefix()
    {
        var tmps = GetComponentsInChildren<TMP_Text>(true);

        VAL_Level1 ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", StringComparison.OrdinalIgnoreCase) && t.name.ToLower().Contains("level1"));
        VAL_Level2 ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", StringComparison.OrdinalIgnoreCase) && t.name.ToLower().Contains("level2"));
        VAL_Rest ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", StringComparison.OrdinalIgnoreCase) && t.name.ToLower().Contains("rest"));
        VAL_Chance ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", StringComparison.OrdinalIgnoreCase) && t.name.ToLower().Contains("chance"));

        VAL_SpawnInterval ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", StringComparison.OrdinalIgnoreCase) && t.name.ToLower().Contains("spawn"));
        VAL_BallSpeed ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", StringComparison.OrdinalIgnoreCase) && t.name.ToLower().Contains("ball"));
        VAL_SpeedInc ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", StringComparison.OrdinalIgnoreCase) && (t.name.ToLower().Contains("inc") || t.name.ToLower().Contains("increase")));
        VAL_SpeedDec ??= tmps.FirstOrDefault(t => t.name.StartsWith("VAL_", StringComparison.OrdinalIgnoreCase) && (t.name.ToLower().Contains("dec") || t.name.ToLower().Contains("decrease")));

        var inputs = GetComponentsInChildren<TMP_InputField>(true);

        IF_Level1 ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", StringComparison.OrdinalIgnoreCase) && i.name.ToLower().Contains("level1"));
        IF_Level2 ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", StringComparison.OrdinalIgnoreCase) && i.name.ToLower().Contains("level2"));
        IF_Rest ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", StringComparison.OrdinalIgnoreCase) && i.name.ToLower().Contains("rest"));
        IF_Chance ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", StringComparison.OrdinalIgnoreCase) && i.name.ToLower().Contains("chance"));

        IF_SpawnInterval ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", StringComparison.OrdinalIgnoreCase) && i.name.ToLower().Contains("spawn"));
        IF_BallSpeed ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", StringComparison.OrdinalIgnoreCase) && i.name.ToLower().Contains("ball"));
        IF_SpeedInc ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", StringComparison.OrdinalIgnoreCase) && (i.name.ToLower().Contains("inc") || i.name.ToLower().Contains("increase")));
        IF_SpeedDec ??= inputs.FirstOrDefault(i => i.name.StartsWith("IF_", StringComparison.OrdinalIgnoreCase) && (i.name.ToLower().Contains("dec") || i.name.ToLower().Contains("decrease")));

        Change_button ??= GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name.ToLower().Contains("change"));
        if (!Change_buttonLabel && Change_button)
            Change_buttonLabel = Change_button.GetComponentInChildren<TMP_Text>(true);
    }

    // ---------- Отрисовка значений ----------
    public void Render(Patient p)
    {
        if (p == null) return;

        object s = p.settings;
        InvokeIfExists(s, "EnsureDefaults");

        if (selectedPatientLabel) selectedPatientLabel.text = $"selected: {p.displayName}";

        // ints/chance — из settings
        if (VAL_Level1) VAL_Level1.text = GetInt(s, N_L1, 180) + " s";
        if (VAL_Level2) VAL_Level2.text = GetInt(s, N_L2, 120) + " s";
        if (VAL_Rest) VAL_Rest.text = GetInt(s, N_REST, 60) + " s";
        if (VAL_Chance) VAL_Chance.text = GetFloat(s, N_CH, 0.35f).ToString("0.##", INV);

        // Тюнинг — сперва из файла, потом из компонента, потом из settings (если вдруг есть)
        int pid = PatientManager.Instance != null ? PatientManager.Instance.CurrentPatientID : -1;
        var spawner = FindObjectOfType<BallSpawnerBallCatch>(includeInactive: true);
        var tune = LoadSpawnerTuning(pid) ?? FromSpawner(spawner) ?? new SpawnerTuning();

        if (VAL_SpawnInterval) VAL_SpawnInterval.text = tune.spawnInterval.ToString("0.##", INV) + " s";
        if (VAL_BallSpeed) VAL_BallSpeed.text = tune.ballSpeed.ToString("0.##", INV);
        if (VAL_SpeedInc) VAL_SpeedInc.text = "×" + tune.speedIncreaseFactor.ToString("0.##", INV);
        if (VAL_SpeedDec) VAL_SpeedDec.text = "×" + tune.speedDecreaseFactor.ToString("0.##", INV);
    }

    private void ToggleEditUI(bool showInputs)
    {
        SetActiveSafe(VAL_Level1, !showInputs);
        SetActiveSafe(VAL_Level2, !showInputs);
        SetActiveSafe(VAL_Rest, !showInputs);
        SetActiveSafe(VAL_Chance, !showInputs);
        SetActiveSafe(VAL_SpawnInterval, !showInputs);
        SetActiveSafe(VAL_BallSpeed, !showInputs);
        SetActiveSafe(VAL_SpeedInc, !showInputs);
        SetActiveSafe(VAL_SpeedDec, !showInputs);

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
    }

    private int _lastClickFrame = -1;
    public void OnChangeClicked()
    {
        if (Time.frameCount == _lastClickFrame) return;
        _lastClickFrame = Time.frameCount;

        var p = PatientManager.Instance?.Current;
        object s = p?.settings;
        if (p == null || s == null) return;

        if (!editMode)
        {
            InvokeIfExists(s, "EnsureDefaults");
            PrefillInputsFromCurrent(p);
            ToggleEditUI(true);
            return;
        }

        if (TrySaveInputs(p, s, out var msg))
        {
            Debug.Log($"[PreferencesPanel] {msg}");

            ToggleEditUI(false);
            Render(p);
        }
    }

    private void PrefillInputsFromCurrent(Patient p)
    {
        object s = p.settings;
        if (IF_Level1) IF_Level1.text = GetInt(s, N_L1, 180).ToString();
        if (IF_Level2) IF_Level2.text = GetInt(s, N_L2, 120).ToString();
        if (IF_Rest) IF_Rest.text = GetInt(s, N_REST, 60).ToString();
        if (IF_Chance) IF_Chance.text = GetFloat(s, N_CH, 0.35f).ToString(INV);

        int pid = PatientManager.Instance != null ? PatientManager.Instance.CurrentPatientID : -1;
        var spawner = FindObjectOfType<BallSpawnerBallCatch>(true);
        var tune = LoadSpawnerTuning(pid) ?? FromSpawner(spawner) ?? new SpawnerTuning();

        if (IF_SpawnInterval) IF_SpawnInterval.text = tune.spawnInterval.ToString(INV);
        if (IF_BallSpeed) IF_BallSpeed.text = tune.ballSpeed.ToString(INV);
        if (IF_SpeedInc) IF_SpeedInc.text = tune.speedIncreaseFactor.ToString(INV);
        if (IF_SpeedDec) IF_SpeedDec.text = tune.speedDecreaseFactor.ToString(INV);
    }

    private bool TrySaveInputs(Patient p, object settings, out string message)
    {
        message = "";
        if (p == null || settings == null) { message = "No selected patient/settings"; return false; }

        // ---- 1) ints/chance в settings.json ----
        if (IF_Level1 && !string.IsNullOrWhiteSpace(IF_Level1.text))
        {
            if (!int.TryParse(IF_Level1.text.Trim(), out var v) || v <= 0) { message = "Level 1 must be integer > 0"; return false; }
            SetInt(settings, N_L1, Mathf.Clamp(v, 5, 3600));
        }
        if (IF_Level2 && !string.IsNullOrWhiteSpace(IF_Level2.text))
        {
            if (!int.TryParse(IF_Level2.text.Trim(), out var v) || v <= 0) { message = "Level 2 must be integer > 0"; return false; }
            SetInt(settings, N_L2, Mathf.Clamp(v, 5, 3600));
        }
        if (IF_Rest && !string.IsNullOrWhiteSpace(IF_Rest.text))
        {
            if (!int.TryParse(IF_Rest.text.Trim(), out var v) || v < 0) { message = "Rest must be integer ≥ 0"; return false; }
            SetInt(settings, N_REST, Mathf.Clamp(v, 0, 600));
        }
        if (IF_Chance && !string.IsNullOrWhiteSpace(IF_Chance.text))
        {
            var t = IF_Chance.text.Trim().Replace(',', '.');
            if (!float.TryParse(t, NumberStyles.Float, INV, out var v)) { message = "Chance must be 0..1"; return false; }
            SetFloat(settings, N_CH, Mathf.Clamp01(v));
        }

        // ---- 2) тюнинг в spawner.json + применить в компонент ----
        var spawner = FindObjectOfType<BallSpawnerBallCatch>(includeInactive: true);
        var tune = LoadSpawnerTuning(p.id) ?? FromSpawner(spawner) ?? new SpawnerTuning();

        if (IF_SpawnInterval && !string.IsNullOrWhiteSpace(IF_SpawnInterval.text))
        {
            if (!TryParseFloat(IF_SpawnInterval.text, out var v)) { message = "Spawn interval parse error"; return false; }
            tune.spawnInterval = Mathf.Clamp(v, 0.05f, 10f);
        }
        if (IF_BallSpeed && !string.IsNullOrWhiteSpace(IF_BallSpeed.text))
        {
            if (!TryParseFloat(IF_BallSpeed.text, out var v)) { message = "Ball speed parse error"; return false; }
            tune.ballSpeed = Mathf.Clamp(v, 0.1f, 50f);
        }
        if (IF_SpeedInc && !string.IsNullOrWhiteSpace(IF_SpeedInc.text))
        {
            if (!TryParseFloat(IF_SpeedInc.text, out var v)) { message = "Increase factor parse error"; return false; }
            tune.speedIncreaseFactor = Mathf.Clamp(v, 0.5f, 3f);
        }
        if (IF_SpeedDec && !string.IsNullOrWhiteSpace(IF_SpeedDec.text))
        {
            if (!TryParseFloat(IF_SpeedDec.text, out var v)) { message = "Decrease factor parse error"; return false; }
            tune.speedDecreaseFactor = Mathf.Clamp(v, 0.1f, 1f);
        }

        // сначала сохраним, потом применим
        SaveSpawnerTuning(p.id, tune);

        // применяем ИМЕННО тюнинг (а не patient settings)
        if (spawner) spawner.ApplySettings(tune);

        // ---- 3) сохранить на диск обе части ----
        SaveSettingsToDisk(p.id, settings);
        SaveSpawnerTuning(p.id, tune);

        // Сообщение
        message =
            $"Saved: L1={GetInt(settings, N_L1, 0)}, L2={GetInt(settings, N_L2, 0)}, Rest={GetInt(settings, N_REST, 0)}, Chance={GetFloat(settings, N_CH, 0):0.##}, " +
            $"Spawn={tune.spawnInterval:0.##}, Speed={tune.ballSpeed:0.##}, Inc={tune.speedIncreaseFactor:0.##}, Dec={tune.speedDecreaseFactor:0.##}";

        // Дадим знать ScoreManager-у, чтобы подтянул длительности/таймер
        if (ScoreManager.Instance) ScoreManager.Instance.ApplySettingsFromPatient(settings);

        return true;
    }

    // ---------- File I/O ----------
    private static string EnsureDir(int patientId)
    {
        string dir = Path.Combine(Application.persistentDataPath, "Patients", patientId.ToString());
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return dir;
    }

    private static void SaveSettingsToDisk(int patientId, object settings)
    {
        if (settings == null || patientId < 0) return;
        var mi = settings.GetType().GetMethod("EnsureDefaults", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        mi?.Invoke(settings, null);

        string json = JsonUtility.ToJson(settings, true);
        string path = Path.Combine(EnsureDir(patientId), "settings.json");
        File.WriteAllText(path, json);
        Debug.Log($"[PreferencesPanel] Settings saved → {path}");
    }

    // путь для спавнера в папке пациента (совместимость со старым форматом)
    private static string GetSpawnerPath(int patientId)
        => Path.Combine(EnsureDir(patientId), "spawner.json");

    public static void SaveSpawnerTuning(int patientId, SpawnerTuning t)
    {
        if (patientId < 0 || t == null) return;
        string path = GetSpawnerPath(patientId);
        File.WriteAllText(path, JsonUtility.ToJson(t, true));
        Debug.Log($"[PreferencesPanel] Spawner saved → {path}");
    }

    public static SpawnerTuning LoadSpawnerTuning(int patientId)
    {
        if (patientId < 0) return null;
        string path = GetSpawnerPath(patientId);
        if (!File.Exists(path)) return null;
        try { return JsonUtility.FromJson<SpawnerTuning>(File.ReadAllText(path)); }
        catch { return null; }
    }

    private static SpawnerTuning FromSpawner(BallSpawnerBallCatch sp)
    {
        if (!sp) return null;
        return new SpawnerTuning
        {
            spawnInterval = sp.spawnInterval,
            ballSpeed = sp.ballSpeed,
            speedIncreaseFactor = sp.speedIncreaseFactor,
            speedDecreaseFactor = sp.speedDecreaseFactor
        };
    }

    // ---------- Utils ----------
    private static bool TryParseFloat(string s, out float v)
    {
        if (string.IsNullOrWhiteSpace(s)) { v = 0f; return false; }
        s = s.Trim().Replace('×', ' ').Replace(',', '.');
        if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return true;
        return float.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out v);
    }

    private static void SetActiveSafe(UnityEngine.Component c, bool active)
    {
        if (!c) return;
        var go = c.gameObject;
        if (go.activeSelf != active) go.SetActive(active);
    }

    private static readonly BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static void InvokeIfExists(object obj, string methodName)
    {
        if (obj == null) return;
        var m = obj.GetType().GetMethod(methodName, BF, null, Type.EmptyTypes, null);
        if (m != null) { try { m.Invoke(obj, null); } catch { } }
    }

    private static bool TryGetMember(object o, string name, out object value)
    {
        value = null; if (o == null) return false; var t = o.GetType();
        var f = t.GetField(name, BF); if (f != null) { value = f.GetValue(o); return true; }
        var p = t.GetProperty(name, BF); if (p != null && p.CanRead) { value = p.GetValue(o, null); return true; }
        return false;
    }

    private static bool TrySetMember(object o, string name, object v)
    {
        if (o == null) return false; var t = o.GetType();
        var f = t.GetField(name, BF);
        if (f != null)
        {
            try { f.SetValue(o, Convert.ChangeType(v, f.FieldType, INV)); return true; } catch { }
        }
        var p = t.GetProperty(name, BF);
        if (p != null && p.CanWrite)
        {
            try { p.SetValue(o, Convert.ChangeType(v, p.PropertyType, INV), null); return true; } catch { }
        }
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
        {
            if (TryGetMember(s, n, out var v) && v != null)
            {
                // пробуем напрямую
                try { return Convert.ToSingle(v, CultureInfo.CurrentCulture); } catch { }
                // а затем строкой
                var str = v.ToString();
                if (TryParseFloat(str, out var f)) return f;
            }
        }
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
