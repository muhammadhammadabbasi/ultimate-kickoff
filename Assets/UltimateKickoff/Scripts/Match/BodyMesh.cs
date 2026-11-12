using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural, smooth-shaded body parts for the footballers (lofted tubes with elliptical rings),
/// plus generated shirt / face textures. Meshes are shared between all players.
/// </summary>
public static class BodyMesh
{
    // ------------------------------------------------------------------ tube generator

    /// <summary>
    /// Lofted tube along +Y. Each ring i sits at y[i] with elliptical radii rx[i] (X) / rz[i] (Z).
    /// uOffset decides where u = 0.5 lands: 0.25 puts +Z (front) at u = 0.5, 0.75 puts -Z (back) at u = 0.5.
    /// </summary>
    public static Mesh Tube(float[] y, float[] rx, float[] rz, int seg, float uOffset, float[] zOff = null)
    {
        int n = y.Length;
        int stride = seg + 1;
        var verts = new Vector3[n * stride];
        var norms = new Vector3[n * stride];
        var uvs = new Vector2[n * stride];
        float y0 = y[0], yr = Mathf.Max(0.0001f, y[n - 1] - y[0]);

        for (int i = 0; i < n; i++)
        {
            int a = Mathf.Max(i - 1, 0), b = Mathf.Min(i + 1, n - 1);
            float dy = Mathf.Max(0.0001f, y[b] - y[a]);
            float dRx = (rx[b] - rx[a]) / dy;
            float dRz = (rz[b] - rz[a]) / dy;
            float zo = zOff != null ? zOff[i] : 0f;
            for (int j = 0; j <= seg; j++)
            {
                float th = j * Mathf.PI * 2f / seg;
                float c = Mathf.Cos(th), s = Mathf.Sin(th);
                int k = i * stride + j;
                verts[k] = new Vector3(rx[i] * c, y[i], rz[i] * s + zo);
                Vector3 nrm = new Vector3(rz[i] * c, -(dRz * rx[i] * s * s + dRx * rz[i] * c * c), rx[i] * s);
                norms[k] = nrm.sqrMagnitude > 1e-12f ? nrm.normalized : Vector3.up;
                uvs[k] = new Vector2((float)j / seg + uOffset, (y[i] - y0) / yr);
            }
        }

        var tris = new int[(n - 1) * seg * 6];
        int t = 0;
        for (int i = 0; i < n - 1; i++)
        {
            for (int j = 0; j < seg; j++)
            {
                int a = i * stride + j, b = a + 1, c = a + stride, d = c + 1;
                tris[t++] = a; tris[t++] = c; tris[t++] = b;
                tris[t++] = b; tris[t++] = c; tris[t++] = d;
            }
        }
        var m = new Mesh();
        m.vertices = verts;
        m.normals = norms;
        m.uv = uvs;
        m.triangles = tris;
        m.RecalculateBounds();
        return m;
    }

    /// <summary>Convenience: circular tube from (y, radius) pairs. Ends are closed with a tiny ring.</summary>
    public static Mesh Round(float[] y, float[] r, int seg, float ovalZ = 1f, bool closeTop = true, bool closeBottom = true)
    {
        var yy = new List<float>();
        var rr = new List<float>();
        // Tube expects ascending y.
        if (closeBottom) { yy.Add(y[0] - 0.004f); rr.Add(0.002f); }
        for (int i = 0; i < y.Length; i++) { yy.Add(y[i]); rr.Add(r[i]); }
        if (closeTop) { yy.Add(y[y.Length - 1] + 0.004f); rr.Add(0.002f); }
        var rz = new float[rr.Count];
        for (int i = 0; i < rz.Length; i++) rz[i] = rr[i] * ovalZ;
        return Tube(yy.ToArray(), rr.ToArray(), rz, seg, 0f);
    }

    static float[] Rev(float[] a) { var r = (float[])a.Clone(); System.Array.Reverse(r); return r; }

    // ------------------------------------------------------------------ shared shapes

    static Mesh torso, shorts, neck, head, thigh, shortLeg, shin, sleeve, upperArm, foreArm;
    static readonly Dictionary<int, Mesh> hairCaps = new Dictionary<int, Mesh>();

    public const float HeadYMin = -0.125f, HeadYMax = 0.125f;

