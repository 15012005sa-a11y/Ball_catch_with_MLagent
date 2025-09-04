using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PatientCardView : MonoBehaviour
{
    [Header("Texts (можно не заполнять — автонайдёт по именам)")]
    public TMP_Text titleTMP; public Text titleUGUI;      // Patient_1Text
    public TMP_Text ageTMP; public Text ageUGUI;        // Rows/Row1/Value
    public TMP_Text startedTMP; public Text startedUGUI;    // Rows/Row2/Value

    void Awake()
    {
        // Автопоиск, если поля не проставлены в инспекторе
        if (!titleTMP && !titleUGUI)
        {
            var t = transform.Find("Patient_1Text");
            if (t) { titleTMP = t.GetComponent<TMP_Text>(); if (!titleTMP) titleUGUI = t.GetComponent<Text>(); }
        }
        if (!ageTMP && !ageUGUI)
        {
            var t = transform.Find("Rows/Row1/Value");
            if (t) { ageTMP = t.GetComponent<TMP_Text>(); if (!ageTMP) ageUGUI = t.GetComponent<Text>(); }
        }
        if (!startedTMP && !startedUGUI)
        {
            var t = transform.Find("Rows/Row2/Value");
            if (t) { startedTMP = t.GetComponent<TMP_Text>(); if (!startedTMP) startedUGUI = t.GetComponent<Text>(); }
        }
    }

    public void Bind(Patient p)
    {
        SetText(titleTMP, titleUGUI, p.displayName);
        SetText(ageTMP, ageUGUI, p.age.ToString());
        SetText(startedTMP, startedUGUI, p.startedRehab);

        // Пробросить id в ваши скрипты клика/подсветки
        var card = GetComponent<PatientCard>();
        if (card) card.patientId = p.id;

        var hi = GetComponent<PatientCardHighlight>();
        if (hi) hi.patientId = p.id;
    }

    static void SetText(TMP_Text tmp, Text ugui, string v)
    {
        if (tmp) tmp.text = v;
        else if (ugui) ugui.text = v;
    }
}
