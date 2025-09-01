using System.Linq;
using System.Collections.Generic;
using UnityEngine;                    // ← добавлено
using UnityEngine.UI;
using UnityEngine.EventSystems;       // ← добавлено
using TMPro;
using System;

public static class UIPrimitives
{
    // THEME (dark)
    public static readonly Color32 BgMain = new Color32(13, 22, 33, 255);    // #0D1621
    public static readonly Color32 BgPanel = new Color32(23, 27, 34, 255);    // #171B22 (sidebar)
    public static readonly Color32 TextMuted = new Color32(159, 179, 200, 255); // #9FB3C8
    public static readonly Color32 Text = new Color32(218, 230, 245, 255);
    public static readonly Color32 Accent = new Color32(52, 112, 255, 255);  // blue
    public static readonly Color32 FieldBg = new Color32(28, 36, 48, 255);
    public static readonly Color32 ButtonBg = new Color32(46, 91, 233, 255);
    public static readonly Color32 ButtonBgH = new Color32(66, 111, 253, 255);

    // ---------- TextField ----------
    public static (TMP_InputField field, RectTransform root) TextField(Transform parent, string placeholder = "", string defaultText = "")
    {
        var root = new GameObject("TextField", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        var img = root.GetComponent<Image>(); img.color = FieldBg;
        var rt = (RectTransform)root.transform; rt.sizeDelta = new Vector2(240, 44);

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(root.transform, false);
        var text = textGO.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset; text.fontSize = 20; text.color = Text; text.text = defaultText;
        var textRT = (RectTransform)textGO.transform; textRT.anchorMin = new Vector2(0, 0); textRT.anchorMax = new Vector2(1, 1); textRT.offsetMin = new Vector2(12, 6); textRT.offsetMax = new Vector2(-12, -6);

        var phGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        phGO.transform.SetParent(root.transform, false);
        var ph = phGO.GetComponent<TextMeshProUGUI>(); ph.font = TMP_Settings.defaultFontAsset; ph.fontSize = 20; ph.color = new Color32(200, 200, 200, 100); ph.text = placeholder;
        var phRT = (RectTransform)phGO.transform; phRT.anchorMin = new Vector2(0, 0); phRT.anchorMax = new Vector2(1, 1); phRT.offsetMin = new Vector2(12, 6); phRT.offsetMax = new Vector2(-12, -6);

        var input = root.AddComponent<TMP_InputField>();
        input.textViewport = rt; input.textComponent = text; input.placeholder = ph;
        input.contentType = TMP_InputField.ContentType.Standard;

        var le = root.AddComponent<LayoutElement>(); le.preferredHeight = 44; le.minHeight = 40; le.preferredWidth = 260;
        return (input, rt);
    }

    // ---------- Scroll ----------
    public static (ScrollRect scroll, RectTransform content) VerticalScroll(Transform parent, Vector2 size)
    {
        var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        scrollGO.transform.SetParent(parent, false);
        var srt = (RectTransform)scrollGO.transform; srt.sizeDelta = size;
        var bg = scrollGO.GetComponent<Image>(); bg.color = new Color32(0, 0, 0, 0);
        var mask = scrollGO.GetComponent<Mask>(); mask.showMaskGraphic = false;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(scrollGO.transform, false);
        var crt = (RectTransform)contentGO.transform;
        crt.anchorMin = new Vector2(0, 1);
        crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.offsetMin = new Vector2(0, 0);
        crt.offsetMax = new Vector2(0, 0);

        var vl = contentGO.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(8, 8, 8, 8);
        vl.spacing = 8;
        vl.childForceExpandHeight = false;

        var fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = scrollGO.GetComponent<ScrollRect>();
        sr.viewport = srt;
        sr.content = crt;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;

        var le = scrollGO.AddComponent<LayoutElement>();
        le.minHeight = size.y; le.preferredHeight = size.y; le.flexibleHeight = 1;

        return (sr, crt);
    }

    // ---------- Canvas ----------
    public static Canvas EnsureCanvas()
    {
        var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        if (canvas != null) return canvas;

        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    // ---------- Panels ----------
    public static RectTransform FullscreenPanel(Transform parent, string name, Color32 bg)
    {
        var rt = Panel(parent, name, bg);
        Stretch(rt);
        return rt;
    }

    public static RectTransform Panel(Transform parent, string name, Color32 bg)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = bg;
        return (RectTransform)go.transform;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ---------- Label ----------
    public static TextMeshProUGUI Label(Transform parent, string text, int size = 16, bool bold = false, Color32? color = null, TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        var go = new GameObject("TMP_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = size;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.color = color ?? Text;
        tmp.alignment = align;
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        return tmp;
    }

    // ---------- Button ----------
    public static Button Button(Transform parent, string label, int fontSize, System.Action onClick, float preferredWidth = -1f)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>(); img.color = ButtonBg;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = ButtonBgH;
        colors.pressedColor = ButtonBgH;
        colors.selectedColor = ButtonBg;
        btn.colors = colors;

        var t = Label(go.transform, label, fontSize, true, Color.white, TextAlignmentOptions.Center);
        Stretch((RectTransform)t.transform);

        btn.onClick.AddListener(() => onClick?.Invoke());

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 44;
        le.minHeight = 40;
        if (preferredWidth > 0f) { le.preferredWidth = preferredWidth; le.minWidth = preferredWidth; }

        return btn;
    }

    // ---------- Toggle ----------
    public static Toggle Toggle(Transform parent, string label, bool defaultOn)
    {
        var root = new GameObject("Toggle", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rt = (RectTransform)root.transform;
        var le = root.AddComponent<LayoutElement>(); le.preferredHeight = 44;

        var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(root.transform, false);
        var bgRT = (RectTransform)bgGO.transform; bgRT.sizeDelta = new Vector2(24, 24); bgRT.anchorMin = new Vector2(0, .5f); bgRT.anchorMax = new Vector2(0, .5f); bgRT.anchoredPosition = new Vector2(12, 0);
        var bgImg = bgGO.GetComponent<Image>(); bgImg.color = FieldBg;

        var ckGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        ckGO.transform.SetParent(bgGO.transform, false);
        var ckRT = (RectTransform)ckGO.transform; ckRT.sizeDelta = new Vector2(18, 18); ckRT.anchorMin = ckRT.anchorMax = new Vector2(.5f, .5f); ckRT.anchoredPosition = Vector2.zero;
        var ckImg = ckGO.GetComponent<Image>(); ckImg.color = Accent;

        var text = Label(root.transform, label, 18, false, Text);
        var tRT = (RectTransform)text.transform; tRT.anchorMin = new Vector2(0, 0); tRT.anchorMax = new Vector2(1, 1); tRT.offsetMin = new Vector2(48, 0); tRT.offsetMax = new Vector2(0, 0);

        var toggle = root.AddComponent<Toggle>();
        toggle.isOn = defaultOn;
        toggle.targetGraphic = bgImg;
        toggle.graphic = ckImg;
        ckImg.enabled = defaultOn;
        toggle.onValueChanged.AddListener(v => ckImg.enabled = v);
        return toggle;
    }

    // ---------- NumericField ----------
    public static (TMP_InputField field, RectTransform root) NumericField(Transform parent, string defaultText, string suffix = " s")
    {
        var root = new GameObject("NumericField", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        var img = root.GetComponent<Image>(); img.color = FieldBg;
        var rt = (RectTransform)root.transform; rt.sizeDelta = new Vector2(320, 44);

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(root.transform, false);
        var text = textGO.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset; text.fontSize = 20; text.color = Text; text.enableAutoSizing = false; text.text = defaultText + suffix;
        var textRT = (RectTransform)textGO.transform; textRT.anchorMin = new Vector2(0, 0); textRT.anchorMax = new Vector2(1, 1); textRT.offsetMin = new Vector2(12, 6); textRT.offsetMax = new Vector2(-12, -6);

        var phGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        phGO.transform.SetParent(root.transform, false);
        var ph = phGO.GetComponent<TextMeshProUGUI>(); ph.font = TMP_Settings.defaultFontAsset; ph.fontSize = 20; ph.color = new Color32(200, 200, 200, 100); ph.text = "";
        var phRT = (RectTransform)phGO.transform; phRT.anchorMin = new Vector2(0, 0); phRT.anchorMax = new Vector2(1, 1); phRT.offsetMin = new Vector2(12, 6); phRT.offsetMax = new Vector2(-12, -6);

        var input = root.AddComponent<TMP_InputField>();
        input.textViewport = rt;
        input.textComponent = text;
        input.placeholder = ph;
        input.contentType = TMP_InputField.ContentType.Standard;
        input.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        input.onEndEdit.AddListener(_ =>
        {
            string num = ExtractNumber(input.text);
            if (string.IsNullOrEmpty(num)) num = defaultText;
            input.text = num + suffix;
        });

        var le = root.AddComponent<LayoutElement>(); le.preferredHeight = 44; le.minHeight = 40; le.preferredWidth = 260;
        return (input, rt);
    }

    // ---------- FloatField ----------
    public static (TMP_InputField field, RectTransform root) FloatField(Transform parent, string defaultText)
    {
        var root = new GameObject("FloatField", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        var img = root.GetComponent<Image>(); img.color = FieldBg;
        var rt = (RectTransform)root.transform; rt.sizeDelta = new Vector2(180, 44);

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(root.transform, false);
        var text = textGO.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset; text.fontSize = 20; text.color = Text; text.text = defaultText;
        var textRT = (RectTransform)textGO.transform; textRT.anchorMin = new Vector2(0, 0); textRT.anchorMax = new Vector2(1, 1); textRT.offsetMin = new Vector2(12, 6); textRT.offsetMax = new Vector2(-12, -6);

        var phGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        phGO.transform.SetParent(root.transform, false);
        var ph = phGO.GetComponent<TextMeshProUGUI>(); ph.font = TMP_Settings.defaultFontAsset; ph.fontSize = 20; ph.color = new Color32(200, 200, 200, 100); ph.text = "0.0";
        var phRT = (RectTransform)phGO.transform; phRT.anchorMin = new Vector2(0, 0); phRT.anchorMax = new Vector2(1, 1); phRT.offsetMin = new Vector2(12, 6); phRT.offsetMax = new Vector2(-12, -6);

        var input = root.AddComponent<TMP_InputField>();
        input.textViewport = rt; input.textComponent = text; input.placeholder = ph;
        input.characterValidation = TMP_InputField.CharacterValidation.Decimal;

        var le = root.AddComponent<LayoutElement>(); le.preferredHeight = 44; le.minHeight = 40; le.preferredWidth = 160;
        return (input, rt);
    }

    // ---------- Helpers ----------
    public static string ExtractNumber(string mixed)
    {
        if (string.IsNullOrEmpty(mixed)) return string.Empty;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (char c in mixed)
            if ((c >= '0' && c <= '9') || c == '.' || c == ',') sb.Append(c == ',' ? '.' : c);
        return sb.ToString();
    }

    public static float ParseOrDefault(TMP_InputField f, float def)
    {
        if (f == null) return def;
        string num = ExtractNumber(f.text);
        if (float.TryParse(num, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v))
            return v;
        return def;
    }

    // ---------- Dropdown (TMP_Dropdown) ----------
    public static (TMP_Dropdown dd, RectTransform root) Dropdown(
        Transform parent,
        IEnumerable<string> items,
        int defaultIndex,
        Vector2 size)
    {
        var root = new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
        root.transform.SetParent(parent, false);
        var rt = (RectTransform)root.transform;
        rt.sizeDelta = size;
        var bg = root.GetComponent<Image>(); bg.color = FieldBg;

        var dd = root.GetComponent<TMP_Dropdown>();

        // Label
        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(root.transform, false);
        var caption = labelGO.GetComponent<TextMeshProUGUI>();
        caption.font = TMP_Settings.defaultFontAsset; caption.fontSize = 20; caption.color = Text;
        var lrt = (RectTransform)labelGO.transform;
        lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 1);
        lrt.offsetMin = new Vector2(12, 6); lrt.offsetMax = new Vector2(-28, -6);

        // Arrow
        var arrowGO = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
        arrowGO.transform.SetParent(root.transform, false);
        var art = (RectTransform)arrowGO.transform;
        art.anchorMin = new Vector2(1, 0.5f); art.anchorMax = new Vector2(1, 0.5f);
        art.sizeDelta = new Vector2(12, 8); art.anchoredPosition = new Vector2(-10, 0);

        // Template
        var templateGO = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        templateGO.transform.SetParent(root.transform, false);
        var templateRT = (RectTransform)templateGO.transform;
        templateRT.anchorMin = new Vector2(0, 0); templateRT.anchorMax = new Vector2(1, 0);
        templateRT.pivot = new Vector2(0.5f, 1); templateRT.sizeDelta = new Vector2(0, 160);
        templateGO.SetActive(false);
        templateGO.GetComponent<Image>().color = FieldBg;
        var sr = templateGO.GetComponent<ScrollRect>(); sr.horizontal = false;

        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGO.transform.SetParent(templateGO.transform, false);
        var viewportRT = (RectTransform)viewportGO.transform;
        viewportRT.anchorMin = new Vector2(0, 0); viewportRT.anchorMax = new Vector2(1, 1);
        viewportRT.offsetMin = Vector2.zero; viewportRT.offsetMax = Vector2.zero;
        viewportGO.GetComponent<Image>().color = new Color32(0, 0, 0, 0);
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRT = (RectTransform)contentGO.transform;
        contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false; vlg.spacing = 2; vlg.padding = new RectOffset(4, 4, 4, 4);
        contentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = contentRT; sr.viewport = viewportRT;

        // Item
        var itemGO = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
        itemGO.transform.SetParent(contentGO.transform, false);
        var itemRT = (RectTransform)itemGO.transform; itemRT.sizeDelta = new Vector2(0, 28);
        var tgl = itemGO.GetComponent<Toggle>();
        var itemBg = itemGO.AddComponent<Image>(); itemBg.color = new Color32(255, 255, 255, 20);
        tgl.targetGraphic = itemBg;

        var checkGO = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
        checkGO.transform.SetParent(itemGO.transform, false);
        var cmRT = (RectTransform)checkGO.transform;
        cmRT.anchorMin = new Vector2(0, 0.5f); cmRT.anchorMax = new Vector2(0, 0.5f);
        cmRT.anchoredPosition = new Vector2(10, 0); cmRT.sizeDelta = new Vector2(16, 16);
        var cmImg = checkGO.GetComponent<Image>(); cmImg.color = new Color32(80, 160, 255, 255);
        tgl.graphic = cmImg;

        var itemLabelGO = new GameObject("Item Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        itemLabelGO.transform.SetParent(itemGO.transform, false);
        var itemLabel = itemLabelGO.GetComponent<TextMeshProUGUI>();
        itemLabel.font = TMP_Settings.defaultFontAsset; itemLabel.fontSize = 20; itemLabel.color = Text;
        var ilRT = (RectTransform)itemLabelGO.transform;
        ilRT.anchorMin = new Vector2(0, 0); ilRT.anchorMax = new Vector2(1, 1);
        ilRT.offsetMin = new Vector2(32, 4); ilRT.offsetMax = new Vector2(-8, -4);

        // Связка
        dd.template = templateRT;
        dd.captionText = caption;
        dd.itemText = itemLabel;

        // Опции
        dd.options = items.Select(s => new TMP_Dropdown.OptionData(s)).ToList();
        dd.value = Mathf.Clamp(defaultIndex, 0, Mathf.Max(dd.options.Count - 1, 0));
        dd.RefreshShownValue();

        var le = root.AddComponent<LayoutElement>(); le.preferredHeight = 44; le.minHeight = 40;
        return (dd, rt);
    }
}
