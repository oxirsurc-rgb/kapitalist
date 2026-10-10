using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Arayüzün renk paleti
public static class HudTheme
{
    public static readonly Color Bg      = Hex("0D1320");
public static readonly Color Panel   = Hex("151E30");
// EK-FIX: Modal arka planı için tamamen opak panel rengi
public static readonly Color PanelSolid = Hex("101828");   // %100 opak, biraz daha koyu
    public static readonly Color PanelHi = Hex("1E2B44");
    public static readonly Color Line    = Hex("2F3F5E");
    public static readonly Color Gold    = Hex("F2B84B");
    public static readonly Color Dark    = Hex("1A1405");
    public static readonly Color Text    = Hex("E9EEF7");
    public static readonly Color Dim     = Hex("93A1BA");
    public static readonly Color Good    = Hex("4CC38A");
    public static readonly Color Bad     = Hex("E5534B");
    public static readonly Color Warn    = Hex("E8A33D");
    public static readonly Color Info    = Hex("4C9BE8");
    public static readonly Color Action  = Hex("23406B");

    public static Color Hex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString("#" + hex, out c);
        return c;
    }

    public static string Tag(Color c) { return "#" + ColorUtility.ToHtmlStringRGB(c); }
}

// uGUI parçalarını koddan üreten küçük yardımcılar (prefab veya Inspector bağlantısı gerektirmez)
public static class HudKit
{
    static Sprite _round, _pill;
    public static Sprite Round { get { if (_round == null) _round = MakeRounded(32, 12); return _round; } }
    public static Sprite Pill  { get { if (_pill == null)  _pill  = MakeRounded(16, 6);  return _pill; } }

    // Köşeleri yuvarlak, 9-dilimli beyaz sprite üretir (rengi Image.color belirler)
    static Sprite MakeRounded(int size, int radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                float a = Mathf.Clamp01(radius - d + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                             SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    public static RectTransform NewRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static void Fill(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }

    public static void Place(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = oMin;
        rt.offsetMax = oMax;
    }

    public static Image Box(Transform parent, string name, Color color, bool rounded = true, bool pill = false)
    {
        var rt = NewRect(parent, name);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        if (rounded)
        {
            img.sprite = pill ? Pill : Round;
            img.type = Image.Type.Sliced;
        }
        return img;
    }

    public static TextMeshProUGUI Label(Transform parent, string text, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Left, FontStyles style = FontStyles.Normal)
    {
        var rt = NewRect(parent, "Text");
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = UIManager.Clean(text);
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.raycastTarget = false;
        return t;
    }

    public static LayoutElement Size(GameObject go, float prefW = -1, float prefH = -1, float flexW = -1, float flexH = -1, float minH = -1)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (prefW >= 0) le.preferredWidth = prefW;
        if (prefH >= 0) le.preferredHeight = prefH;
        if (flexW >= 0) le.flexibleWidth = flexW;
        if (flexH >= 0) le.flexibleHeight = flexH;
        if (minH >= 0) le.minHeight = minH;
        return le;
    }


      public static VerticalLayoutGroup VStack(GameObject go, float spacing = 8, int pad = 0,
    TextAnchor align = TextAnchor.UpperLeft, bool expandW = true)
{
    if (go == null)
    {
        Debug.LogError("[HudKit] VStack cagrildi ama GameObject null!");
        return null;
    }

    // EK-18: LayoutGroup çakışma kontrolü — Unity aynı GameObject'te iki LayoutGroup'a izin vermiyor
    var existingH = go.GetComponent<HorizontalLayoutGroup>();
    if (existingH != null)
    {
        Debug.LogWarning($"[HudKit] '{go.name}' üzerinde zaten HorizontalLayoutGroup var — VStack atlanıyor.");
        return null;
    }
    var existingV = go.GetComponent<VerticalLayoutGroup>();
    if (existingV != null) return existingV;   // Zaten VStack varsa mevcut olanı döndür

    var v = go.AddComponent<VerticalLayoutGroup>();
    v.spacing = spacing;
    v.padding = new RectOffset(pad, pad, pad, pad);
    v.childAlignment = align;
    v.childControlWidth = true;
    v.childControlHeight = true;
    v.childForceExpandWidth = expandW;
    v.childForceExpandHeight = false;
    return v;
}

   public static HorizontalLayoutGroup HStack(GameObject go, float spacing = 8, int pad = 0,
    TextAnchor align = TextAnchor.MiddleLeft, bool expandW = false, bool expandH = true)
{
    if (go == null)
    {
        Debug.LogError("[HudKit] HStack cagrildi ama GameObject null!");
        return null;
    }

    // EK-18: LayoutGroup çakışma kontrolü
    var existingV = go.GetComponent<VerticalLayoutGroup>();
    if (existingV != null)
    {
        Debug.LogWarning($"[HudKit] '{go.name}' üzerinde zaten VerticalLayoutGroup var — HStack atlanıyor.");
        return null;
    }
    var existingH = go.GetComponent<HorizontalLayoutGroup>();
    if (existingH != null) return existingH;

    var h = go.AddComponent<HorizontalLayoutGroup>();
    h.spacing = spacing;
    h.padding = new RectOffset(pad, pad, pad, pad);
    h.childAlignment = align;
    h.childControlWidth = true;
    h.childControlHeight = true;
    h.childForceExpandWidth = expandW;
    h.childForceExpandHeight = expandH;
    return h;
}

    public static Button MakeButton(Transform parent, string label, Color bg, Color fg, float fontSize,
        Action onClick, float height = 56f)
    {
        var img = Box(parent, "Btn", bg);
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        cb.pressedColor = new Color(0.68f, 0.68f, 0.68f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        var txt = Label(img.transform, label, fontSize, fg, TextAlignmentOptions.Center, FontStyles.Bold);
        Fill(txt.rectTransform, 10, 4, 10, 4);
        Size(img.gameObject, prefH: height, minH: height);

        if (onClick != null)
        {
            btn.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayClick();
                onClick();
            });
        }
        return btn;
    }

    // Yatay doluluk çubuğu
    public static void Bar(Transform parent, float value01, Color color, float height = 10f)
    {
        var track = Box(parent, "Bar", Line_(), true, true);
        track.raycastTarget = false;
        Size(track.gameObject, prefH: height, minH: height);
        var fill = Box(track.transform, "Fill", color, true, true);
        fill.raycastTarget = false;
        var rt = fill.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Color Line_() { return HudTheme.Line; }

    // Dikey kaydırılabilir sayfa. content: içine eleman eklenecek alan
    public static RectTransform ScrollView(Transform parent, string name, out RectTransform content,
        int pad = 16, float spacing = 14)
    {
        var root = NewRect(parent, name);
        var scroll = root.gameObject.AddComponent<ScrollRect>();

        var vp = NewRect(root, "Viewport");
        Fill(vp);
        vp.gameObject.AddComponent<RectMask2D>();
        var vpImg = vp.gameObject.AddComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0); // sürükleme için ışın alır, görünmez

        content = NewRect(vp, "Content");
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        VStack(content.gameObject, spacing, pad);
        var fit = content.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = vp;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35f;
        return root;
    }

    public static void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            var c = t.GetChild(i).gameObject;
            c.SetActive(false); // yerleşimden hemen çık
            UnityEngine.Object.Destroy(c);
        }
    }
}
