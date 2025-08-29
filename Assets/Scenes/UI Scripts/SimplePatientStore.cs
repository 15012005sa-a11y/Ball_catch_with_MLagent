using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SimplePatientStore
{
    public class Patient { public int Id; public string Name; }

    static List<Patient> _items = new List<Patient>();
    static int _nextId = 1;

    public static List<Patient> GetAll() => _items.ToList();

    public static Patient Add(string name)
    {
        var p = new Patient { Id = _nextId++, Name = name };
        _items.Add(p);
        Debug.Log($"[Patients] Added: {p.Id} {p.Name}");
        return p;
    }

    public static void Remove(int id)
    {
        var it = _items.FirstOrDefault(x => x.Id == id);
        if (it != null) _items.Remove(it);
    }
}
