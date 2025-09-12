using System.IO;
using UnityEngine;

public static class PatientSettingsIO
{
    private static string EnsureDir(int patientId)
    {
        string dir = Path.Combine(Application.persistentDataPath, "Patients", patientId.ToString());
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return dir;
    }

    public static string GetPath(int patientId)
    {
        return Path.Combine(EnsureDir(patientId), "settings.json");
    }

    public static void Save(int patientId, PatientSettings s)
    {
        if (patientId < 0 || s == null) return;
        s.EnsureDefaults();
        string json = JsonUtility.ToJson(s, prettyPrint: true);
        File.WriteAllText(GetPath(patientId), json);
        Debug.Log($"[SettingsIO] Saved → {GetPath(patientId)}");
    }

    public static bool TryLoad(int patientId, out PatientSettings s)
    {
        s = null;
        if (patientId < 0) return false;
        string path = GetPath(patientId);
        if (!File.Exists(path)) return false;
        try
        {
            string json = File.ReadAllText(path);
            var loaded = JsonUtility.FromJson<PatientSettings>(json);
            loaded.EnsureDefaults();
            s = loaded;
            return true;
        }
        catch { return false; }
    }
}
