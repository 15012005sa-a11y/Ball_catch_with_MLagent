using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class PatientCardView : MonoBehaviour
{
    [Header("Заголовок (имя сверху слева)")]
    public TMP_Text titleTMP; public Text titleUGUI;          // "Patient_1Text"

    [Header("Строки значений")]
    public TMP_Text nameTMP; public Text nameUGUI;           // "Rows/Row1/Value"
    public TMP_Text ageTMP; public Text ageUGUI;            // "Rows/Row2/Value"
    public TMP_Text startTMP; public Text startUGUI;          // "Rows/Row3/Value"

    [Header("UI")]
    public Button deleteButton;                               // "DeleteBtn" (X)
    public ConfirmOverlayPanel confirmOverlay;                // ModalLayer/ConfirmOverlay (можно не заполнять)

    private int boundId = -1;
    private string boundName = "";

    void Awake()
    {
        Auto(ref titleTMP, ref titleUGUI, "Patient_1Text");
        Auto(ref nameTMP, ref nameUGUI, "Rows/Row1/Value");
        Auto(ref ageTMP, ref ageUGUI, "Rows/Row2/Value");
        Auto(ref startTMP, ref startUGUI, "Rows/Row3/Value");

        if (!deleteButton)
        {
            var t = transform.Find("DeleteBtn");
            if (t) deleteButton = t.GetComponent<Button>();
        }
        if (deleteButton) deleteButton.onClick.AddListener(OnDeleteClicked);
    }

    void OnDestroy()
    {
        if (deleteButton) deleteButton.onClick.RemoveListener(OnDeleteClicked);
    }

    public void Bind(Patient p)
    {
        if (p == null) return;

        boundId = p.id;
        boundName = p.displayName ?? "patient";

        Set(titleTMP, titleUGUI, p.displayName);
        Set(nameTMP, nameUGUI, p.displayName);
        Set(ageTMP, ageUGUI, p.age.ToString());
        Set(startTMP, startUGUI, p.startedRehab);

        var card = GetComponent<PatientCard>(); if (card) card.patientId = p.id;
        var hi = GetComponent<PatientCardHighlight>(); if (hi) hi.patientId = p.id;
    }

    void OnDeleteClicked()
    {
        if (boundId < 0) return;

        // 1) Попробуем найти ConfirmOverlayPanel, если ссылка не задана
        var ov = GetOverlay();
        if (ov != null)
        {
            // Ваша панель имеет простой Show(int id) — покажем её.
            // Логика удаления по кнопке "Да" должна быть внутри ConfirmOverlayPanel.
            ov.Show(boundId);
            return;
        }

        // 2) Фолбэк — ConfirmDialog (если есть)
        var dlg = ConfirmDialog.Instance;
        if (dlg != null)
        {
            dlg.Ask(
                "Удалить пациента",
                $"Вы точно хотите удалить «{boundName}»?",
                onYes: () => PatientManager.Instance?.RemovePatient(boundId),
                onNo: null
            );
            return;
        }

        // 3) Ничего не нашли — удалим сразу (крайний вариант)
        Debug.LogWarning("[PatientCardView] Confirm overlay not found — deleting immediately.");
        PatientManager.Instance?.RemovePatient(boundId);
    }

    // --- helpers ---
    ConfirmOverlayPanel GetOverlay()
    {
        if (confirmOverlay == null)
            confirmOverlay = Object.FindObjectOfType<ConfirmOverlayPanel>(true); // true — искать и в неактивных
        return confirmOverlay;
    }

    void Auto(ref TMP_Text tmp, ref Text ugui, string path)
    {
        if (!tmp && !ugui)
        {
            var t = transform.Find(path);
            if (t)
            {
                tmp = t.GetComponent<TMP_Text>();
                if (!tmp) ugui = t.GetComponent<Text>();
            }
        }
    }

    void Set(TMP_Text tmp, Text ugui, string v)
    {
        if (tmp) tmp.text = v;
        else if (ugui) ugui.text = v;
    }
}
