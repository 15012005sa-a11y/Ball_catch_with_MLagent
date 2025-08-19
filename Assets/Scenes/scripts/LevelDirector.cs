using UnityEngine;

public class LevelDirector : MonoBehaviour
{
    [Header("Refs")]
    public BallSpawnerBallCatch spawner;  // перетащите сюда ваш спавнер
    public ScoreManager score;            // перетащите ваш ScoreManager (тот, что DontDestroyOnLoad)

    [Header("Level 2 visuals")]
    public Material blueMaterial;
    public Material redMaterial;
    [Range(0f, 1f)] public float redChance = 0.35f;

    [Header("Durations (sec)")]
    public float level1Duration = 60f;
    public float level2Duration = 60f;

    [Header("UI")]
    public GameObject graphButton;

    int currentLevel = 0;

    void Awake()
    {
        if (!spawner) spawner = FindObjectOfType<BallSpawnerBallCatch>();
        if (!score) score = FindObjectOfType<ScoreManager>();
    }

    void OnEnable()
    {
        // надёжная подписка на завершение сессии
        if (score) score.OnSessionFinished.AddListener(OnSessionFinished);
    }
    void OnDisable()
    {
        if (score) score.OnSessionFinished.RemoveListener(OnSessionFinished);
    }

    // --- публичные методы для кнопок/старта ---
    // Привяжите ЭТО к кнопке "Начать игру" вместо прямого ScoreManager.StartSession
    public void StartLevel1()
    {
        currentLevel = 1;

        // настройки уровня 1: без цветов
        if (spawner)
        {
            spawner.useColors = false;
            // остальные поля спавнера — как у вас по умолчанию
        }

        if (score)
        {
            score.SetShowStartButton(false);
            score.SetShowGraphButton(false);
            score.sessionDuration = level1Duration;
            score.StartSession(); // это спрятет меню само
        }
    }

    // Можно вызывать вручную из инспектора, если нужно протестировать сразу 2-й уровень
    public void StartLevel2()
    {
        currentLevel = 2;

        if (spawner)
        {
            spawner.useColors = true;
            spawner.redChance = redChance;
            spawner.blueMaterial = blueMaterial;
            spawner.redMaterial = redMaterial;
        }

        if (score)
        {
            score.SetShowStartButton(false);
            score.SetShowGraphButton(false);
            score.sessionDuration = level2Duration;
            score.StartSession();
        }
    }

    // --- обработчик завершения сессии ---
    void OnSessionFinished()
    {
        if (currentLevel == 1)
        {
            // сразу запускаем 2-й уровень
            StartLevel2();
        }
        else
        {
            score.SetShowStartButton(true);
            if (score) score.SetShowGraphButton(true);
            // оба уровня пройдены — можно показать финальное меню/диалог
            Debug.Log("[LevelDirector] Все уровни завершены.");
        }
    }
}
