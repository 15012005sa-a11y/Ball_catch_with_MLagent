using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public static class GpuLeakDetection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Enable()
    {
#if UNITY_EDITOR
        UnsafeUtility.SetLeakDetectionMode(NativeLeakDetectionMode.EnabledWithStackTrace);
#endif
    }
}
