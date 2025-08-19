using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HistoryController : MonoBehaviour
{
    [Header("Source")]
    public string fileName = "PatientProgress3.csv";

    [Header("UI")]
    public TMP_Dropdown ddPatient;
    public TMP_Dropdown ddGranularity; // 0 - Session, 1 - Week, 2 - Month
    public TMP_Dropdown ddGraph;       // 0 - Hands, 1 - Success rate, 2 - Reaction

    public TMP_InputField ifDateFrom;
    public TMP_InputField ifDateTo;

    public Toggle tgRight;
    public Toggle tgLeft;

    public Toggle tgSmooth;
    public TMP_InputField ifSmoothWin;

    public Button btn7d, btn30d, btn90d, btnAll;
    public Button btnExportPNG, btnExportPDF;

    [Header("Chart")]
    public SessionChart chart;

    // Выпадающий пресет дат (Header/DD_DatesPresets)
    public TMP_Dropdown ddDatesPresets;

    private enum DatePreset { Last7Days = 0, Last30Days = 1, All = 2 }
    private DatePreset _preset = DatePreset.All;

    // данные
    private List<ProgressRow> _all = new();
    private List<int> _patients = new();

    enum GraphMode { Hands = 0, Success = 1, Reaction = 2 }

    Transform _root;

    // ---------- helpers ----------
    T FindUnder<T>(string path) where T : UnityEngine.Component
    {
        if (_root == null) return null;
        var tr = _root.Find(path);
        return tr ? tr.GetComponent<T>() : null;
    }

    void AutoWireIfNull()
    {
        // скрипт висит на HistoryPanel
        _root = transform;

        ddPatient ??= FindUnder<TMP_Dropdown>("Header/DD_Patient");
        ddGranularity ??= FindUnder<TMP_Dropdown>("Header/DD_Granularity");
        ddGraph ??= FindUnder<TMP_Dropdown>("Header/DD_Graph");
        ddDatesPresets ??= FindUnder<TMP_Dropdown>("Header/DD_DatesPresets");

        ifDateFrom ??= FindUnder<TMP_InputField>("Header/IF_DateFrom");
        ifDateTo ??= FindUnder<TMP_InputField>("Header/IF_DateTo");
        tgRight ??= FindUnder<Toggle>("Header/TG_Right");
        tgLeft ??= FindUnder<Toggle>("Header/TG_Left");
        tgSmooth ??= FindUnder<Toggle>("Header/TG_Smoothing");
        ifSmoothWin ??= FindUnder<TMP_InputField>("Header/IF_SmoothWin");

        btn7d ??= FindUnder<Button>("Header/Presets_7d");
        btn30d ??= FindUnder<Button>("Header/Presets_30d");
        btn90d ??= FindUnder<Button>("Header/Presets_90d");
        btnAll ??= FindUnder<Button>("Header/Presets_All");
        btnExportPNG ??= FindUnder<Button>("Header/BTN_ExportPNG");
        btnExportPDF ??= FindUnder<Button>("Header/BTN_ExportPDF");

        chart ??= FindUnder<SessionChart>("Content/HistoryChartImage");
    }

    // ---------- lifecycle ----------
    private void Start()
    {
        AutoWireIfNull();

        if (chart == null)
        {
            Debug.LogError("[History] Не назначен SessionChart (Content/HistoryChartImage).");
            enabled = false; return;
        }

        // загрузка данных
        string path = System.IO.Path.Combine(Application.persistentDataPath, fileName);
        _all = PatientProgressLoader.LoadCsv(path).OrderBy(r => r.Date).ToList();

        // пациенты
        _patients = _all.Select(r => r.PatientID).Distinct().OrderBy(v => v).ToList();
        if (ddPatient)
        {
            ddPatient.ClearOptions();
            ddPatient.AddOptions(_patients.ConvertAll(p => new TMP_Dropdown.OptionData(p.ToString())));
        }
        else
        {
            Debug.LogWarning("[History] ddPatient не найден. Автопереключение пациента недоступно.");
        }

        // DD_Graph: 3 пункта
        if (ddGraph != null)
        {
            if (ddGraph.options == null || ddGraph.options.Count < 3)
            {
                ddGraph.ClearOptions();
                ddGraph.AddOptions(new List<TMP_Dropdown.OptionData> {
                    new("Right/Left"),
                    new("Success rate"),
                    new("Reaction")
                });
            }
            ddGraph.value = 0;
        }

        // DD_DatesPresets: 7 day, 30 day, All
        // --- пресеты дат (7 day / 30 day / All)
        if (ddDatesPresets != null)
        {
            ddDatesPresets.ClearOptions();
            ddDatesPresets.AddOptions(new List<TMP_Dropdown.OptionData> {
                new TMP_Dropdown.OptionData("7 day"),
                new TMP_Dropdown.OptionData("30 day"),
                new TMP_Dropdown.OptionData("All")
            });
            ddDatesPresets.value = 2; // All по умолчанию
            ddDatesPresets.onValueChanged.RemoveAllListeners();
            ddDatesPresets.onValueChanged.AddListener(OnDatesPresetChanged);
        }

        // события
        if (ddPatient) ddPatient.onValueChanged.AddListener(_ => Redraw());
        if (ddGranularity) ddGranularity.onValueChanged.AddListener(_ => Redraw());
        if (ddGraph) ddGraph.onValueChanged.AddListener(_ => { UpdateUiForMode(); Redraw(); });

        if (tgRight) tgRight.onValueChanged.AddListener(_ => Redraw());
        if (tgLeft) tgLeft.onValueChanged.AddListener(_ => Redraw());
        if (tgSmooth) tgSmooth.onValueChanged.AddListener(_ => Redraw());
        if (ifSmoothWin) ifSmoothWin.onEndEdit.AddListener(_ => Redraw());

        if (btn7d) btn7d.onClick.AddListener(() => { _preset = DatePreset.Last7Days; ApplyDatePresetAndRedraw(); });
        if (btn30d) btn30d.onClick.AddListener(() => { _preset = DatePreset.Last30Days; ApplyDatePresetAndRedraw(); });
        if (btn90d) btn90d.onClick.AddListener(() => { _preset = DatePreset.Last30Days; ApplyDatePresetAndRedraw(); }); // можно перенастроить
        if (btnAll) btnAll.onClick.AddListener(() => { _preset = DatePreset.All; ApplyDatePresetAndRedraw(); });

        if (btnExportPNG) btnExportPNG.onClick.AddListener(ExportPNG);
        if (btnExportPDF) btnExportPDF.onClick.AddListener(ExportPDF);

        UpdateUiForMode();

        // старт: применим выбранный пресет (по умолчанию All)
        ApplyDatePresetAndRedraw();

        if (_patients.Count == 0)
            Debug.LogWarning("[History] CSV пустой или не найден — график появится после первой сессии.");
    }

    private void UpdateUiForMode()
    {
        var mode = (GraphMode)(ddGraph ? ddGraph.value : 0);
        bool hands = (mode == GraphMode.Hands);
        if (tgRight) tgRight.gameObject.SetActive(hands);
        if (tgLeft) tgLeft.gameObject.SetActive(hands);
    }

    // ---------- presets ----------
    private void OnDatesPresetChanged(int idx)
    {
        _preset = (DatePreset)Mathf.Clamp(idx, 0, 2);
        ApplyDatePresetAndRedraw();
    }

    private void ApplyDatePresetAndRedraw()
    {
        if (_all.Count == 0) return;
        var maxDate = _all.Max(r => r.Date).Date;

        switch (_preset)
        {
            case DatePreset.Last7Days:
                ifDateTo.text = maxDate.ToString("yyyy-MM-dd");
                ifDateFrom.text = maxDate.AddDays(-6).ToString("yyyy-MM-dd");
                chart.xMode = SessionChart.XMode.Dates;   // показать 7 дат
                break;

            case DatePreset.Last30Days:
                ifDateTo.text = maxDate.ToString("yyyy-MM-dd");
                ifDateFrom.text = maxDate.AddDays(-29).ToString("yyyy-MM-dd");
                chart.xMode = SessionChart.XMode.Weeks4;  // подписи 1..4 week
                break;

            default: // All
                ifDateFrom.text = "";
                ifDateTo.text = "";
                chart.xMode = SessionChart.XMode.Dates;
                break;
        }
        Redraw();
    }

    // ---------- reading dates ----------
    private (DateTime? from, DateTime? to) ReadDates()
    {
        DateTime f, t; DateTime? from = null, to = null;
        if (ifDateFrom && DateTime.TryParse(ifDateFrom.text, out f)) from = f.Date;
        if (ifDateTo && DateTime.TryParse(ifDateTo.text, out t)) to = t.Date;
        return (from, to);
    }

    // ---------- redraw ----------
    private void Redraw()
    {
        if (chart == null) return;
        if (_patients.Count == 0 || _all.Count == 0) { chart.ShowSingle(new List<float>(), DateTime.Today, DateTime.Today, "", false); return; }
        if (ddPatient == null || ddPatient.value < 0) return;

        int pid = _patients[Mathf.Clamp(ddPatient.value, 0, _patients.Count - 1)];
        var (from, to) = ReadDates();

        IEnumerable<ProgressRow> q = _all.Where(r => r.PatientID == pid);
        if (from.HasValue) q = q.Where(r => r.Date >= from.Value);
        if (to.HasValue) q = q.Where(r => r.Date <= to.Value);
        var list = q.OrderBy(r => r.Date).ToList();
        if (list.Count == 0) return;

        int gran = ddGranularity ? ddGranularity.value : 0;
        GraphMode mode = (GraphMode)(ddGraph ? ddGraph.value : 0);

        var x = new List<DateTime>();
        var y1 = new List<float>(); // основная серия
        var y2 = new List<float>(); // вторая серия (только для Hands)

        if (gran == 1) // Week
        {
            if (mode == GraphMode.Hands)
            {
                var w = HistoryAggregator.ToWeeklyAverages(list);
                foreach (var t in w) { x.Add(t.weekStart); y1.Add(t.right); y2.Add(t.left); }
            }
            else if (mode == GraphMode.Success)
            {
                var w = HistoryAggregator.ToWeeklyAvgSuccess(list);
                foreach (var t in w) { x.Add(t.weekStart); y1.Add(t.success); }
            }
            else // Reaction
            {
                var w = HistoryAggregator.ToWeeklyAvgReaction(list);
                foreach (var t in w) { x.Add(t.weekStart); y1.Add(t.reaction); }
            }
        }
        else if (gran == 2) // Month
        {
            if (mode == GraphMode.Hands)
            {
                var m = HistoryAggregator.ToMonthlyAverages(list);
                foreach (var t in m) { x.Add(t.month); y1.Add(t.right); y2.Add(t.left); }
            }
            else if (mode == GraphMode.Success)
            {
                var m = HistoryAggregator.ToMonthlyAvgSuccess(list);
                foreach (var t in m) { x.Add(t.month); y1.Add(t.success); }
            }
            else // Reaction
            {
                var m = HistoryAggregator.ToMonthlyAvgReaction(list);
                foreach (var t in m) { x.Add(t.month); y1.Add(t.reaction); }
            }
        }
        else // Session
        {
            foreach (var r in list)
            {
                x.Add(r.Date);
                if (mode == GraphMode.Hands) { y1.Add(r.RightHand); y2.Add(r.LeftHand); }
                else if (mode == GraphMode.Success) y1.Add(r.SuccessRate); // 0..1
                else y1.Add(r.Reaction);                                   // сек
            }
        }

        if (tgSmooth && tgSmooth.isOn && int.TryParse(ifSmoothWin?.text, out int wWin) && wWin > 1)
        {
            y1 = HistoryAggregator.MovingAverage(y1, wWin);
            if (mode == GraphMode.Hands) y2 = HistoryAggregator.MovingAverage(y2, wWin);
        }

        if (mode == GraphMode.Hands)
        {
            if (tgRight && !tgRight.isOn) y1 = new List<float>(new float[y1.Count]);
            if (tgLeft && !tgLeft.isOn) y2 = new List<float>(new float[y2.Count]);
        }

        DateTime axisStart = from?.Date ?? x.First();
        DateTime axisEnd = to?.Date ?? x.Last();

        if (mode == GraphMode.Hands)
            chart.ShowAngles(y1, y2, axisStart, axisEnd);
        else if (mode == GraphMode.Success)
            chart.ShowSingle(y1, axisStart, axisEnd, "Success rate", yAsPercent: true);
        else
            chart.ShowSingle(y1, axisStart, axisEnd, "Reaction (s)", yAsPercent: false);
    }

    // ---------- export ----------
    private void ExportPNG()
    {
        var tex = chart.GetComponent<RawImage>().texture as Texture2D;
        if (tex == null) return;
        byte[] png = tex.EncodeToPNG();
        var path = System.IO.Path.Combine(Application.persistentDataPath, $"History_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        System.IO.File.WriteAllBytes(path, png);
        Debug.Log($"[History] Saved PNG: {path}");
    }

    private void ExportPDF()
    {
        ExportPNG();
        Debug.Log("[History] PDF: сейчас сохраняем PNG. Для реального PDF подключите библиотеку (iText7/QuestPDF).");
    }
}
