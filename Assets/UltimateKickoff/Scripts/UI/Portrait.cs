using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a head-and-shoulders portrait for every player (skin, hair and kit match the 3D player in the match).
/// Drawn with anti-aliased shapes into a small texture and cached per player.
/// </summary>
public static class Portrait
{
    const int N = 112;
    static readonly Dictionary<int, Sprite> cache = new Dictionary<int, Sprite>();

    public static Sprite Get(PlayerData p)
    {
        Sprite s;
        if (cache.TryGetValue(p.id, out s) && s != null) return s;
        s = Build(p);
        cache[p.id] = s;
        return s;
    }

    static Color KitOf(PlayerData p)
    {
        if (p.legend) return new Color(0.93f, 0.93f, 0.9f);
        if (!string.IsNullOrEmpty(p.club))
        {
            var club = PlayerDatabase.Clubs.Find(c => c.abbr == p.club || c.name == p.club);
            if (club != null) return club.kit;
        }
        return new Color(0.25f, 0.3f, 0.4f);
    }

    // ---- tiny signed-distance helpers (coordinates: x,y in -1..1, y up) ----

    static float Ellipse(float x, float y, float cx, float cy, float rx, float ry)
    {
        float dx = (x - cx) / rx, dy = (y - cy) / ry;
        return (Mathf.Sqrt(dx * dx + dy * dy) - 1f) * Mathf.Min(rx, ry);
    }

