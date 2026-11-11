using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Called by GitHub Actions (GameCI) to build the Android APK:
///   buildMethod: CIBuild.BuildAndroid
/// It generates the match scene, applies Android player settings and builds the APK.
/// </summary>
public static class CIBuild
{
    const string IconPath = "Assets/FootballPrototype/Generated/AppIcon.png";

    public static void BuildAndroid()
    {
        try
        {
            FootballPrototypeBuilder.BuildCore();
            ApplyPlayerSettings();

            string outPath = GetArg("-customBuildPath");
            if (string.IsNullOrEmpty(outPath)) outPath = "build/Android/UltimateKickoff.apk";
            if (!outPath.EndsWith(".apk", StringComparison.OrdinalIgnoreCase)) outPath += ".apk";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath)));

            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { FootballPrototypeBuilder.SceneAssetPath },
                locationPathName = outPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;
            Debug.Log($"[CIBuild] Result: {s.result}, size: {s.totalSize / (1024f * 1024f):0.0} MB, errors: {s.totalErrors}, output: {outPath}");
            EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    static void ApplyPlayerSettings()
    {
        var android = NamedBuildTarget.Android;
        PlayerSettings.companyName = "Hammad Games";
        PlayerSettings.productName = "Ultimate Kickoff";
        PlayerSettings.SetApplicationIdentifier(android, "com.hammadgames.ultimatekickoff");

        string version = GetArg("-buildVersion");
        if (!string.IsNullOrEmpty(version) && version != "none") PlayerSettings.bundleVersion = version;
        string code = GetArg("-androidVersionCode");
        if (int.TryParse(code, out int versionCode) && versionCode > 0) PlayerSettings.Android.bundleVersionCode = versionCode;

        // 64-bit IL2CPP build: required by modern phones and the Play Store
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.useCustomKeystore = false; // debug-signed APK, installs directly on the phone

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        // Mobile-friendly quality
        QualitySettings.shadowDistance = 45f;
        QualitySettings.antiAliasing = 2;

        SetAppIcon();
    }

    static void SetAppIcon()
    {
        const int size = 512;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(size / 2f, size / 2f);
        Color bgTop = new Color(0.10f, 0.45f, 0.18f), bgBottom = new Color(0.04f, 0.22f, 0.09f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color col = Color.Lerp(bgBottom, bgTop, y / (float)size);
                if ((y / 48) % 2 == 0) col *= 1.08f; // pitch stripes
                float d = Vector2.Distance(new Vector2(x, y), c);
                if (d < 170f)
                {
                    col = Color.white;
                    // simple pentagon panels
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 p = i == 0 ? c : c + new Vector2(Mathf.Cos(i * 72f * Mathf.Deg2Rad + Mathf.PI / 2f), Mathf.Sin(i * 72f * Mathf.Deg2Rad + Mathf.PI / 2f)) * 125f;
                        if (Vector2.Distance(new Vector2(x, y), p) < (i == 0 ? 48f : 36f)) col = new Color(0.08f, 0.08f, 0.1f);
                    }
                }
                else if (d < 182f) col = new Color(0.08f, 0.08f, 0.1f);
                col.a = 1f;
                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();
        File.WriteAllBytes(IconPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon != null)
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
    }

    static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }
}
