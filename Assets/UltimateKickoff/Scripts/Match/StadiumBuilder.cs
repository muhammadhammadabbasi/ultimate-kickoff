using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Builds the stadium (pitch, lines, goals, boards, stands, crowd, floodlights) at runtime.</summary>
public static class StadiumBuilder
{
    public static Transform Build()
    {
        var root = new GameObject("Stadium").transform;

        SetupLighting();

        // Outer ground
        var outer = GameObject.CreatePrimitive(PrimitiveType.Plane);
        outer.name = "Ground";
        Object.Destroy(outer.GetComponent<Collider>());
        outer.transform.SetParent(root);
        outer.transform.position = new Vector3(0f, -0.02f, 0f);
        outer.transform.localScale = new Vector3(26f, 1f, 20f);
        outer.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(new Color(0.10f, 0.25f, 0.09f), 0.03f);

        // Pitch with mowing stripes (includes run-off area)
        var pitch = GameObject.CreatePrimitive(PrimitiveType.Plane);
        pitch.name = "Pitch";
        pitch.transform.SetParent(root);
        pitch.transform.position = Vector3.zero;
        pitch.transform.localScale = new Vector3((Pitch.L + 10f) / 10f, 1f, (Pitch.W + 8f) / 10f);
        var grassMat = MatLib.Grass(GrassTexture(), GrassDetailTexture(), new Vector2(totalDetailTiles(Pitch.L + 10f), totalDetailTiles(Pitch.W + 8f)));
        pitch.GetComponent<Renderer>().sharedMaterial = grassMat;
        pitch.GetComponent<Renderer>().receiveShadows = true;

        BuildLines(root);
        BuildGoal(root, 1f);
        BuildGoal(root, -1f);
        BuildBoards(root);
        BuildStands(root);
        BuildFloodlights(root);
        BuildCornerFlags(root);
        BuildDugouts(root);
        return root;
    }

    static float totalDetailTiles(float metres) { return metres / 2.2f; }

    static void SetupLighting()
    {
        var sun = Object.FindFirstObjectByType<Light>();
        if (sun == null)
        {
            var go = new GameObject("Sun");
            sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
        sun.intensity = 1.15f;
        sun.color = new Color(1f, 0.97f, 0.9f);
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.7f;
        sun.shadowBias = 0.03f;
        sun.shadowNormalBias = 0.3f;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.8f);
        RenderSettings.ambientEquatorColor = new Color(0.46f, 0.5f, 0.5f);
        RenderSettings.ambientGroundColor = new Color(0.24f, 0.26f, 0.22f);
        RenderSettings.fog = false;

        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.shadowDistance = 75f;
        QualitySettings.shadowCascades = 2;
        QualitySettings.antiAliasing = 4;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
    }

    // ------------------------------------------------------------------ textures

