// Put this script in AppShell scene and bind it to the "Start game" Button.
// It guarantees a clean launch of the game scene.

using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-10)]
public class StartGameFromAppShell : MonoBehaviour
{
    [Header("Target scene")] public string gameSceneName = "SampleScene"; // ← ИМЯ вашей игровой сцены

    [Header("(Optional) Save settings before load")]
    public MonoBehaviour preferencesPanel;   // сюда можно перетащить PreferencesPanel, если хотите вызвать её Save перед стартом
    public bool callSaveOnPanel = true;

    // Названия методов, которые пытаемся вызвать у панели, если они существуют
    private static readonly string[] _possibleSaveMethods = {
        "Save", "Apply", "ApplyAndSave", "SaveConfig", "OnStartGameClicked"
    };

    [Header("Safety")] public bool ensureSingleEventSystem = true;

    public void StartGame()
    {
        Debug.Log("[AppShell] StartGameFromAppShell.StartGame()");

        Time.timeScale = 1f; // на всякий случай

        // Попробуем сохранить настройки на панели (если задана)
        if (callSaveOnPanel && preferencesPanel)
        {
            TryCallSave(preferencesPanel);
        }

        // Если старый ScoreManager вдруг остался в DontDestroyOnLoad — уничтожим
        if (ScoreManager.Instance && ScoreManager.Instance.gameObject.scene.buildIndex == -1)
        {
            Debug.Log("[AppShell] Destroy stale ScoreManager from DDOL");
            Destroy(ScoreManager.Instance.gameObject);
        }

        // Чистим лишние EventSystem в текущей сцене (иногда из-за дубликатов клики пропадают)
        if (ensureSingleEventSystem)
        {
            var ev = FindObjectsOfType<EventSystem>();
            for (int i = 1; i < ev.Length; i++) Destroy(ev[i].gameObject);
        }

        // Стартуем игру
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError("[AppShell] gameSceneName is empty — укажите имя игровой сцены в инспекторе.");
            return;
        }

        if (!ApplicationCanLoad(gameSceneName))
            Debug.LogWarning($"[AppShell] Scene '{gameSceneName}' не найдена в Build Settings — добавьте её в Scenes In Build.");

        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private void TryCallSave(MonoBehaviour panel)
    {
        var t = panel.GetType();
        foreach (var name in _possibleSaveMethods)
        {
            var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null && m.GetParameters().Length == 0)
            {
                try { Debug.Log($"[AppShell] Call {t.Name}.{name}() before load"); m.Invoke(panel, null); }
                catch (Exception e) { Debug.LogWarning($"[AppShell] Save call failed: {e.Message}"); }
                return;
            }
        }
    }

    private static bool ApplicationCanLoad(string scene)
    {
        // Проверка: есть ли сцена в Build Settings
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string p = SceneUtility.GetScenePathByBuildIndex(i);
            string n = System.IO.Path.GetFileNameWithoutExtension(p);
            if (string.Equals(n, scene, StringComparison.Ordinal)) return true;
        }
        return false;
    }
}
