using UnityEngine;
using System.Collections.Generic;

// Этот скрипт вешается на объект, который создает шары
public class BallSpawner : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private float minSpeed = 2.0f;
    [SerializeField] private float maxSpeed = 10.0f;
    
    // Текущая сложность (от 0 до 1), которой управляет Агент
    private float currentDifficultyFactor = 0.5f;

    [Header("Statistics for Agent")]
    // Окно истории для сглаживания данных (последние 20 бросков)
    private Queue<bool> hitHistory = new Queue<bool>(); 
    private int historySize = 20;

    // Свойство для получения текущей скорости шаров
    public float CurrentBallSpeed 
    {
        get { return Mathf.Lerp(minSpeed, maxSpeed, currentDifficultyFactor); }
    }

    // Метод, который вызывает Агент для изменения сложности
    // actionValue: -1 (уменьшить), 0 (ничего), +1 (увеличить) или непрерывное значение
    public void UpdateDifficulty(float actionValue)
    {
        // Плавно меняем сложность. 
        // Если actionValue отрицательное -> сложность падает.
        // Time.deltaTime обеспечивает плавность.
        currentDifficultyFactor += actionValue * Time.deltaTime * 0.5f;
        currentDifficultyFactor = Mathf.Clamp01(currentDifficultyFactor);
    }

    // Вызывать этот метод, когда пациент поймал (true) или уронил (false) шар
    public void RegisterResult(bool isCatch)
    {
        if (hitHistory.Count >= historySize) hitHistory.Dequeue();
        hitHistory.Enqueue(isCatch);
    }

    // Вычисляем Success Rate (0.0 - 1.0)
    public float GetSuccessRate()
    {
        if (hitHistory.Count == 0) return 1.0f; // По умолчанию считаем, что все ок

        int catches = 0;
        foreach (bool hit in hitHistory)
        {
            if (hit) catches++;
        }
        return (float)catches / hitHistory.Count;
    }
    
    public float GetCurrentDifficulty()
    {
        return currentDifficultyFactor;
    }
}