    static float Box(float x, float y, float cx, float cy, float hx, float hy, float round)
    {
        float dx = Mathf.Abs(x - cx) - hx + round, dy = Mathf.Abs(y - cy) - hy + round;
        float ox = Mathf.Max(dx, 0f), oy = Mathf.Max(dy, 0f);
        return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(dx, dy), 0f) - round;
    }

    const float AA = 2f / N;

    static float Cov(float d) { return Mathf.Clamp01(0.5f - d / AA); }

    static void Over(ref Color dst, Color src, float a)
    {
        a *= src.a;
        if (a <= 0f) return;
        float oa = a + dst.a * (1f - a);
        dst.r = (src.r * a + dst.r * dst.a * (1f - a)) / oa;
        dst.g = (src.g * a + dst.g * dst.a * (1f - a)) / oa;
        dst.b = (src.b * a + dst.b * dst.a * (1f - a)) / oa;
        dst.a = oa;
    }

    static Color Mul(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, c.a); }

    static Sprite Build(PlayerData p)
    {
        int h = p.Hash;
        Color skin = HumanoidRig.SkinTones[h % HumanoidRig.SkinTones.Length];
        Color hair = HumanoidRig.HairFor(h);
        Color kit = KitOf(p);
        Color trim = kit.r * 0.3f + kit.g * 0.59f + kit.b * 0.11f > 0.55f ? new Color(0.1f, 0.12f, 0.2f) : Color.white;
        if (p.IsGK) { kit = new Color(0.95f, 0.8f, 0.1f); trim = new Color(0.1f, 0.1f, 0.12f); }
        int style = (h / 3) % 5;          // 0 crop, 1 quiff, 2 buzz, 3 fuller, 4 bald
        bool beard = (h / 11) % 3 == 0;
        float faceW = 0.39f + ((h / 17) % 5) * 0.008f;
        Color skinShade = Mul(skin, 0.78f);
        Color lip = Color.Lerp(skin, new Color(0.62f, 0.22f, 0.2f), 0.5f);
        Color eye = new Color(0.16f, 0.1f, 0.07f);
        if ((h / 5) % 6 == 0) eye = new Color(0.2f, 0.32f, 0.45f);

        var px = new Color32[N * N];
        for (int iy = 0; iy < N; iy++)
        {
            float y = ((iy + 0.5f) / N) * 2f - 1f;
            for (int ix = 0; ix < N; ix++)
            {
                float x = ((ix + 0.5f) / N) * 2f - 1f;
                Color c = new Color(0f, 0f, 0f, 0f);
                float ax = Mathf.Abs(x);

                // shoulders / shirt
                float sh = Ellipse(x, y, 0f, -1.32f, 0.98f, 0.70f);
                float shade = 1f - 0.22f * Mathf.Clamp01(ax / 0.95f) + 0.08f * Mathf.Clamp01(-x);
                Over(ref c, Mul(kit, shade), Cov(sh));
                // collar
                float col = Mathf.Abs(Ellipse(x, y, 0f, -0.60f, 0.27f, 0.17f)) - 0.035f;
                if (y < -0.56f) Over(ref c, trim, Cov(col) * Cov(sh));

                // neck
                float nk = Box(x, y, 0f, -0.52f, 0.17f, 0.22f, 0.08f);
                Over(ref c, skinShade, Cov(nk));

                // ears
                float er = Ellipse(ax, y, faceW + 0.015f, 0.08f, 0.065f, 0.115f);
                Over(ref c, skinShade, Cov(er));

                // head (jaw narrows towards the chin)
                float jaw = y < 0.1f ? 1f - 0.30f * Mathf.Pow(Mathf.Clamp01((0.1f - y) / 0.5f), 1.6f) : 1f;
                float hd = Ellipse(x, y, 0f, 0.12f, faceW * jaw, 0.50f);
                float hc = Cov(hd);
                float light = 1f + 0.10f * Mathf.Clamp01(-x / faceW) - 0.10f * Mathf.Clamp01(x / faceW) - 0.06f * Mathf.Clamp01((0f - y) / 0.4f);
                Over(ref c, Mul(skin, light), hc);

                if (hc > 0f)
                {
                    // beard
                    if (beard)
                    {
                        float bd = Mathf.Max(hd + 0.0f, y + 0.02f);
                        float mouthGap = Ellipse(x, y, 0f, -0.15f, 0.15f, 0.075f);
                        float ba = Cov(bd) * Mathf.Clamp01(mouthGap / 0.03f) * Mathf.Clamp01((-0.02f - y) / 0.12f + 0.3f);
                        Over(ref c, hair, ba * 0.8f * hc);
                    }
                    // eyes
                    float ew = Ellipse(ax, y, 0.155f, 0.13f, 0.078f, 0.046f);
                    Over(ref c, new Color(0.97f, 0.97f, 0.95f), Cov(ew) * hc);
                    float ir = Ellipse(ax, y, 0.155f, 0.13f, 0.04f, 0.044f);
                    Over(ref c, eye, Cov(ir) * Cov(ew + 0.004f) * hc);
                    float lid = Box(ax, y, 0.155f, 0.178f, 0.085f, 0.008f, 0.008f);
                    Over(ref c, Mul(skin, 0.6f), Cov(lid) * 0.7f * hc);
                    // brows
                    float bw = Box(ax, y - (0.03f * (1f - Mathf.Abs(ax - 0.16f) / 0.1f)), 0.16f, 0.235f, 0.095f, 0.02f, 0.02f);
                    Over(ref c, Color.Lerp(hair, Color.black, 0.3f), Cov(bw) * 0.9f * hc);
                    // nose
                    float ns = Ellipse(x, y, 0.035f, -0.01f, 0.03f, 0.10f);
                    Over(ref c, skinShade, Cov(ns) * 0.45f * hc);
                    float nt = Ellipse(x, y, 0f, -0.075f, 0.075f, 0.03f);
                    Over(ref c, skinShade, Cov(nt) * 0.5f * hc);
                    // mouth
                    float mo = Ellipse(x, y, 0f, -0.185f, 0.125f, 0.024f);
                    Over(ref c, lip, Cov(mo) * hc);
                    float ml = Box(x, y, 0f, -0.185f, 0.115f, 0.004f, 0.004f);
                    Over(ref c, Mul(lip, 0.55f), Cov(ml) * hc);
                }

                // hair on top
                if (style != 4)
                {
                    float topLift = style == 1 ? 0.07f : style == 2 ? 0.0f : style == 3 ? 0.05f : 0.03f;
                    float grow = style == 2 ? 0.012f : 0.045f;
                    float cap = Ellipse(x, y, 0f, 0.12f + topLift * 0.5f, faceW + grow, 0.50f + grow + topLift);
                    // hairline: higher in the middle, comes down at the temples
                    float line = (style == 2 ? 0.40f : 0.36f) - 0.20f * Mathf.Pow(Mathf.Clamp01(ax / faceW), 2.2f) + (style == 1 ? 0.03f * Mathf.Sin(x * 9f) : 0f);
                    float sideburn = style == 3 ? 0.04f : 0.10f;
                    if (ax > faceW * 0.86f) line = Mathf.Min(line, sideburn);
                    float ha = Cov(cap) * Mathf.Clamp01((y - line) / AA + 0.5f);
                    Color hc2 = Mul(hair, 1f + 0.25f * Mathf.Clamp01(-x) + 0.15f * Mathf.Clamp01((y - 0.45f) / 0.2f));
                    Over(ref c, hc2, ha * (style == 2 ? 0.75f : 1f));
                }

                px[iy * N + ix] = (Color32)c;
            }
        }

        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply(false, true);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0f), 100f);
    }
}
