using UnityEngine;
using System;

public class PerformanceWindow : MonoBehaviour
{
    public event Action<bool, float, float> OnResult; // success, reactionSec, rom01
    public int window = 20;

    private int _count;
    private int _success;
    private float _sumReaction;
    private float _sumRom;

    public float SuccessRate01 => _count == 0 ? 0f : (float)_success / _count;
    public float MeanReactionSec => _count == 0 ? 0f : _sumReaction / _count;
    public float MeanRom01 => _count == 0 ? 0f : _sumRom / _count;

    public void ResetWindow() { _count = 0; _success = 0; _sumReaction = 0; _sumRom = 0; }

    // Вызывай через адаптер на каждый исход попытки
    public void Report(bool success, float reactionSec, float rom01)
    {
        _count = Mathf.Min(_count + 1, window);
        _success = Mathf.Min(_success + (success ? 1 : 0), window);
        _sumReaction = Mathf.Clamp(_sumReaction + reactionSec, 0, 1e6f);
        _sumRom = Mathf.Clamp01(_sumRom + rom01);

        OnResult?.Invoke(success, reactionSec, rom01);
    }
}
