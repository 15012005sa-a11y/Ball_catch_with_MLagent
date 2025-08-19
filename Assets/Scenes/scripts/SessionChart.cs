using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static System.Net.Mime.MediaTypeNames;

public class SessionChart : MonoBehaviour
{
    [Header("Colors")]
    public Color32 background = new(14, 20, 28, 255);
    public Color32 gridColor = new(60, 70, 85, 255);
    public Color32 borderColor = new(255, 40, 40, 255);     // красная рамка
    public Color32 rightColor = new(255, 102, 0, 255);      // оранжевый
    public Color32 leftColor = new(255, 0, 0, 255);        // красный
    public Color32 axisText = new(90, 255, 90, 255);      // зелёные подписи

    [Header("Layout")]
    public int padding = 40;
    public int minTexW = 1200, minTexH = 500;
    public int borderThickness = 3;

    [Header("Axes (labels)")]
    public RectTransform axisLayer;            // контейнер подписей (стретч)
    public TMP_Text labelPrefab;               // шаблон метки TMP
    public int xTicks = 6;                     // колонок сетки (между вертикалями)
    public int yTicks = 4;                     // строк сетки (между горизонталями)
    [Space(6)]
    public int xLabelOffset = 18;        // на сколько опустить подпись X от нижнего края поля
    public bool includeEndXLabels = true;      // показывать подписи у левого/правого бордера
    public string dateFmtShort = "dd-MM-yy";   // формат даты под сеткой
    public string dateFmtLong = "MM-yyyy";    // для очень длинных диапазонов

    public enum XMode { Dates, Weeks4 }

    [Header("X‑axis mode")]
    public XMode xMode = XMode.Dates;

    RawImage img;
    Texture2D tex;

    void Reset()
    {
        img = GetComponent<RawImage>() ?? gameObject.AddComponent<RawImage>();
        img.raycastTarget = false;
    }

    void Awake()
    {
        img = GetComponent<RawImage>() ?? gameObject.AddComponent<RawImage>();
        img.raycastTarget = false;
        EnsureTexture();
        EnsureAxisLayer();
    }

    void OnEnable()
    {
        EnsureTexture();
        EnsureAxisLayer();
    }