    /// <summary>Head profile (egg shape, narrower jaw). Returns radius multiplier 0..1 at local y.</summary>
    static float HeadShape(float y)
    {
        float t = Mathf.Clamp01((y - HeadYMin) / (HeadYMax - HeadYMin));
        float e = Mathf.Sqrt(Mathf.Max(0f, 1f - (2f * t - 1f) * (2f * t - 1f)));
        return e * (0.74f + 0.26f * t);
    }

    public static Mesh Torso
    {
        get
        {
            if (torso != null) return torso;
            // bottom -> top: hem inside the shorts, waist, chest, shoulders, collar
            var y = new[] { -0.100f, 0.000f, 0.120f, 0.260f, 0.400f, 0.500f, 0.570f, 0.610f, 0.640f, 0.655f, 0.660f };
            var rx = new[] { 0.160f, 0.158f, 0.150f, 0.168f, 0.196f, 0.216f, 0.220f, 0.165f, 0.080f, 0.066f, 0.002f };
            var rz = new[] { 0.100f, 0.098f, 0.094f, 0.102f, 0.114f, 0.118f, 0.108f, 0.090f, 0.068f, 0.060f, 0.002f };
            torso = Tube(y, rx, rz, 18, 0.75f);
            return torso;
        }
    }

    public static Mesh Shorts
    {
        get
        {
            if (shorts != null) return shorts;
            var y = new[] { -0.160f, -0.080f, 0.020f, 0.070f, 0.080f };
            var rx = new[] { 0.170f, 0.174f, 0.157f, 0.148f, 0.002f };
            var rz = new[] { 0.112f, 0.114f, 0.100f, 0.092f, 0.002f };
            shorts = Tube(y, rx, rz, 16, 0f);
            return shorts;
        }
    }

    public static Mesh Neck
    {
        get
        {
            if (neck != null) return neck;
            neck = Round(new[] { 0f, 0.05f, 0.12f }, new[] { 0.062f, 0.052f, 0.05f }, 12, 1f, true, false);
            return neck;
        }
    }

