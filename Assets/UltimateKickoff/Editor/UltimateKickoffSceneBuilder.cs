using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Tools > Ultimate Kickoff > Build Scene
/// Creates the single game scene (camera, light, App object with template materials).
/// Everything else (stadium, menus, match) is built at runtime by AppController.
/// </summary>
public static class UltimateKickoffSceneBuilder
{
    const string Root = "Assets/UltimateKickoff";
    const string GenFolder = Root + "/Generated";
    const string ScenePath = Root + "/Scenes/Main.unity";

    public static string SceneAssetPath => ScenePath;
    public static string GeneratedFolder => GenFolder;

    [MenuItem("Tools/Ultimate Kickoff/Build Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildCore();
        EditorUtility.DisplayDialog("Ultimate Kickoff", "Scene ready: " + ScenePath + "\nPress Play.\n\nPC: WASD move, Shift sprint, J pass, L through, K shoot (hold).", "OK");
    }

    public static void BuildCore()
    {
        EnsureFolder(GenFolder);
        EnsureFolder(Root + "/Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var lit = CreateMaterial("TemplateLit", false);
        var cutout = CreateMaterial("TemplateCutout", true);

        var app = new GameObject("App").AddComponent<AppController>();
        app.litTemplate = lit;
        app.cutoutTemplate = cutout;

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 22f, -62f);
            cam.transform.LookAt(Vector3.zero);
            cam.farClipPlane = 450f;
            cam.gameObject.AddComponent<CameraRig>();
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
    }

    static Material CreateMaterial(string name, bool cutout)
    {
        string path = GenFolder + "/" + name + ".mat";
        var shader = Shader.Find("Standard");
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        else m.shader = shader;

        m.color = Color.white;
        m.SetFloat("_Glossiness", 0.2f);
        m.SetFloat("_Metallic", 0f);
        if (cutout)
        {
            m.SetFloat("_Mode", 1f);
            m.SetFloat("_Cutoff", 0.5f);
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.EnableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
