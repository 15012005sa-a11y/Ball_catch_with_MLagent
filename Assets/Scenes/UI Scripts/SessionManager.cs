using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

[Serializable]
public class SessionConfig
{
    public int level1Sec = 60;
    public int level2Sec = 40;
    [Range(0, 1)] public float redChance = 0.35f;
    public int restSec = 10;
    public bool stopKinectOnGameEnd = true;
    public bool stopBetweenLevels = false;
}

public class SessionManager : MonoBehaviour
{
    public static SessionManager I;

    [Header("Current selection")]
    public int currentPatientId = -1;
    public string currentPatientName = "Пациент 1";
    public SessionConfig current = new SessionConfig();

    [Header("Optional manual links (drag if you want)")]
    public MonoBehaviour levelDirector;   // перетащи сюда свой LevelDirector (необязательно)
    public MonoBehaviour scoreManager;    // перетащи сюда свой ScoreManager (необязательно)

    void Awake()
    {
        if (I == null) { I = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (I == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // === Вызывай из UI «Новая сессия» ===
    public void SetLevel1(float sec) { current.level1Sec = Mathf.RoundToInt(sec); }
    public void SetLevel2(float sec) { current.level2Sec = Mathf.RoundToInt(sec); }
    public void SetRest(float sec) { current.restSec = Mathf.RoundToInt(sec); }
    public void SetRedChance(float p) { current.redChance = Mathf.Clamp01(p); }
    public void SetStopKinectEnd(bool v) { current.stopKinectOnGameEnd = v; }
    public void SetStopBetweenLvls(bool v) { current.stopBetweenLevels = v; }

    // === Автоподключение после загрузки GameScene ===
    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        if (s.name != "GameScene") return;

        // Если ссылки не проставлены в инспекторе — найдём первые попавшиеся по имени класса
        if (levelDirector == null) levelDirector = FindByClassName("LevelDirector");
        if (scoreManager == null) scoreManager = FindByClassName("ScoreManager");

        ApplyConfigToLevelDirector();
        TrySubscribeSessionFinished();
        TryStartGameplay();
    }

    // Поищем компонент по имени класса (без namespace)
    MonoBehaviour FindByClassName(string className)
    {
        foreach (var mb in GameObject.FindObjectsOfType<MonoBehaviour>(true))
        {
            if (mb != null && mb.GetType().Name == className) return mb;
        }
        return null;
    }

    void ApplyConfigToLevelDirector()
    {
        if (levelDirector == null) return;
        var t = levelDirector.GetType();

        // Поддерживаем и int, и float поля
        TrySetNumber(t, levelDirector, "level1Duration", current.level1Sec);
        TrySetNumber(t, levelDirector, "Level1Duration", current.level1Sec);
        TrySetNumber(t, levelDirector, "level2Duration", current.level2Sec);
        TrySetNumber(t, levelDirector, "Level2Duration", current.level2Sec);
        TrySetNumber(t, levelDirector, "redChance", current.redChance);
        TrySetNumber(t, levelDirector, "RedChance", current.redChance);

        // разные варианты имени «Rest между уровнями»
        if (!TrySetNumber(t, levelDirector, "restBetweenLevels", current.restSec))
            if (!TrySetNumber(t, levelDirector, "restBetweenLevelsSec", current.restSec))
                TrySetNumber(t, levelDirector, "RestBetweenLevelsS", current.restSec);

        TrySetBool(t, levelDirector, "stopKinectOnGameEnd", current.stopKinectOnGameEnd);
        TrySetBool(t, levelDirector, "StopKinectOnGameEnd", current.stopKinectOnGameEnd);
        TrySetBool(t, levelDirector, "stopBetweenLevels", current.stopBetweenLevels);
        TrySetBool(t, levelDirector, "StopBetweenLevels", current.stopBetweenLevels);

        Debug.Log("[SessionManager] Config applied (where fields matched).");
    }

    void TrySubscribeSessionFinished()
    {
        if (scoreManager == null) return;
        var t = scoreManager.GetType();
        var f = t.GetField("OnSessionFinished", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.GetValue(scoreManager) is UnityEvent ev)
        {
            ev.AddListener(OnSessionFinished);
            Debug.Log("[SessionManager] Subscribed to ScoreManager.OnSessionFinished");
        }
        else
        {
            Debug.Log("[SessionManager] UnityEvent OnSessionFinished not found (ok). " +
                      "Call SessionManager.I.OnSessionFinished() manually on game end.");
        }
    }

    void TryStartGameplay()
    {
        if (InvokeIfExists(levelDirector, "BeginWithConfig", current)) return;  // рекомендуемый путь
        if (InvokeIfExists(levelDirector, "Begin")) return;
        if (InvokeIfExists(levelDirector, "StartLevel1")) return;
        if (InvokeIfExists(scoreManager, "StartSessionKeepScore")) return;
        if (InvokeIfExists(scoreManager, "StartSession")) return;

        Debug.Log("[SessionManager] No start method found. Start manually or add public BeginWithConfig(SessionConfig).");
    }

    public void OnSessionFinished()
    {
        Debug.Log("[SessionManager] Session finished. Returning to AppShell.");
        if (AppManager.I != null) AppManager.I.GoHome();
    }

    // -------- Helpers (без неоднозначных сигнатур) --------

    bool InvokeIfExists(object target, string method, params object[] args)
    {
        if (target == null) return false;
        var m = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m == null) return false;
        m.Invoke(target, args);
        Debug.Log($"[SessionManager] Called {target.GetType().Name}.{method}()");
        return true;
    }

    bool TrySetNumber(Type t, object o, string fieldName, double value)
    {
        var f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null) return false;
        try
        {
            if (f.FieldType == typeof(int)) f.SetValue(o, Convert.ToInt32(Math.Round(value)));
            else if (f.FieldType == typeof(float)) f.SetValue(o, Convert.ToSingle(value));
            else return false;
            return true;
        }
        catch { return false; }
    }

    bool TrySetBool(Type t, object o, string fieldName, bool value)
    {
        var f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null || f.FieldType != typeof(bool)) return false;
        try { f.SetValue(o, value); return true; } catch { return false; }
    }
}
