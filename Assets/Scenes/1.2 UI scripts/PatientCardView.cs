using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class PatientCardView : MonoBehaviour
{
    [Header("Заголовок (имя сверху слева)")]
    public TMP_Text titleTMP; public Text titleUGUI;          // "Patient_1Text"

    [Header("Строки значений")]
    public TMP_Text nameTMP; public Text nameUGUI;           // "Rows/Row1/Value"  ← Имя пациента
    public TMP_Text ageTMP; public Text ageUGUI;            // "Rows/Row2/Value"  ← Возраст
    public TMP_Text startTMP; public Text startUGUI;          // "Rows/Row3/Value"  ← Начало реабилитаций

    [Header("UI (необязательно)")]
    public Button deleteButton;                               // "DeleteBtn" (X в углу)

    private int boundId = -1;

    void Awake()
    {
        // Автопоиск нужных узлов, если ссылки не заданы в инспекторе
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

    /// <summary>Заполнить карточку данными пациента.</summary>
    public void Bind(Patient p)
    {
        if (p == null) return;
        boundId = p.id;

        // Заголовок сверху
        Set(titleTMP, titleUGUI, p.displayName);

        // Значения по строкам
        Set(nameTMP, nameUGUI, p.displayName);      // Строка 1: Имя пациента
        Set(ageTMP, ageUGUI, p.age.ToString());   // Строка 2: Возраст
        Set(startTMP, startUGUI, p.startedRehab);     // Строка 3: Начало реабилитаций

        // Пробрасываем id во вспомогательные скрипты (если используются)
        var card = GetComponent<PatientCard>(); if (card) card.patientId = p.id;
        var hi = GetComponent<PatientCardHighlight>(); if (hi) hi.patientId = p.id;
    }

    private void OnDeleteClicked()
    {
        if (boundId < 0) return;
        var pm = PatientManager.Instance;
        if (pm != null) pm.RemovePatient(boundId);
    }

    // ---------- helpers ----------
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