    public static Mesh Head
    {
        get
        {
            if (head != null) return head;
            const int n = 14;
            var y = new float[n]; var rx = new float[n]; var rz = new float[n]; var zo = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / (n - 1);
                float yy = Mathf.Lerp(HeadYMin, HeadYMax, t);
                float s = HeadShape(yy);
                y[i] = yy;
                rx[i] = Mathf.Max(0.002f, 0.080f * s);
                rz[i] = Mathf.Max(0.002f, 0.097f * s);
                zo[i] = 0.004f * (1f - t);   // face slightly forward of the skull
            }
            head = Tube(y, rx, rz, 20, 0.25f, zo);
            return head;
        }
    }

    public static Mesh HairCap(int variant)
    {
        variant = Mathf.Abs(variant) % 3;
        Mesh m;
        if (hairCaps.TryGetValue(variant, out m)) return m;
        float grow = variant == 0 ? 1.06f : variant == 1 ? 1.09f : 1.14f;
        float lift = variant == 2 ? 0.02f : 0.006f;
        float[] ys = { 0.050f, 0.066f, 0.084f, 0.102f, 0.116f, 0.124f, 0.128f };
        int n = ys.Length + 1;
        var y = new float[n]; var rx = new float[n]; var rz = new float[n]; var zo = new float[n];
        for (int i = 0; i < ys.Length; i++)
        {
            float s = HeadShape(ys[i]);
            y[i] = ys[i];
            rx[i] = 0.080f * s * grow + 0.003f;
            rz[i] = 0.097f * s * grow + 0.003f;
            zo[i] = -0.004f;
        }
        y[n - 1] = ys[ys.Length - 1] + lift + 0.004f;
        rx[n - 1] = 0.002f; rz[n - 1] = 0.002f; zo[n - 1] = -0.004f;
        m = Tube(y, rx, rz, 20, 0.25f, zo);
        hairCaps[variant] = m;
        return m;
    }

    public static Mesh Thigh
    {
        get
        {
            if (thigh != null) return thigh;
            thigh = Round(Rev(new[] { 0.02f, -0.08f, -0.20f, -0.34f, -0.46f }), Rev(new[] { 0.082f, 0.090f, 0.083f, 0.070f, 0.060f }), 12, 1.05f, false, false);
            return thigh;
        }
    }

    public static Mesh ShortLeg
    {
        get
        {
            if (shortLeg != null) return shortLeg;
            shortLeg = Round(Rev(new[] { 0.03f, -0.06f, -0.15f, -0.27f }), Rev(new[] { 0.096f, 0.104f, 0.106f, 0.102f }), 14, 1.05f, false, true);
            return shortLeg;
        }
    }

    public static Mesh Shin
    {
        get
        {
            if (shin != null) return shin;
            shin = Round(Rev(new[] { 0.012f, -0.10f, -0.20f, -0.34f, -0.43f }), Rev(new[] { 0.066f, 0.078f, 0.070f, 0.052f, 0.048f }), 12, 1.02f, false, false);
            return shin;
        }
    }

    public static Mesh Sleeve
    {
        get
        {
            if (sleeve != null) return sleeve;
            sleeve = Round(Rev(new[] { 0.04f, 0.0f, -0.08f, -0.16f }), Rev(new[] { 0.060f, 0.064f, 0.060f, 0.056f }), 12, 1f, false, true);
            return sleeve;
        }
    }

    public static Mesh UpperArm
    {
        get
        {
            if (upperArm != null) return upperArm;
            upperArm = Round(Rev(new[] { 0.02f, -0.12f, -0.28f }), Rev(new[] { 0.050f, 0.048f, 0.040f }), 10, 1f, false, false);
            return upperArm;
        }
    }

    public static Mesh ForeArm
    {
        get
        {
            if (foreArm != null) return foreArm;
            // forearm with a small hand at the end
            foreArm = Round(Rev(new[] { 0.01f, -0.10f, -0.22f, -0.26f, -0.30f, -0.35f }), Rev(new[] { 0.040f, 0.042f, 0.030f, 0.036f, 0.040f, 0.026f }), 10, 1f, false, true);
            return foreArm;
        }
    }

    // ------------------------------------------------------------------ textures / materials

    static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

    static float Lum(Color c) { return c.r * 0.3f + c.g * 0.59f + c.b * 0.11f; }

    static readonly string[] Digits =
    {
        "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001",
        "111100111001111", "111100111101111", "111001001010010", "111101111101111", "111101111001111"
    };

    public static Material ShirtMaterial(Color main, Color trim, int number, bool keeper)
    {
        Color num = Lum(main) > 0.55f ? new Color(0.08f, 0.1f, 0.2f) : Color.white;
        int style = keeper ? 0 : Mathf.Abs(Mathf.RoundToInt(main.r * 7f + main.g * 13f + main.b * 17f)) % 4;
        string key = "S" + ColorKey(main) + ColorKey(trim) + number + style + (keeper ? "k" : "");
        Material mat;
        if (matCache.TryGetValue(key, out mat) && mat != null) return mat;

        const int W = 128, H = 128;
        var px = new Color32[W * H];
        Color alt = Lum(main) > 0.5f ? main * 0.9f : Color.Lerp(main, Color.white, 0.13f);
        alt.a = 1f;
        var rnd = new System.Random(number * 31 + style);
        for (int yy = 0; yy < H; yy++)
        {
            float v = (float)yy / H;
            for (int xx = 0; xx < W; xx++)
            {
                float u = (float)xx / W;
                Color c = main;
                if (style == 1 && Mathf.FloorToInt(u * 18f) % 2 == 0) c = alt;
                else if (style == 2 && Mathf.FloorToInt(v * 14f) % 2 == 0) c = alt;
                else if (style == 3 && v > 0.42f && v < 0.56f) c = alt;
                if (v > 0.935f) c = trim;                       // collar
                float g = 1f + ((float)rnd.NextDouble() - 0.5f) * 0.05f;
                px[yy * W + xx] = (Color32)new Color(c.r * g, c.g * g, c.b * g, 1f);
            }
        }

        // chest badge (wearer's left chest, front centre is u = 0 / 1)
        FillDisc(px, W, H, 4, 82, 5, trim);
        FillDisc(px, W, H, 4, 82, 3, alt);

        // back number, centred on u = 0.5
        string txt = Mathf.Clamp(number, 0, 99).ToString();
        const int gw = 15, gh = 40, gap = 4;
        int total = txt.Length * gw + (txt.Length - 1) * gap;
        int x0 = W / 2 - total / 2, y0 = 52;
        for (int ci = 0; ci < txt.Length; ci++)
        {
            string g = Digits[txt[ci] - '0'];
            for (int row = 0; row < 5; row++)
                for (int col = 0; col < 3; col++)
                {
                    if (g[row * 3 + col] != '1') continue;
                    int bx = x0 + ci * (gw + gap) + col * (gw / 3);
                    int by = y0 + (4 - row) * (gh / 5);
                    for (int dy = 0; dy < gh / 5; dy++)
                        for (int dx = 0; dx < gw / 3; dx++)
                        {
                            int fx = bx + dx, fy = by + dy;
                            if (fx >= 0 && fx < W && fy >= 0 && fy < H) px[fy * W + fx] = (Color32)num;
                        }
                }
        }

        var tex = new Texture2D(W, H, TextureFormat.RGBA32, true);
        tex.SetPixels32(px);
        tex.Apply(true);
        tex.wrapModeU = TextureWrapMode.Repeat;
        tex.wrapModeV = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        mat = MatLib.Textured(tex, Color.white, Vector2.one, 0.22f);
        matCache[key] = mat;
        return mat;
    }

    static void FillDisc(Color32[] px, int W, int H, int cx, int cy, int r, Color c)
    {
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                if (dx * dx + dy * dy > r * r) continue;
                int x = (cx + dx + W) % W, y = cy + dy;
                if (y < 0 || y >= H) continue;
                px[y * W + x] = (Color32)c;
            }
    }

    public static Material HeadMaterial(Color skin, Color hair, int hairline)
    {
        hairline = Mathf.Abs(hairline) % 3;
        string key = "H" + ColorKey(skin) + ColorKey(hair) + hairline;
        Material mat;
        if (matCache.TryGetValue(key, out mat) && mat != null) return mat;

        const int W = 128, H = 128;
        var px = new Color32[W * H];
        Color skinDark = skin * 0.8f; skinDark.a = 1f;
        Color lips = Color.Lerp(skin, new Color(0.7f, 0.25f, 0.25f), 0.55f);
        float hairV = 0.74f + hairline * 0.015f;
        for (int yy = 0; yy < H; yy++)
        {
            float v = (float)yy / H;
            for (int xx = 0; xx < W; xx++)
            {
                float u = (float)xx / W;
                float d = Mathf.Abs(u - 0.5f);          // 0 = front, 0.5 = back
                Color c = skin;
                bool ear = d > 0.21f && d < 0.29f && v > 0.38f && v < 0.60f;
                if (ear) c = skinDark;
                bool isHair = v > hairV || (d > 0.2f && v > 0.62f) || (d > 0.31f && v > 0.42f);
                if (isHair && !(ear && v < 0.6f)) c = hair;
                if (!isHair)
                {
                    // eyes
                    float ex = Mathf.Abs(d - 0.05f), ey = v - 0.52f;
                    if (d > 0.03f && d < 0.075f && Mathf.Abs(ey) < 0.035f && ex < 0.022f) c = Color.white;
                    if (ex < 0.011f && Mathf.Abs(ey) < 0.02f && d > 0.03f) c = new Color(0.12f, 0.08f, 0.06f);
                    // brows
                    float by = v - 0.62f;
                    if (d > 0.025f && d < 0.085f && Mathf.Abs(by) < 0.014f) c = Color.Lerp(skin, hair, 0.85f);
                    // nose
                    if (d < 0.012f && v > 0.36f && v < 0.5f) c = Color.Lerp(skin, skinDark, 0.5f);
                    // mouth
                    if (d < 0.035f && Mathf.Abs(v - 0.28f) < 0.012f) c = lips;
                }
                px[yy * W + xx] = (Color32)c;
            }
        }
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, true);
        tex.SetPixels32(px);
        tex.Apply(true);
        tex.wrapModeU = TextureWrapMode.Repeat;
        tex.wrapModeV = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        mat = MatLib.Textured(tex, Color.white, Vector2.one, 0.25f);
        matCache[key] = mat;
        return mat;
    }

    static string ColorKey(Color c)
    {
        return Mathf.RoundToInt(c.r * 31f).ToString("x2") + Mathf.RoundToInt(c.g * 31f).ToString("x2") + Mathf.RoundToInt(c.b * 31f).ToString("x2");
    }

    public static Color Contrast(Color shirt, Color other)
    {
        if (Squad.ColorDistance(shirt, other) > 0.35f) return other;
        return Lum(shirt) > 0.5f ? new Color(0.1f, 0.12f, 0.22f) : Color.white;
    }

    public static void ClearCache() { matCache.Clear(); }
}