    static Texture2D GrassTexture()
    {
        const int w = 2048, h = 1024;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float totalL = Pitch.L + 10f, totalW = Pitch.W + 8f;
        Color light = new Color(0.25f, 0.54f, 0.20f);
        Color dark = new Color(0.17f, 0.42f, 0.14f);
        Color edge = new Color(0.11f, 0.29f, 0.10f);
        Color worn = new Color(0.36f, 0.42f, 0.18f);
        var rnd = new System.Random(7);
        var px = new Color32[w * h];
        const float stripeLen = Pitch.L / 20f;

        // Per-column stripe colour (depends on x only)
        var colX = new Color[w];
        var wxs = new float[w];
        for (int x = 0; x < w; x++)
        {
            float wx = ((float)x / w - 0.5f) * totalL;
            wxs[x] = wx;
            float sp = (wx + Pitch.HalfL) / stripeLen;
            float fract = sp - Mathf.Floor(sp);
            float edgeSoft = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fract / 0.05f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - fract) / 0.05f));
            bool even = ((int)Mathf.Floor(sp) & 1) == 0;
            Color c = even ? light : dark;
            c *= 0.96f + 0.08f * Mathf.Abs(fract - 0.5f) * 2f * (even ? 1f : -1f);
            colX[x] = Color.Lerp(Color.Lerp(light, dark, 0.5f), c, edgeSoft * 0.3f + 0.7f);
        }

        // Low-frequency noise on a coarse grid (cheap), sampled with nearest filtering
        const int gw = 512, gh = 256;
        var grid = new float[gw * gh];
        for (int gy = 0; gy < gh; gy++)
        {
            float gz = ((float)gy / gh - 0.5f) * totalW;
            for (int gx = 0; gx < gw; gx++)
            {
                float gwx = ((float)gx / gw - 0.5f) * totalL;
                grid[gy * gw + gx] = (Mathf.PerlinNoise(gwx * 0.07f + 11f, gz * 0.07f + 3f) - 0.5f) * 0.09f
                                   + (Mathf.PerlinNoise(gwx * 0.9f, gz * 0.9f) - 0.5f) * 0.05f;
            }
        }

        for (int y = 0; y < h; y++)
        {
            float wz = ((float)y / h - 0.5f) * totalW;
            float az = Mathf.Abs(wz);
            float oz = Mathf.Max(0f, az - Pitch.HalfW - 1.5f);
            float goalZ = Mathf.Max(0f, 1f - az / 9f);
            int gyi = Mathf.Min(gh - 1, y * gh / h);
            for (int x = 0; x < w; x++)
            {
                float wx = wxs[x];
                Color c = colX[x];
                float n = grid[gyi * gw + Mathf.Min(gw - 1, x * gw / w)] + ((float)rnd.NextDouble() - 0.5f) * 0.045f;
                c = new Color(c.r + n * 0.8f, c.g + n * 1.2f, c.b + n * 0.6f, 1f);

                // worn goalmouths and centre spot
                float ax = Mathf.Abs(wx);
                float gm = Mathf.Max(0f, 1f - Mathf.Min(Mathf.Abs(ax - Pitch.HalfL + 6f) / 9f, 1f)) * goalZ;
                float cs = Mathf.Max(0f, 1f - Mathf.Sqrt(wx * wx + wz * wz) / 6f);
                float wearAmt = gm * 0.35f + cs * 0.15f;
                if (wearAmt > 0.001f) c = Color.Lerp(c, worn, Mathf.Clamp01(wearAmt));

                // fade to dark outside the touchlines so it blends with the surrounding ground
                float ox = Mathf.Max(0f, ax - Pitch.HalfL - 1.5f);
                if (ox > 0f || oz > 0f)
                {
                    float outside = Mathf.Clamp01(Mathf.Sqrt(ox * ox + oz * oz) / 3f);
                    c = Color.Lerp(c, edge, outside);
                }
                px[y * w + x] = (Color32)c;
            }
        }
        tex.SetPixels32(px);
        tex.Apply(true);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 8;
        return tex;
    }

    /// <summary>Tileable grass-blade detail (mid grey = neutral) used as the Standard shader detail map.</summary>
    static Texture2D GrassDetailTexture()
    {
        const int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, true);
        var rnd = new System.Random(21);
        var px = new float[n * n];
        for (int i = 0; i < px.Length; i++) px[i] = 0.5f;
        // short vertical blade strokes, wrapped so the tile is seamless
        for (int k = 0; k < 5200; k++)
        {
            int bx = rnd.Next(n), by = rnd.Next(n);
            int len = 3 + rnd.Next(6);
            float v = (float)rnd.NextDouble() < 0.5f ? 0.38f : 0.62f;
            float lean = ((float)rnd.NextDouble() - 0.5f) * 0.6f;
            for (int j = 0; j < len; j++)
            {
                int xx = ((bx + Mathf.RoundToInt(lean * j)) % n + n) % n;
                int yy = (by + j) % n;
                px[yy * n + xx] = Mathf.Lerp(px[yy * n + xx], v, 0.8f);
            }
        }
        var cols = new Color32[n * n];
        for (int i = 0; i < cols.Length; i++)
        {
            float g = Mathf.Clamp01(px[i] + ((float)rnd.NextDouble() - 0.5f) * 0.06f);
            cols[i] = (Color32)new Color(g, g, g, 1f);
        }
        tex.SetPixels32(cols);
        tex.Apply(true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        tex.anisoLevel = 4;
        return tex;
    }

    static Texture2D CrowdTexture()
    {
        const int w = 256, h = 128;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        var rnd = new System.Random(11);
        Color[] shirts =
        {
            new Color(0.85f, 0.2f, 0.2f), new Color(0.2f, 0.3f, 0.8f), new Color(0.95f, 0.95f, 0.95f),
            new Color(0.95f, 0.8f, 0.2f), new Color(0.15f, 0.15f, 0.18f), new Color(0.3f, 0.65f, 0.35f),
            new Color(0.6f, 0.3f, 0.7f), new Color(0.9f, 0.5f, 0.2f)
        };
        Color[] skins = { new Color(0.95f, 0.8f, 0.68f), new Color(0.8f, 0.6f, 0.45f), new Color(0.55f, 0.38f, 0.26f), new Color(0.35f, 0.24f, 0.16f) };
        var px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(0.2f, 0.22f, 0.27f);
        for (int row = 0; row < h / 8; row++)
        {
            for (int col = 0; col < w / 6; col++)
            {
                if (col % 14 == 13 || rnd.NextDouble() < 0.06) continue;   // stairways + a few empty seats
                Color shirt = shirts[rnd.Next(shirts.Length)];
                Color skin = skins[rnd.Next(skins.Length)];
                int bx = col * 6 + rnd.Next(2), by = row * 8 + rnd.Next(2);
                for (int y = 0; y < 7; y++)
                    for (int x = 0; x < 5; x++)
                    {
                        int xx = bx + x, yy = by + y;
                        if (xx >= w || yy >= h) continue;
                        bool head = y >= 4 && x >= 1 && x <= 3;
                        bool body = y < 4;
                        if (head) px[yy * w + xx] = skin;
                        else if (body) px[yy * w + xx] = shirt * (0.8f + 0.2f * (float)rnd.NextDouble());
                    }
            }
        }
        tex.SetPixels(px);
        tex.Apply(true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    public static Texture2D NetTexture()
    {
        const int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, true);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                bool line = (x % 16) < 2 || (y % 16) < 2;
                tex.SetPixel(x, y, line ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        tex.Apply(true);
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    public static Texture2D BallTexture()
    {
        const int w = 256, h = 128;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        Vector3[] ico =
        {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
        };
        for (int i = 0; i < ico.Length; i++) ico[i].Normalize();
        for (int y = 0; y < h; y++)
        {
            float lat = ((float)y / (h - 1) - 0.5f) * Mathf.PI;
            for (int x = 0; x < w; x++)
            {
                float lon = (float)x / w * Mathf.PI * 2f;
                Vector3 d = new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon));
                float best = -1f;
                foreach (var c in ico) best = Mathf.Max(best, Vector3.Dot(d, c));
                Color col = best > 0.94f ? new Color(0.08f, 0.08f, 0.1f) : Color.white;
                if (best > 0.925f && best <= 0.94f) col = new Color(0.4f, 0.4f, 0.45f);
                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply(true);
        return tex;
    }

    // ------------------------------------------------------------------ geometry

    static void BuildLines(Transform root)
    {
        const float lw = 0.12f;
        var b = new MeshUtil.FlatBuilder(0.012f);
        float hl = Pitch.HalfL, hw = Pitch.HalfW;
        b.Rect(-hl, -hw, hl, hw, lw);
        b.Segment(new Vector3(0, 0, -hw), new Vector3(0, 0, hw), lw);
        b.Arc(Vector3.zero, 9.15f, 0f, 360f, lw, 64);
        b.Dot(Vector3.zero, 0.35f);
        foreach (float s in new[] { -1f, 1f })
        {
            float gx = s * hl;
            b.Rect(gx, -Pitch.BoxHalfWidth, gx - s * Pitch.BoxDepth, Pitch.BoxHalfWidth, lw);
            b.Rect(gx, -9.16f, gx - s * 5.5f, 9.16f, lw);
            Vector3 spot = new Vector3(gx - s * 11f, 0, 0);
            b.Dot(spot, 0.3f);
            // Penalty arc (outside the box only)
            float half = Mathf.Acos(5.5f / 9.15f) * Mathf.Rad2Deg;
            float baseAngle = s > 0 ? 180f : 0f;
            b.Arc(spot, 9.15f, baseAngle - half, baseAngle + half, lw, 16);
            // Corner arcs
            b.Arc(new Vector3(gx, 0, -hw), 1f, s > 0 ? 90f : 0f, s > 0 ? 180f : 90f, lw, 6);
            b.Arc(new Vector3(gx, 0, hw), 1f, s > 0 ? 180f : 270f, s > 0 ? 270f : 360f, lw, 6);
        }
        var go = new GameObject("Lines");
        go.transform.SetParent(root);
        go.AddComponent<MeshFilter>().sharedMesh = b.Build();
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = MatLib.Lit(new Color(0.96f, 0.96f, 0.96f), 0.1f);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static void BuildGoal(Transform root, float sign)
    {
        var g = new GameObject(sign > 0 ? "Goal_East" : "Goal_West").transform;
        g.SetParent(root);
        float x = sign * Pitch.HalfL;
        float back = x + sign * Pitch.GoalDepth;
        float hw = Pitch.GoalHalfWidth, h = Pitch.GoalHeight;
        var white = MatLib.Lit(Color.white, 0.6f);
        const float t = 0.12f;

        Bar(g, new Vector3(x, 0, -hw), new Vector3(x, h, -hw), t, white, true);
        Bar(g, new Vector3(x, 0, hw), new Vector3(x, h, hw), t, white, true);
        Bar(g, new Vector3(x, h, -hw), new Vector3(x, h, hw), t, white, true);
        Bar(g, new Vector3(back, 0, -hw), new Vector3(back, h * 0.75f, -hw), t * 0.5f, white, false);
        Bar(g, new Vector3(back, 0, hw), new Vector3(back, h * 0.75f, hw), t * 0.5f, white, false);
        Bar(g, new Vector3(x, h, -hw), new Vector3(back, h * 0.75f, -hw), t * 0.5f, white, false);
        Bar(g, new Vector3(x, h, hw), new Vector3(back, h * 0.75f, hw), t * 0.5f, white, false);
        Bar(g, new Vector3(back, h * 0.75f, -hw), new Vector3(back, h * 0.75f, hw), t * 0.5f, white, false);

        var netMat = MatLib.Cutout(NetTexture(), new Color(0.95f, 0.95f, 0.95f), new Vector2(8f, 3f));
        var quad = MeshUtil.DoubleQuad();
        float midX = (x + back) / 2f;
        float topLen = Vector2.Distance(new Vector2(x, h), new Vector2(back, h * 0.75f));
        Net(g, quad, netMat, new Vector3(back, h * 0.375f, 0f), Quaternion.Euler(0, 90, 0), new Vector3(hw * 2f, h * 0.75f, 1f));
        Net(g, quad, netMat, new Vector3(midX, h * 0.44f, -hw), Quaternion.identity, new Vector3(Pitch.GoalDepth, h * 0.88f, 1f));
        Net(g, quad, netMat, new Vector3(midX, h * 0.44f, hw), Quaternion.identity, new Vector3(Pitch.GoalDepth, h * 0.88f, 1f));
        float tilt = Mathf.Atan2(h * 0.25f, Pitch.GoalDepth) * Mathf.Rad2Deg;
        Net(g, quad, netMat, new Vector3(midX, h * 0.875f, 0f), Quaternion.AngleAxis(-sign * tilt, Vector3.forward) * Quaternion.Euler(90f, 0f, 0f), new Vector3(topLen, hw * 2f, 1f));

        // Invisible net colliders
        Wall(g, new Vector3(back + sign * 0.1f, h / 2f, 0f), new Vector3(0.2f, h, hw * 2f + 0.4f));
        Wall(g, new Vector3(midX, h / 2f, -hw - 0.1f), new Vector3(Pitch.GoalDepth, h, 0.2f));
        Wall(g, new Vector3(midX, h / 2f, hw + 0.1f), new Vector3(Pitch.GoalDepth, h, 0.2f));
        Wall(g, new Vector3(midX, h + 0.1f, 0f), new Vector3(Pitch.GoalDepth, 0.2f, hw * 2f));
    }

    static void Bar(Transform parent, Vector3 a, Vector3 b, float thickness, Material m, bool collider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Bar";
        if (!collider) Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent);
        Vector3 dir = b - a;
        go.transform.position = (a + b) / 2f;
        go.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
        go.transform.localScale = new Vector3(thickness, dir.magnitude / 2f + thickness / 2f, thickness);
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    static void Net(Transform parent, Mesh quad, Material m, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        var go = new GameObject("Net");
        go.transform.SetParent(parent);
        go.transform.SetPositionAndRotation(pos, rot);
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = quad;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static void Wall(Transform parent, Vector3 pos, Vector3 size)
    {
        var go = new GameObject("NetCollider");
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.AddComponent<BoxCollider>().size = size;
    }

    static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material m, bool shadows = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent);
        go.transform.position = center;
        go.transform.localScale = size;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    static void BuildBoards(Transform root)
    {
        var parent = new GameObject("AdBoards").transform;
        parent.SetParent(root);
        float bx = Pitch.HalfL + 5f, bz = Pitch.HalfW + 4f;
        var boardMat = MatLib.Lit(new Color(0.05f, 0.09f, 0.22f), 0.5f);
        Box(parent, "Board_N", new Vector3(0, 0.5f, bz), new Vector3(bx * 2f, 1f, 0.2f), boardMat);
        Box(parent, "Board_S", new Vector3(0, 0.5f, -bz), new Vector3(bx * 2f, 1f, 0.2f), boardMat);
        Box(parent, "Board_E", new Vector3(bx, 0.5f, 0), new Vector3(0.2f, 1f, bz * 2f), boardMat);
        Box(parent, "Board_W", new Vector3(-bx, 0.5f, 0), new Vector3(0.2f, 1f, bz * 2f), boardMat);

        // Text on the far-side board (the one the broadcast camera sees)
        var canvasGo = new GameObject("BoardText", typeof(RectTransform), typeof(Canvas));
        canvasGo.transform.SetParent(parent);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)canvasGo.transform;
        rt.sizeDelta = new Vector2(bx * 200f, 100f);
        rt.position = new Vector3(0f, 0.5f, bz - 0.11f);
        rt.rotation = Quaternion.identity;
        rt.localScale = Vector3.one * 0.01f;
        string[] words = { "ULTIMATE KICKOFF", "HAMMAD GAMES", "ACADEMY GROUND", "ULTIMATE KICKOFF", "WORLD LEGENDS", "HAMMAD GAMES" };
        int n = 10;
        for (int i = 0; i < n; i++)
        {
            var t = UIKit.Txt(rt, words[i % words.Length], 64, i % 2 == 0 ? new Color(1f, 0.85f, 0.3f) : Color.white);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2((i + 0.5f) / n, 0.5f);
            t.rectTransform.sizeDelta = new Vector2(bx * 200f / n, 100f);
            t.rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    // ------------------------------------------------------------------ bowl stadium ("Academy Ground")

    const float RingA = Pitch.HalfL + 12f;   // inner edge of the stands (half length)
    const float RingB = Pitch.HalfW + 10f;   // inner edge of the stands (half width)
    const int RingSeg = 128;

    /// <summary>Point on the stadium ring: angle t around the pitch, d metres outward from the inner edge, h metres up.</summary>
    static Vector3 RingPoint(float t, float d, float h)
    {
        float c = Mathf.Cos(t), s = Mathf.Sin(t);
        float ac = Mathf.Abs(c), asn = Mathf.Abs(s);
        // rounded rectangle (superellipse, exponent 8)
        float x = RingA * Mathf.Sign(c) * Mathf.Pow(ac, 0.25f);
        float z = RingB * Mathf.Sign(s) * Mathf.Pow(asn, 0.25f);
        float nx = Mathf.Sign(c) * Mathf.Pow(ac, 1.75f) / RingA;
        float nz = Mathf.Sign(s) * Mathf.Pow(asn, 1.75f) / RingB;
        float nl = Mathf.Sqrt(nx * nx + nz * nz);
        if (nl < 1e-6f) { nx = c; nz = s; nl = 1f; }
        return new Vector3(x + nx / nl * d, h, z + nz / nl * d);
    }

    /// <summary>Collects ring strips for one material. Faces point towards the pitch only, so the camera can look in from outside.</summary>
    class RingMesh
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> tri = new List<int>();

        /// <param name="p0">(distance out, height) of the lower/inner edge</param>
        /// <param name="p1">(distance out, height) of the other edge</param>
        public void Strip(Vector2 p0, Vector2 p1, float uTile, float vRepeat)
        {
            int start = v.Count;
            float len0 = 0f, len1 = 0f;
            Vector3 prevA = Vector3.zero, prevB = Vector3.zero;
            Vector2 mid = (p0 + p1) * 0.5f;
            for (int j = 0; j <= RingSeg; j++)
            {
                float t = (float)j / RingSeg * Mathf.PI * 2f;
                Vector3 a = RingPoint(t, p0.x, p0.y), b = RingPoint(t, p1.x, p1.y);
                if (j > 0) { len0 += Vector3.Distance(a, prevA); len1 += Vector3.Distance(b, prevB); }
                prevA = a; prevB = b;
                Vector3 tan = RingPoint(t + 0.004f, mid.x, mid.y) - RingPoint(t - 0.004f, mid.x, mid.y);
                Vector3 nn = Vector3.Cross(tan, b - a).normalized;
                v.Add(a); v.Add(b);
                n.Add(nn); n.Add(nn);
                uv.Add(new Vector2(len0 / uTile, 0f));
                uv.Add(new Vector2(len1 / uTile, vRepeat));
            }
            for (int j = 0; j < RingSeg; j++)
            {
                int A = start + j * 2, C = A + 1, B = A + 2, D = A + 3;
                tri.Add(A); tri.Add(B); tri.Add(C);
                tri.Add(B); tri.Add(D); tri.Add(C);
            }
        }

        public void Create(Transform parent, string name, Material mat)
        {
            var m = new Mesh();
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }

    static void BuildStands(Transform root)
    {
        var parent = new GameObject("Stands").transform;
        parent.SetParent(root);

        var crowdTex = CrowdTexture();
        var crowdMat = MatLib.Textured(crowdTex, Color.white, Vector2.one, 0.02f);
        var concrete = MatLib.Lit(new Color(0.36f, 0.38f, 0.42f), 0.05f);
        var darkWall = MatLib.Lit(new Color(0.06f, 0.08f, 0.16f), 0.3f);
        var ribbon = MatLib.Lit(new Color(0.12f, 0.45f, 0.95f), 0.6f);
        var roofMat = MatLib.Lit(new Color(0.78f, 0.8f, 0.84f), 0.25f);
        var roofEdge = MatLib.Lit(new Color(0.97f, 0.97f, 1f), 0.5f);
        var lamps = MatLib.Lit(new Color(1f, 1f, 0.9f), 0.9f);

        // Cross-section (distance outward, height)
        var wallTop = new Vector2(0f, 2.6f);
        var lowerTop = new Vector2(17f, 12.6f);
        var balconyTop = new Vector2(17f, 15.4f);
        var upperTop = new Vector2(37f, 32f);
        var backTop = new Vector2(37f, 39f);
        var roofBack = new Vector2(38f, 39.5f);
        var roofFront = new Vector2(9f, 34.5f);

        var crowd = new RingMesh();
        crowd.Strip(new Vector2(0.4f, 2.6f), lowerTop, 13f, Vector2.Distance(wallTop, lowerTop) / 8f);
        crowd.Strip(new Vector2(17.4f, 15.4f), upperTop, 13f, Vector2.Distance(balconyTop, upperTop) / 8f);
        crowd.Create(parent, "Crowd", crowdMat);

        var walls = new RingMesh();
        walls.Strip(Vector2.zero, wallTop, 10f, 1f);
        walls.Strip(wallTop, new Vector2(0.4f, 2.6f), 10f, 1f);
        walls.Create(parent, "PitchWall", darkWall);

        var conc = new RingMesh();
        conc.Strip(upperTop, backTop, 10f, 1f);
        conc.Strip(lowerTop + new Vector2(0f, 1.1f), balconyTop, 10f, 1f);
        conc.Strip(balconyTop, new Vector2(17.4f, 15.4f), 10f, 1f);
        conc.Create(parent, "Concrete", concrete);

        var led = new RingMesh();
        led.Strip(lowerTop, lowerTop + new Vector2(0f, 1.1f), 10f, 1f);
        led.Create(parent, "LedRibbon", ribbon);

        var roof = new RingMesh();
        roof.Strip(roofBack, roofFront, 10f, 1f);
        roof.Create(parent, "Roof", roofMat);

        var edge = new RingMesh();
        edge.Strip(roofFront, roofFront + new Vector2(0f, 1.3f), 10f, 1f);
        edge.Create(parent, "RoofEdge", roofEdge);

        // Floodlight bars hanging under the roof edge
        var lampMesh = new RingMesh();
        lampMesh.Strip(roofFront + new Vector2(1.2f, -0.6f), roofFront + new Vector2(1.2f, 0.1f), 10f, 1f);
        lampMesh.Create(parent, "RoofLights", lamps);

        // Giant screens behind each goal
        foreach (float sx in new[] { -1f, 1f })
        {
            Vector3 pos = new Vector3(sx * (RingA + 19f), 19f, 0f);
            var frame = Box(parent, "ScreenFrame", pos, new Vector3(0.6f, 6.5f, 19f), darkWall, false);
            var canvasGo = new GameObject("ScreenText", typeof(RectTransform), typeof(Canvas));
            canvasGo.transform.SetParent(parent);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)canvasGo.transform;
            rt.sizeDelta = new Vector2(1800f, 600f);
            rt.position = pos - new Vector3(sx * 0.35f, 0f, 0f);
            rt.rotation = Quaternion.LookRotation(new Vector3(sx, 0f, 0f));
            rt.localScale = Vector3.one * 0.01f;
            var bg = UIKit.Img(rt, "Bg", null, new Color(0.03f, 0.1f, 0.3f, 1f));
            UIKit.Stretch(bg.rectTransform);
            var t1 = UIKit.Txt(rt, "ACADEMY GROUND", 150, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(1700f, 220f));
            t1.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var t2 = UIKit.Txt(rt, "ULTIMATE KICKOFF", 110, new Color(1f, 0.85f, 0.3f), new Vector2(0.5f, 0.5f), new Vector2(0f, -130f), new Vector2(1700f, 180f));
            t2.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    static void BuildFloodlights(Transform root) { }

    static void BuildCornerFlags(Transform root)
    {
        var yellow = MatLib.Lit(new Color(1f, 0.85f, 0.1f), 0.2f);
        var white = MatLib.Lit(Color.white, 0.3f);
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
            {
                var p = new Vector3(sx * Pitch.HalfL, 0, sz * Pitch.HalfW);
                Box(root, "FlagPole", p + Vector3.up * 0.75f, new Vector3(0.04f, 1.5f, 0.04f), white);
                Box(root, "Flag", p + new Vector3(0.18f, 1.35f, 0f), new Vector3(0.35f, 0.25f, 0.02f), yellow);
            }
    }

    static void BuildDugouts(Transform root)
    {
        var shell = MatLib.Lit(new Color(0.75f, 0.78f, 0.85f), 0.6f);
        var seats = MatLib.Lit(new Color(0.1f, 0.25f, 0.6f), 0.3f);
        foreach (float sx in new[] { -1f, 1f })
        {
            float x = sx * 9f, z = Pitch.HalfW + 2.6f;
            Box(root, "DugoutBack", new Vector3(x, 1.1f, z + 0.9f), new Vector3(8f, 2.2f, 0.2f), shell);
            Box(root, "DugoutRoof", new Vector3(x, 2.2f, z + 0.3f), new Vector3(8f, 0.12f, 1.4f), shell);
            Box(root, "DugoutSeats", new Vector3(x, 0.45f, z + 0.5f), new Vector3(7.6f, 0.9f, 0.6f), seats);
        }
    }
}
