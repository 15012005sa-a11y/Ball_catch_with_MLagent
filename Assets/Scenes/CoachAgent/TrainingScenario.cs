using UnityEngine;

/// <summary>
/// Имитирует поведение разных пациентов или усталость одного пациента.
/// Меняет настройки PlayerSimulatorLite каждые N секунд.
/// </summary>
public class TrainingScenario : MonoBehaviour
{
    public PlayerSimulatorLite simulator;
    
    private float timer = 0;
    private float changeInterval = 30f; // Каждые 30 секунд меняем "пациента"

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > changeInterval)
        {
            timer = 0;
            RandomizePatientProfile();
        }
    }

    void RandomizePatientProfile()
    {
        // Случайно выбираем тип пациента
        int patientType = Random.Range(0, 3);

        switch (patientType)
        {
            case 0: // "Спортсмен" (Быстрый, точный)
                SetParams(speed: 8.0f, reaction: 0.15f, clumsy: 0.0f);
                Debug.Log("Training: New Patient - ATHLETE");
                break;
            case 1: // "Обычный" (Средний)
                SetParams(speed: 4.0f, reaction: 0.4f, clumsy: 0.1f);
                Debug.Log("Training: New Patient - NORMAL");
                break;
            case 2: // "Постинсультный / Усталый" (Медленный, плохая реакция)
                SetParams(speed: 2.0f, reaction: 0.8f, clumsy: 0.3f);
                Debug.Log("Training: New Patient - REHAB");
                break;
        }
    }

    void SetParams(float speed, float reaction, float clumsy)
    {
        // Используем рефлексию или сделайте поля public, если не хотите менять модификаторы доступа
        // Но лучше просто сделать поля в PlayerSimulatorLite public.
        
        // Пример (если поля public):
        /*
        simulator.handSpeed = speed;
        simulator.reactionTime = reaction;
        simulator.clumsyProbability = clumsy;
        */
        
        // Для демонстрации через SendMessage или прямой доступ, если измените поля на public
        var type = typeof(PlayerSimulatorLite);
        type.GetField("handSpeed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            ?.SetValue(simulator, speed);
        type.GetField("reactionTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            ?.SetValue(simulator, reaction);
        type.GetField("clumsyProbability", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            ?.SetValue(simulator, clumsy);
    }
}
