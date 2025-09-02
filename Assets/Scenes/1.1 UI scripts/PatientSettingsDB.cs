using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Простое JSON-хранилище настроек пациентов. Один файл:
/// { "items":[ {"Id":1,"Settings":{...}}, {"Id":2,"Settings":{...}} ] }
/// </summary>
public static class PatientSettingsDB
{
    [Serializable]
    private class Entry
    {
        public int Id;
        public PatientSettings Settings;
    }

    [Serializable]
    private class FileModel
    {
        public List<Entry> items = new List<Entry>();
    }

    private static readonly string _filePath =
        Path.Combine(Application.persistentDataPath, "patient_settings.json");

    public static string FilePath => _filePath;

    private static Dictionary<int, PatientSettings> _cache;
    private static bool _loaded;

    // --- загрузка всего файла в кэш (лениво) ---
    private static void EnsureLoaded()
    {
        if (_loaded) return;

        _cache = new Dictionary<int, PatientSettings>();
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var model = JsonUtility.FromJson<FileModel>(string.IsNullOrEmpty(json) ? "{}" : json) ?? new FileModel();
                if (model.items != null)
                {
                    foreach (var e in model.items)
                    {
                        if (e == null) continue;
                        if (e.Settings == null) e.Settings = new PatientSettings();
                        _cache[e.Id] = e.Settings;
                    }
                }
            }
            else
            {
                // файла нет — создадим пустой
                SaveAll();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PatientSettingsDB] Broken file, recreating: {_filePath}\n{ex}");
            _cache.Clear();
            SaveAll();
        }

        _loaded = true;
    }

    // --- сброс кэша в файл ---
    public static void SaveAll()
    {
        try
        {
            var model = new FileModel();
            foreach (var kv in _cache)
                model.items.Add(new Entry { Id = kv.Key, Settings = kv.Value });

            var json = JsonUtility.ToJson(model, true);
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath) ?? Application.persistentDataPath);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PatientSettingsDB] SaveAll failed: {_filePath}\n{ex}");
        }
    }

    // --- API ---
    public static PatientSettings Load(int patientId)
    {
        EnsureLoaded();
        if (patientId <= 0) return new PatientSettings();

        if (_cache.TryGetValue(patientId, out var s) && s != null)
            return s;

        var def = new PatientSettings();
        _cache[patientId] = def;
        SaveAll();
        return def;
    }

    public static void Save(int patientId, PatientSettings s)
    {
        if (patientId <= 0 || s == null) return;
        EnsureLoaded();
        _cache[patientId] = s;
        SaveAll();
    }

    public static void Delete(int patientId)
    {
        EnsureLoaded();
        if (_cache.Remove(patientId))
            SaveAll();
    }
}