    void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled) return;
        EnsureTexture();
    }

    void EnsureTexture()
    {
        if (img == null)
        {
            img = GetComponent<RawImage>() ?? gameObject.AddComponent<RawImage>();
            img.raycastTarget = false;
        }
        var rt = transform as RectTransform; if (!rt) return;

        int w = Mathf.RoundToInt(rt.rect.width);
        int h = Mathf.RoundToInt(rt.rect.height);
        if (w < 2 || h < 2) { w = Mathf.Max(minTexW, 256); h = Mathf.Max(minTexH, 128); }

        if (tex == null || tex.width != w || tex.height != h)
        {
            tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            img.texture = tex;
            img.color = Color.white;
        }
    }

    void EnsureAxisLayer()
    {
        if (!axisLayer)
        {
            var go = new GameObject("AxisLayer", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            axisLayer = go.GetComponent<RectTransform>();
            axisLayer.anchorMin = Vector2.zero;
            axisLayer.anchorMax = Vector2.one;
            axisLayer.offsetMin = Vector2.zero;
            axisLayer.offsetMax = Vector2.zero;
            axisLayer.pivot = new Vector2(0, 0);
        }
        if (!labelPrefab)
        {
            var g = new GameObject("LabelTemplate", typeof(RectTransform), typeof(TextMeshProUGUI));
            g.transform.SetParent(axisLayer, false);
            var rt = (RectTransform)g.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
            var tmp = g.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 12;
            tmp.color = axisText;
            tmp.raycastTarget = false;
            tmp.alignment = TextAlignmentOptions.Center;
            g.SetActive(false);
            labelPrefab = tmp;
        }
    }

    public void ShowAngles(IList<float> right, IList<float> left, DateTime start, DateTime end)
    {
        EnsureTexture(); EnsureAxisLayer(); if (!tex) return;
        Clear(tex, background);

        int w = tex.width, h = tex.height;
        int px = padding, py = padding;
        int gw = Mathf.Max(8, w - 2 * px);
        int gh = Mathf.Max(8, h - 2 * py);

        DrawBorder(tex, px, py, gw, gh, borderColor, borderThickness);

        // В режиме 4 недель сетка = 4 колонки, иначе как раньше
        int cols = (xMode == XMode.Weeks4) ? 4 : Mathf.Max(2, xTicks + (includeEndXLabels ? 0 : 1));
        int rows = Mathf.Max(2, yTicks);
        DrawGrid(tex, px, py, gw, gh, gridColor, rows, cols);

        float maxY = 1f;
        if (right != null && right.Count > 0) maxY = Mathf.Max(maxY, Max(right));
        if (left != null && left.Count > 0) maxY = Mathf.Max(maxY, Max(left));
        float yMax = NiceCeil(maxY);

        void DrawSeries(IList<float> s, Color32 col, float yMaxLocal)
        {
            if (s == null || s.Count < 2) return;
            for (int i = 1; i < s.Count; i++)
            {
                float t0 = (i - 1) / (float)(s.Count - 1);
                float t1 = i / (float)(s.Count - 1);
                int x0 = px + Mathf.RoundToInt(t0 * gw);
                int x1 = px + Mathf.RoundToInt(t1 * gw);
                int y0 = py + Mathf.RoundToInt(Mathf.Clamp01(s[i - 1] / yMaxLocal) * gh);
                int y1p = py + Mathf.RoundToInt(Mathf.Clamp01(s[i] / yMaxLocal) * gh);
                DrawLine(tex, x0, y0, x1, y1p, col);
            }
        }
        DrawSeries(right, rightColor, yMax);
        DrawSeries(left, leftColor, yMax);
        tex.Apply(false);

        // подписи осей
        RebuildAxisLabels(px, py, gw, gh, yMax, start.Date, end.Date, cols, rows);
    }


    public void ShowSingle(IList<float> y, DateTime start, DateTime end, string label, bool yAsPercent)
    {
        EnsureTexture(); EnsureAxisLayer(); if (!tex) return;
        Clear(tex, background);

        int w = tex.width, h = tex.height;
        int px = padding, py = padding;
        int gw = Mathf.Max(8, w - 2 * px);
        int gh = Mathf.Max(8, h - 2 * py);

        DrawBorder(tex, px, py, gw, gh, borderColor, borderThickness);

        int cols = (xMode == XMode.Weeks4) ? 4 : Mathf.Max(2, xTicks);
        int rows = Mathf.Max(2, yTicks);
        DrawGrid(tex, px, py, gw, gh, gridColor, rows, cols);

        float maxY = 1f;
        if (y != null && y.Count > 0) maxY = Mathf.Max(maxY, Max(y));
        if (yAsPercent) maxY = Mathf.Max(maxY, 1f);
        float yMax = NiceCeil(yAsPercent ? maxY * 100f : maxY);

        if (y != null && y.Count > 1)
        {
            for (int i = 1; i < y.Count; i++)
            {
                float v0 = yAsPercent ? y[i - 1] * 100f : y[i - 1];
                float v1 = yAsPercent ? y[i] * 100f : y[i];
                float t0 = (i - 1) / (float)(y.Count - 1);
                float t1 = i / (float)(y.Count - 1);
                int x0 = px + Mathf.RoundToInt(t0 * gw);
                int x1 = px + Mathf.RoundToInt(t1 * gw);
                int y0 = py + Mathf.RoundToInt(Mathf.Clamp01(v0 / yMax) * gh);
                int y1p = py + Mathf.RoundToInt(Mathf.Clamp01(v1 / yMax) * gh);
                DrawLine(tex, x0, y0, x1, y1p, rightColor);
            }
        }
        tex.Apply(false);

        RebuildAxisLabels(px, py, gw, gh, yMax, start, end, cols, rows);
    }


    // ---------- подписи осей ----------
    void RebuildAxisLabels(int px, int py, int gw, int gh, float yMax,
                       DateTime start, DateTime end, int cols, int rows)
    {
        if (!axisLayer || !labelPrefab) return;

        // очистка старых
        for (int i = axisLayer.childCount - 1; i >= 0; i--)
        {
            var go = axisLayer.GetChild(i).gameObject;
            if (go == labelPrefab.gameObject) continue;
            if(UnityEngine.Application.isPlaying) Destroy(go); else DestroyImmediate(go);

        }

        // X-подписи (Dates или Weeks4)
        int yForX = py + xLabelOffset;
        BuildXLabels(px, yForX, gw, gh, start, end, cols);

        // Y-подписи (как раньше)
        float step = NiceStep(yMax / Mathf.Max(2, rows));
        for (float v = 0; v <= yMax + 0.001f; v += step)
        {
            float ty = (yMax <= 0f) ? 0f : Mathf.Clamp01(v / yMax);
            int x = px - 8;
            int y = py + Mathf.RoundToInt(ty * gh);
            string txt = v.ToString(v >= 100 ? "0" : v >= 10 ? "0.#" : "0.##");
            var lbl = CreateLabel(new Vector2(x, y), txt, TextAlignmentOptions.MidlineRight,
                                  pivot: new Vector2(1f, 0.5f));
            lbl.color = axisText;
        }
    }

    // Построение X‑подписей (Dates / Weeks4)
    void BuildXLabels(int px, int yForX, int gw, int gh, DateTime start, DateTime end, int cols)
    {
        if (xMode == XMode.Weeks4)
        {
            const int ticks = 4;                 // 4 «коробки» недель
            for (int i = 0; i <= ticks; i++)
            {
                float t = i / (float)ticks;      // 0..1
                int x = px + Mathf.RoundToInt(gw * t);
                string text = (i == 0 || i == ticks) ? "" : $"{i} week";
                CreateXLabel(x, yForX, text);
            }
        }
        else
        {
            int gridCols = Mathf.Max(1, cols);
            int totalDays = Mathf.Max(0, (end.Date - start.Date).Days);
            for (int c = 0; c <= gridCols; c++)
            {
                float t = c / (float)gridCols;
                int x = px + Mathf.RoundToInt(gw * t);
                int dayIndex = Mathf.Clamp(Mathf.RoundToInt(totalDays * t), 0, totalDays);
                DateTime d = start.Date.AddDays(dayIndex);
                string text = (totalDays > 180) ? d.ToString(dateFmtLong) : d.ToString(dateFmtShort);
                CreateXLabel(x, yForX, text);
            }
        }
    }


    TMP_Text CreateXLabel(int x, int y, string text)
    {
        var lbl = Instantiate(labelPrefab, axisLayer);
        lbl.gameObject.SetActive(true);
        lbl.text = text;
        lbl.alignment = TextAlignmentOptions.Midline;
        lbl.color = axisText;
        var rt = lbl.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        return lbl;
    }

    TMP_Text CreateLabel(Vector2 anchoredPos, string text, TextAlignmentOptions align, Vector2? pivot = null)
    {
        var lbl = Instantiate(labelPrefab, axisLayer);
        lbl.gameObject.SetActive(true);
        lbl.text = text;
        lbl.alignment = align;
        var rt = lbl.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
        rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        return lbl;
    }

    // ---------- utils ----------
    static float Max(IList<float> a) { float m = 0f; for (int i = 0; i < a.Count; i++) if (a[i] > m) m = a[i]; return m; }
    static void Clear(Texture2D t, Color32 c) { var fill = new Color32[t.width * t.height]; for (int i = 0; i < fill.Length; i++) fill[i] = c; t.SetPixels32(fill); }

    static void DrawGrid(Texture2D t, int x, int y, int w, int h, Color32 c, int rows, int cols)
    {
        DrawRect(t, x, y, w, h, c); // тонкая рамка сетки
        for (int r = 1; r < rows; r++)
        {
            int yy = y + Mathf.RoundToInt(h * (r / (float)rows));
            DrawLine(t, x, yy, x + w, yy, c);
        }
        for (int col = 1; col < cols; col++)
        {
            int xx = x + Mathf.RoundToInt(w * (col / (float)cols));
            DrawLine(t, xx, y, xx, y + h, c);
        }
    }

    static void DrawRect(Texture2D t, int x, int y, int w, int h, Color32 c)
    { DrawLine(t, x, y, x + w, y, c); DrawLine(t, x, y + h, x + w, y + h, c); DrawLine(t, x, y, x, y + h, c); DrawLine(t, x + w, y, x + w, y + h, c); }

    static void DrawLine(Texture2D t, int x0, int y0, int x1, int y1, Color32 c)
    {
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy, e2;
        while (true)
        {
            if ((uint)x0 < t.width && (uint)y0 < t.height) t.SetPixel(x0, y0, c);
            if (x0 == x1 && y0 == y1) break;
            e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    static void DrawBorder(Texture2D t, int x, int y, int w, int h, Color32 c, int thickness)
    {
        thickness = Mathf.Max(1, thickness);
        for (int i = 0; i < thickness; i++) DrawRect(t, x + i, y + i, w - 2 * i, h - 2 * i, c);
    }

    static float NiceCeil(float x)
    {
        if (x <= 0) return 1f;
        float exp = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(x)));
        float f = x / exp;
        float nf = (f <= 1f) ? 1f : (f <= 2f) ? 2f : (f <= 5f) ? 5f : 10f;
        return nf * exp;
    }
    static float NiceStep(float raw)
    {
        if (raw <= 0) return 1f;
        float exp = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(raw)));
        float f = raw / exp;
        float nf = (f <= 1f) ? 1f : (f <= 2f) ? 2f : (f <= 5f) ? 5f : 10f;
        return nf * exp;
    }
}
