using System.Collections.Generic;
using UnityEngine;

public static class Pitch
{
    public const float L = 105f;
    public const float W = 68f;
    public const float HalfL = 52.5f;
    public const float HalfW = 34f;
    public const float GoalHalfWidth = 3.66f;
    public const float GoalHeight = 2.44f;
    public const float GoalDepth = 2f;
    public const float BoxDepth = 16.5f;
    public const float BoxHalfWidth = 20.16f;

    /// <summary>World position for a team-relative point. depth 0 = own goal line, 1 = opponent goal line; side -1 left .. +1 right.</summary>
    public static Vector3 ToWorld(int team, float depth, float side)
    {
        float dir = team == 0 ? 1f : -1f;
        return new Vector3(dir * (depth * L - HalfL), 0f, -dir * side * HalfW);
    }

    public static float DepthOf(int team, Vector3 p)
    {
        float dir = team == 0 ? 1f : -1f;
        return (dir * p.x + HalfL) / L;
    }

    public static float SideOf(int team, Vector3 p)
    {
        float dir = team == 0 ? 1f : -1f;
        return -dir * p.z / HalfW;
    }

    public static Vector3 GoalCenter(int defendingTeam)
    {
        return new Vector3(defendingTeam == 0 ? -HalfL : HalfL, 0f, 0f);
    }

    /// <summary>Goal the given team attacks.</summary>
    public static Vector3 AttackGoal(int team) { return GoalCenter(1 - team); }

    public static bool InBox(int defendingTeam, Vector3 p)
    {
        float gx = defendingTeam == 0 ? -HalfL : HalfL;
        return Mathf.Abs(p.x - gx) < BoxDepth && Mathf.Abs(p.z) < BoxHalfWidth && Mathf.Sign(p.x) == Mathf.Sign(gx);
    }
}

/// <summary>Runtime materials (cloned from template materials in the scene so shaders are included in the build).</summary>
public static class MatLib
{
    static Material lit, cutout;
    static readonly Dictionary<int, Material> cache = new Dictionary<int, Material>();

    public static void Init(Material litTemplate, Material cutoutTemplate)
    {
        lit = litTemplate;
        cutout = cutoutTemplate;
        cache.Clear();
    }

    public static Material Lit(Color c, float smoothness = 0.15f)
    {
        int key = Key(c, smoothness);
        Material m;
        if (cache.TryGetValue(key, out m) && m != null) return m;
        m = new Material(lit);
        m.color = c;
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        cache[key] = m;
        return m;
    }

    public static Material Textured(Texture tex, Color tint, Vector2 tiling, float smoothness = 0.1f)
    {
        var m = new Material(lit);
        m.color = tint;
        m.mainTexture = tex;
        m.mainTextureScale = tiling;
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        return m;
    }

    public static Material Cutout(Texture tex, Color tint, Vector2 tiling)
    {
        var m = new Material(cutout);
        m.color = tint;
        m.mainTexture = tex;
        m.mainTextureScale = tiling;
        return m;
    }

    static int Key(Color c, float s)
    {
        int r = Mathf.RoundToInt(c.r * 255f), g = Mathf.RoundToInt(c.g * 255f), b = Mathf.RoundToInt(c.b * 255f);
        int sm = Mathf.RoundToInt(s * 20f);
        return (((r * 256 + g) * 256 + b) * 32) + sm;
    }
}

public static class MeshUtil
{
    static Mesh capsule, sphere;

    /// <summary>Low-poly capsule, same size as Unity's primitive (radius 0.5, height 2).</summary>
    public static Mesh Capsule { get { if (capsule == null) capsule = BuildRounded(10, 3, 0.5f); return capsule; } }

    /// <summary>Low-poly sphere, diameter 1.</summary>
    public static Mesh Sphere { get { if (sphere == null) sphere = BuildRounded(12, 4, 0f); return sphere; } }

