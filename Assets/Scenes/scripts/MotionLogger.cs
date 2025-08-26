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

    private void Start()
    {
        // Получаем инстанс уже после того, как KinectManager себя зарегистрировал
        kinectManager = KinectManager.Instance;
        _records = new List<MotionRecord>();
    }

    public void StartLogging()
    {
        Debug.Log("[MotionLogger] *** StartLogging()");
        _records.Clear();
        leftArmAngles.Clear();
        rightArmAngles.Clear();
        _sessionStartTime = Time.time;
        _isLogging = true;
    }

    public void StopLogging(string fileName)
    {
        Debug.Log("[MotionLogger] *** StopLogging()");
        _isLogging = false;
        if (saveRawCsv)                         // <-- сохраняем только при true
            SaveToCsv(fileName);
        else
            _records.Clear();
    }

    // Важно: используем LateUpdate, чтобы к этому моменту KinectManager уже заполнил свои данные
    private void LateUpdate()
    {
        if (!_isLogging)
        {
            Debug.Log("[MotionLogger] _isLogging == false, пропускаем кадр");
            return;
        }

        if (kinectManager == null || !kinectManager.IsInitialized())
        {
            Debug.Log("[MotionLogger] KinectManager не готов");
            return;
        }

        long userId = kinectManager.GetPrimaryUserID();
        if (userId <= 0)
        {
            Debug.Log("[MotionLogger] Нет пользователя, пропускаем кадр");
            return;
        }

        double t = Time.time - _sessionStartTime;

        // Собственно лог позиций
        foreach (var jt in jointsToTrack)
        {
            if (!kinectManager.IsJointTracked(userId, (int)jt))
                continue;

            Vector3 pos = kinectManager.GetJointPosition(userId, (int)jt);
            _records.Add(new MotionRecord(t, jt.ToString(), pos));
        }

        // И — если включено — лог углов рук
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
                Debug.Log($"[MotionLogger] Left angle added: {angle:F1}");
            }
            else
            {
                Debug.Log("[MotionLogger] Left NOT tracked");
            }

            // Правая рука
            if (kinectManager.IsJointTracked(userId, (int)KinectInterop.JointType.ShoulderRight) &&
                kinectManager.IsJointTracked(userId, (int)KinectInterop.JointType.HandRight))
            {
                Vector3 s = kinectManager.GetJointPosition(userId, (int)KinectInterop.JointType.ShoulderRight);
                Vector3 h = kinectManager.GetJointPosition(userId, (int)KinectInterop.JointType.HandRight);
                float angle = Vector3.Angle(h - s, Vector3.up);
                rightArmAngles.Add(angle);
                Debug.Log($"[MotionLogger] Right angle added: {angle:F1}");
            }
            else
            {
                Debug.Log("[MotionLogger] Right NOT tracked");
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
