using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class KpiCardView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private RawImage sparkline;   // optional
    [SerializeField] private Image background;     // для акцентной подсветки, если нужно

    [Header("Sparkline")]
    [SerializeField] private int sparkWidth = 220;
    [SerializeField] private int sparkHeight = 28;
    [SerializeField] private Color sparkColor = new Color(0.27f, 0.83f, 0.52f); // мягкий зелёный
    [SerializeField] private Color sparkFade = new Color(0.27f, 0.83f, 0.52f, 0.2f); // фон линии (fill)

    private Texture2D _tex;

    public void SetLabel(string text) => labelText.text = text;

    public void SetValue(string text, Color? color = null)
    {
        valueText.text = text;
        if (color.HasValue) valueText.color = color.Value;
    }

    public void SetBackground(Color c)
    {
        if (background != null) background.color = c;
    }

    /// <summary>
    /// Рисует спарклайн по массиву значений (N последних сессий).
    /// Если min/max не заданы — берём по данным.
    /// </summary>
    public void SetSparkline(float[] data, float? minOverride = null, float? maxOverride = null, bool invert = false)
    {
        if (sparkline == null || data == null || data.Length < 2) { ClearSparkline(); return; }

        float min = minOverride ?? Mathf.Min(data);
        float max = maxOverride ?? Mathf.Max(data);
        if (Mathf.Approximately(min, max)) max = min + 1f;

        // Инициализация текстуры один раз
        if (_tex == null || _tex.width != sparkWidth || _tex.height != sparkHeight)
        {
            _tex = new Texture2D(sparkWidth, sparkHeight, TextureFormat.RGBA32, false);
            _tex.wrapMode = TextureWrapMode.Clamp;
            _tex.filterMode = FilterMode.Bilinear;
        }

        // Очистка
        var clear = new Color32(0, 0, 0, 0);
        var pixels = _tex.GetPixels32();
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        // Нормализация и рисование
        int n = data.Length;
        Vector2 prev = Vector2.zero;
        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 0 : (float)i / (n - 1);
            int x = Mathf.RoundToInt(Mathf.Lerp(0, sparkWidth - 1, t));
            float y01 = Mathf.InverseLerp(min, max, data[i]);
            if (invert) y01 = 1f - y01;
            int y = Mathf.RoundToInt(y01 * (sparkHeight - 1));

            var pt = new Vector2(x, y);
            if (i > 0) DrawLineAA(pixels, sparkWidth, sparkHeight, prev, pt, sparkColor);
            prev = pt;
        }

        _tex.SetPixels32(pixels);
        _tex.Apply();
        sparkline.texture = _tex;
    }

    public void ClearSparkline()
    {
        if (sparkline != null) sparkline.texture = null;
    }

    // Простейшая антиалиасная линия (двойной проход)
    private void DrawLineAA(Color32[] buf, int w, int h, Vector2 a, Vector2 b, Color col)
    {
        int steps = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(a, b)));
        Vector2 d = (b - a) / steps;

        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = a + d * i;
            int xi = (int)p.x;
            int yi = (int)p.y;
            // Толщина 2px
            Plot(buf, w, h, xi, yi, col);
            Plot(buf, w, h, xi, yi + 1, new Color(col.r, col.g, col.b, col.a * 0.6f));
        }
    }

    private void Plot(Color32[] buf, int w, int h, int x, int y, Color c)
    {
        if ((uint)x >= (uint)w || (uint)y >= (uint)h) return;
        int idx = y * w + x;
        var dst = buf[idx];
        // Alpha blend
        float a = c.a;
        float ia = 1f - a;
        buf[idx] = new Color(
            c.r * a + dst.r / 255f * ia,
            c.g * a + dst.g / 255f * ia,
            c.b * a + dst.b / 255f * ia,
            Mathf.Clamp01(a + dst.a / 255f)
        );
    }
}