    static Mesh BuildRounded(int seg, int hemi, float halfLen)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        int rings = 0;
        for (int half = 0; half < 2; half++)
        {
            for (int i = 0; i <= hemi; i++)
            {
                float lat = half == 0 ? 90f - i * (90f / hemi) : -i * (90f / hemi);
                float offset = half == 0 ? halfLen : -halfLen;
                float latR = lat * Mathf.Deg2Rad;
                for (int j = 0; j <= seg; j++)
                {
                    float lon = j * Mathf.PI * 2f / seg;
                    Vector3 n = new Vector3(Mathf.Cos(latR) * Mathf.Cos(lon), Mathf.Sin(latR), Mathf.Cos(latR) * Mathf.Sin(lon));
                    verts.Add(n * 0.5f + new Vector3(0f, offset, 0f));
                    norms.Add(n);
                    uvs.Add(new Vector2((float)j / seg, (lat + 90f) / 180f));
                }
                rings++;
            }
        }
        int stride = seg + 1;
        for (int r = 0; r < rings - 1; r++)
        {
            for (int j = 0; j < seg; j++)
            {
                int a = r * stride + j, b = a + 1, c = (r + 1) * stride + j, d = c + 1;
                tris.Add(a); tris.Add(b); tris.Add(d);
                tris.Add(a); tris.Add(d); tris.Add(c);
            }
        }
        var m = new Mesh();
        m.SetVertices(verts);
        m.SetNormals(norms);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        return m;
    }

    public static GameObject Part(string name, Transform parent, Mesh mesh, Material mat, Vector3 localPos, Vector3 scale, bool shadows = true)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = true;
        r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        return go;
    }

    /// <summary>Builds flat ground quads (lines) as one mesh. Quads are double sided.</summary>
    public class FlatBuilder
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<int> t = new List<int>();
        readonly List<Vector2> uv = new List<Vector2>();
        float y;

        public FlatBuilder(float height) { y = height; }

        public void Segment(Vector3 a, Vector3 b, float width)
        {
            a.y = y; b.y = y;
            Vector3 dir = (b - a).normalized;
            Vector3 n = new Vector3(-dir.z, 0f, dir.x) * (width * 0.5f);
            Quad(a - n, a + n, b - n, b + n);
        }

        public void Rect(float x0, float z0, float x1, float z1, float width)
        {
            Segment(new Vector3(x0, 0, z0), new Vector3(x1, 0, z0), width);
            Segment(new Vector3(x1, 0, z0), new Vector3(x1, 0, z1), width);
            Segment(new Vector3(x1, 0, z1), new Vector3(x0, 0, z1), width);
            Segment(new Vector3(x0, 0, z1), new Vector3(x0, 0, z0), width);
        }

        public void Arc(Vector3 center, float radius, float fromDeg, float toDeg, float width, int steps)
        {
            Vector3 prev = center + new Vector3(Mathf.Cos(fromDeg * Mathf.Deg2Rad), 0f, Mathf.Sin(fromDeg * Mathf.Deg2Rad)) * radius;
            for (int i = 1; i <= steps; i++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, (float)i / steps) * Mathf.Deg2Rad;
                Vector3 p = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                Segment(prev, p, width);
                prev = p;
            }
        }

        public void Dot(Vector3 c, float r)
        {
            Arc(c, r * 0.5f, 0f, 360f, r, 10);
        }

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.up); uv.Add(Vector2.one);
            t.Add(i); t.Add(i + 1); t.Add(i + 3);
            t.Add(i); t.Add(i + 3); t.Add(i + 2);
            t.Add(i); t.Add(i + 3); t.Add(i + 1);
            t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        public Mesh Build()
        {
            var m = new Mesh();
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            var n = new Vector3[v.Count];
            for (int i = 0; i < n.Length; i++) n[i] = Vector3.up;
            m.normals = n;
            m.RecalculateBounds();
            return m;
        }
    }

    /// <summary>Vertical double-sided quad mesh of size 1x1 centred on origin (XY plane).</summary>
    public static Mesh DoubleQuad()
    {
        var m = new Mesh();
        m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) };
        m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        m.triangles = new[] { 0, 2, 1, 1, 2, 3, 0, 1, 2, 1, 3, 2 };
        m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
        m.RecalculateBounds();
        return m;
    }
}
