using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public struct MotionRecord
{
    public double Timestamp;
    public string JointName;
    public Vector3 Position;

    public MotionRecord(double timestamp, string jointName, Vector3 position)
    {
        Timestamp = timestamp;
        JointName = jointName;
        Position = position;
    }
}

public class MotionLogger : MonoBehaviour
{
    private KinectManager kinectManager;
    private List<MotionRecord> _records;
    private float _sessionStartTime;
    private bool _isLogging;

    [Header("Arm Angle Tracking")]
    public bool trackArmAngles = true;
    public List<float> leftArmAngles = new List<float>();
    public List<float> rightArmAngles = new List<float>();

    [Header("Saving")]
    public bool saveRawCsv = false;            // <-- добавьте переключатель
    public string filePrefix = "Motion_";

    // Суставы, которые мы логируем
    private KinectInterop.JointType[] jointsToTrack = new[]
    {
        KinectInterop.JointType.HandLeft,
        KinectInterop.JointType.HandRight,
        KinectInterop.JointType.FootLeft,
        KinectInterop.JointType.FootRight,
        KinectInterop.JointType.Head
    };

    [Header("Diagnostics")]
    [Tooltip("Включайте только когда нужно отладить. В обычной игре держите OFF.")]
    public bool verboseLogs = false;

    /// <summary>
    /// Безопасный логгер: в билдах ничего не пишет, а в редакторе — только если verboseLogs = true
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void VLog(string msg)
    {
        if (verboseLogs) Debug.Log(msg);
    }

    private void Start()
    {
        // Получаем инстанс уже после того, как KinectManager себя зарегистрировал
        kinectManager = KinectManager.Instance;
        _records = new List<MotionRecord>();
    }

    public void StartLogging()
    {
        VLog("[MotionLogger] *** StartLogging()");
        _records?.Clear();
        leftArmAngles?.Clear();
        rightArmAngles?.Clear();
        _sessionStartTime = Time.time;
        _isLogging = true;
    }

    public void StopLogging(string fileName)
    {
        VLog("[MotionLogger] *** StopLogging()");
        _isLogging = false;

        if (saveRawCsv)
            SaveToCsv(fileName);   // ваш существующий метод
        else
            _records?.Clear();
    }


    // Важно: используем LateUpdate, чтобы к этому моменту KinectManager уже заполнил свои данные
    private void LateUpdate()
    {
        // раньше здесь было многократное Debug.Log() каждый кадр.
        if (!_isLogging)
            return;

        if (kinectManager == null || !kinectManager.IsInitialized())
        {
            // редкий лог раз в секунду и только при verbose
            if (verboseLogs && (Time.frameCount % 60 == 0))
                Debug.Log("[MotionLogger] KinectManager не готов");
            return;
        }

        long userId = kinectManager.GetPrimaryUserID();
        if (userId <= 0)
        {
            // редкий лог раз в секунду и только при verbose
            if (verboseLogs && (Time.frameCount % 60 == 0))
                Debug.Log("[MotionLogger] Нет пользователя");
            return;
        }

        double t = Time.time - _sessionStartTime;

        // ===== ваша логика сбора точек/суставов/записей =====
        // Пример: если у вас есть массив jointTypes с нужными суставами:
        // foreach (var jt in jointTypes) { ... _records.Add(...); }
        // ================================================

        if (trackArmAngles)
        {
            // Левая рука
            if (kinectManager.IsJointTracked(userId, (int)KinectInterop.JointType.ShoulderLeft) &&
                kinectManager.IsJointTracked(userId, (int)KinectInterop.JointType.HandLeft))
            {
                Vector3 s = kinectManager.GetJointPosition(userId, (int)KinectInterop.JointType.ShoulderLeft);
                Vector3 h = kinectManager.GetJointPosition(userId, (int)KinectInterop.JointType.HandLeft);
                float angle = Vector3.Angle(h - s, Vector3.up);
                leftArmAngles.Add(angle);

                // не спамим — только при verbose и разреженно
                if (verboseLogs && (Time.frameCount % 30 == 0))
                    Debug.Log($"[MotionLogger] Left angle: {angle:F1}");
            }

            // Правая рука
            if (kinectManager.IsJointTracked(userId, (int)KinectInterop.JointType.ShoulderRight) &&
                kinectManager.IsJointTracked(userId, (int)KinectInterop.JointType.HandRight))
            {
                Vector3 s = kinectManager.GetJointPosition(userId, (int)KinectInterop.JointType.ShoulderRight);
                Vector3 h = kinectManager.GetJointPosition(userId, (int)KinectInterop.JointType.HandRight);
                float angle = Vector3.Angle(h - s, Vector3.up);
                rightArmAngles.Add(angle);

                if (verboseLogs && (Time.frameCount % 30 == 0))
                    Debug.Log($"[MotionLogger] Right angle: {angle:F1}");
            }
        }
    }


    private void SaveToCsv(string fileName)
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, fileName + ".csv");
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine("timestamp,jointName,posX,posY,posZ");
                foreach (var r in _records)
                    writer.WriteLine($"{r.Timestamp:F3},{r.JointName},{r.Position.x:F3},{r.Position.y:F3},{r.Position.z:F3}");
            }
            Debug.Log($"[MotionLogger] CSV saved: {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MotionLogger] Не удалось сохранить CSV: {ex}");
        }
    }
}
