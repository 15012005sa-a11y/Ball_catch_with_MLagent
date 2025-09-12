using UnityEngine;
using UnityEngine.EventSystems;

public class UIInputAutoFix : MonoBehaviour
{
    void Awake()
    {
        // 1) Гарантируем EventSystem
        if (EventSystem.current == null)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            Debug.Log("[UIInputAutoFix] EventSystem создан.");
        }
    }
}
