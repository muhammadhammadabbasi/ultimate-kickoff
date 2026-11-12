using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Small toolkit to build uGUI screens from code (procedural sprites, text, buttons).</summary>
public static class UIKit
{
    public static readonly Color Bg = new Color(0.03f, 0.05f, 0.11f);
    public static readonly Color Panel = new Color(0.07f, 0.1f, 0.19f, 0.94f);
    public static readonly Color PanelLight = new Color(0.12f, 0.16f, 0.28f, 0.95f);
    public static readonly Color Accent = new Color(0.16f, 0.86f, 0.76f);
    public static readonly Color Gold = new Color(0.98f, 0.78f, 0.32f);
    public static readonly Color Green = new Color(0.16f, 0.78f, 0.42f);
    public static readonly Color Blue = new Color(0.16f, 0.42f, 0.9f);
    public static readonly Color Red = new Color(0.9f, 0.25f, 0.25f);
    public static readonly Color TextDim = new Color(0.72f, 0.78f, 0.9f);

    static Font font;
    static Sprite rounded, roundedSmall, circle, ring, gradient, triangle, glow;

    public static Font Font
    {
        get
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }
    }

    public static Sprite Rounded { get { if (rounded == null) rounded = MakeRounded(96, 30); return rounded; } }
    public static Sprite RoundedSmall { get { if (roundedSmall == null) roundedSmall = MakeRounded(48, 12); return roundedSmall; } }
    public static Sprite Circle { get { if (circle == null) circle = MakeCircle(128, 0f); return circle; } }
    public static Sprite Ring { get { if (ring == null) ring = MakeCircle(128, 0.84f); return ring; } }
    public static Sprite Gradient { get { if (gradient == null) gradient = MakeGradient(); return gradient; } }
    public static Sprite Triangle { get { if (triangle == null) triangle = MakeTriangle(64); return triangle; } }
    public static Sprite Glow { get { if (glow == null) glow = MakeGlow(128); return glow; } }

    // ------------------------------------------------------------------ sprites

    static Sprite MakeRounded(int size, int radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                float a = Mathf.Clamp01(radius - d + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
    }

    static Sprite MakeCircle(int size, float inner)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float a = Mathf.Clamp01(r - d);
                if (inner > 0f) a *= Mathf.Clamp01(d - r * inner);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite MakeGradient()
    {
        var tex = new Texture2D(4, 64, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, y / 63f));
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 64), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite MakeTriangle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
        {
            float halfW = (y + 0.5f) / size * size * 0.5f; // pointing down: narrow at bottom
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - size * 0.5f);
                float a = Mathf.Clamp01(halfW - dx + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite MakeGlow(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // ------------------------------------------------------------------ layout helpers

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    /// <summary>Anchor at a single point (anchor), pivot same as anchor.</summary>
    public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }

    public static Image Img(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
    {
        var rt = Rect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = raycast;
        if (sprite != null && sprite.border.sqrMagnitude > 0f) img.type = Image.Type.Sliced;
        return img;
    }

    public static Image Img(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 size, bool raycast = false)
    {
        var img = Img(parent, name, sprite, color, raycast);
        Place(img.rectTransform, anchor, pos, size);
        return img;
    }

    public static Text Txt(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
    {
        var rt = Rect("Text", parent);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = Font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    public static Text Txt(Transform parent, string text, int size, Color color, Vector2 anchor, Vector2 pos, Vector2 boxSize, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
    {
        var t = Txt(parent, text, size, color, align, style);
        Place(t.rectTransform, anchor, pos, boxSize);
        return t;
    }

    public static Text FillText(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
    {
        var t = Txt(parent, text, size, color, align, style);
        Stretch(t.rectTransform);
        return t;
    }

    public static void Shadowed(Graphic g, float dist = 2f, float alpha = 0.6f)
    {
        var s = g.gameObject.AddComponent<Shadow>();
        s.effectDistance = new Vector2(dist, -dist);
        s.effectColor = new Color(0f, 0f, 0f, alpha);
    }

    public static void Outlined(Graphic g, Color c, float dist = 2f)
    {
        var o = g.gameObject.AddComponent<Outline>();
        o.effectColor = c;
        o.effectDistance = new Vector2(dist, -dist);
    }

    /// <summary>Rounded button with label. Returns the Button.</summary>
    public static Button Btn(Transform parent, string label, Color color, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick, int fontSize = 34, Color? textColor = null)
    {
        var img = Img(parent, "Btn_" + label, Rounded, color, anchor, pos, size, true);
        img.pixelsPerUnitMultiplier = 1.4f;
        var gloss = Img(img.transform, "Gloss", Gradient, new Color(1f, 1f, 1f, 0.18f));
        Stretch(gloss.rectTransform, 4, 4, 4, size.y * 0.45f);
        var btn = img.gameObject.AddComponent<Button>();
        var colors = btn.colors;
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        colors.highlightedColor = Color.white;
        btn.colors = colors;
        btn.targetGraphic = img;
        if (onClick != null) btn.onClick.AddListener(() => onClick());
        var t = FillText(img.transform, label, fontSize, textColor ?? Color.white);
        Shadowed(t, 2f, 0.5f);
        return btn;
    }

    public static Text ButtonText(Button b) { return b.GetComponentInChildren<Text>(); }

    // ------------------------------------------------------------------ cards

    public static Color TierColor(PlayerData p)
    {
        if (p.legend) return new Color(0.93f, 0.86f, 0.62f);
        if (p.ovr >= 90) return new Color(0.42f, 0.28f, 0.85f);
        if (p.ovr >= 86) return new Color(0.88f, 0.62f, 0.16f);
        if (p.ovr >= 80) return new Color(0.82f, 0.72f, 0.38f);
        return new Color(0.68f, 0.71f, 0.76f);
    }

    public static Color TierText(PlayerData p)
    {
        if (p.legend || p.ovr < 90) return new Color(0.12f, 0.1f, 0.06f);
        return Color.white;
    }

    public static string PosName(Pos p) { return p.ToString(); }
}
