using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmDialog : MonoBehaviour
{
    public static ConfirmDialog Instance { get; private set; }

    [Header("UI")]
    public CanvasGroup cg;
    public TMP_Text title;
    public TMP_Text body;
    public Button yesBtn;
    public Button noBtn;

    Action _onYes;
    Action _onNo;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Hide(true);

        if (yesBtn) yesBtn.onClick.AddListener(OnYes);
        if (noBtn) noBtn.onClick.AddListener(OnNo);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (yesBtn) yesBtn.onClick.RemoveListener(OnYes);
        if (noBtn) noBtn.onClick.RemoveListener(OnNo);
    }

    public void Ask(string titleText, string bodyText, Action onYes, Action onNo)
    {
        _onYes = onYes;
        _onNo = onNo;

        if (title) title.text = titleText;
        if (body) body.text = bodyText;

        Show();
    }

    void Show()
    {
        gameObject.SetActive(true);
        if (cg)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }

    public void Hide(bool immediate = false)
    {
        if (cg)
        {
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
        _onYes = null; _onNo = null;
    }

    void OnYes()
    {
        try { _onYes?.Invoke(); }
        finally { Hide(); }
    }

    void OnNo()
    {
        try { _onNo?.Invoke(); }
        finally { Hide(); }
    }
}
