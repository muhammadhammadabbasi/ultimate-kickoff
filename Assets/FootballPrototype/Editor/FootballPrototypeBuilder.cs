using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Menu: Tools > Football Prototype > Build Scene
/// Builds the whole prototype scene (pitch, goals, nets, 3v3 teams, keepers, ball, camera, mobile HUD) in one click.
/// Works with URP (recommended) and the Built-in render pipeline.
/// </summary>
public static class FootballPrototypeBuilder
{
    const string Root = "Assets/FootballPrototype";
    const string GenFolder = Root + "/Generated";
    const string ScenePath = Root + "/Scenes/FootballPrototype.unity";

    const float HalfLength = 30f;
    const float HalfWidth = 20f;
    const float GoalHalfWidth = 3f;
    const float GoalHeight = 2.2f;
    const float GoalDepth = 1.6f;

    public static string SceneAssetPath => ScenePath;

    [MenuItem("Tools/Football Prototype/Build Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildCore();
        EditorUtility.DisplayDialog("Football Prototype",
            "Scene ready! Press Play.\n\nPC controls:\nWASD / Arrows = move\nLeft Shift = sprint\nJ = pass (switch player when you don't have the ball)\nK = shoot (hold to charge, release to shoot)",
            "OK");
    }

    /// <summary>Builds and saves the scene without any dialogs (used by the GitHub APK build too).</summary>
    public static void BuildCore()
    {
        EnsureFolder(GenFolder);
        EnsureFolder(Root + "/Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ---------- Materials ----------
        var pitchMat = Mat("Pitch", Color.white, 0.1f, PitchTexture());
        var lineMat = Mat("Line", new Color(0.95f, 0.95f, 0.95f), 0.1f);
        var postMat = Mat("Post", Color.white, 0.6f);
        var boardMat = Mat("Boards", new Color(0.08f, 0.12f, 0.3f), 0.4f);
        var standMat = Mat("Stands", new Color(0.22f, 0.24f, 0.28f), 0.1f);
        var blueMat = Mat("TeamBlue", new Color(0.12f, 0.32f, 0.9f), 0.3f);
        var redMat = Mat("TeamRed", new Color(0.85f, 0.14f, 0.14f), 0.3f);
        var keeperBlueMat = Mat("KeeperBlue", new Color(0.1f, 0.75f, 0.4f), 0.3f);
        var keeperRedMat = Mat("KeeperRed", new Color(0.95f, 0.75f, 0.1f), 0.3f);
        var accentMat = Mat("Accent", Color.white, 0.3f);
        var indicatorMat = Mat("Indicator", new Color(1f, 0.9f, 0.1f), 0.2f);
        var ballMat = Mat("Ball", Color.white, 0.5f);
        var netMat = NetMat(NetTexture());

        var world = new GameObject("World").transform;

        // ---------- Pitch ----------
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Pitch";
        ground.transform.SetParent(world);
        ground.transform.localScale = new Vector3((HalfWidth + 4f) * 2f / 10f, 1f, (HalfLength + 4f) * 2f / 10f);
        ground.GetComponent<Renderer>().sharedMaterial = pitchMat;

        BuildLines(world, lineMat);
        BuildBoardsAndStands(world, boardMat, standMat);
        BuildGoal(world, 1f, postMat, netMat);
        BuildGoal(world, -1f, postMat, netMat);

        // ---------- Ball ----------
        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Ball";
        ball.transform.position = new Vector3(0f, 0.11f, 0f);
        ball.transform.localScale = Vector3.one * 0.22f;
        ball.GetComponent<Renderer>().sharedMaterial = ballMat;
        ball.GetComponent<SphereCollider>().sharedMaterial = BallPhysicsMaterial();
        ball.AddComponent<Rigidbody>();
        var ballCtrl = ball.AddComponent<BallController>();

        // ---------- Teams ----------
        var teams = new GameObject("Teams").transform;
        MakePlayer(teams, "Blue_ST", 0, new Vector3(0f, 0f, -4f), blueMat, accentMat, indicatorMat);
        MakePlayer(teams, "Blue_LM", 0, new Vector3(-9f, 0f, -12f), blueMat, accentMat, indicatorMat);
        MakePlayer(teams, "Blue_RM", 0, new Vector3(9f, 0f, -12f), blueMat, accentMat, indicatorMat);
        MakePlayer(teams, "Red_ST", 1, new Vector3(0f, 0f, 4f), redMat, accentMat, indicatorMat);
        MakePlayer(teams, "Red_LM", 1, new Vector3(-9f, 0f, 12f), redMat, accentMat, indicatorMat);
        MakePlayer(teams, "Red_RM", 1, new Vector3(9f, 0f, 12f), redMat, accentMat, indicatorMat);

        var gkBlue = MakeKeeper(teams, "Blue_GK", 0, new Vector3(0f, 0f, -HalfLength + 0.8f), keeperBlueMat, accentMat);
        var gkRed = MakeKeeper(teams, "Red_GK", 1, new Vector3(0f, 0f, HalfLength - 0.8f), keeperRedMat, accentMat);

        // ---------- Camera & light ----------
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 13f, -13f);
            cam.fieldOfView = 50f;
            cam.farClipPlane = 300f;
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = ball.transform;
        }

        var lightGo = GameObject.Find("Directional Light");
        if (lightGo != null)
        {
            var light = lightGo.GetComponent<Light>();
            lightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
        }

        // ---------- HUD ----------
        var match = new GameObject("MatchManager").AddComponent<MatchManager>();
        match.halfLength = HalfLength;
        match.halfWidth = HalfWidth;
        match.goalHalfWidth = GoalHalfWidth;
        match.goalHeight = GoalHeight;
        match.ball = ballCtrl;
        match.keepers = new[] { gkBlue, gkRed };
        BuildHUD(match);

        // ---------- Save ----------
        EditorSceneManager.SaveScene(scene, ScenePath);
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        list.RemoveAll(s => s.path == ScenePath);
        list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        AssetDatabase.SaveAssets();
    }

    // =====================================================================
    // Pitch
    // =====================================================================

    static void BuildLines(Transform parent, Material m)
    {
        var lines = new GameObject("Lines").transform;
        lines.SetParent(parent);
        const float w = 0.12f;

        Line(lines, new Vector3(-HalfWidth, 0, 0), new Vector3(w, 0, HalfLength * 2f + w), m);
        Line(lines, new Vector3(HalfWidth, 0, 0), new Vector3(w, 0, HalfLength * 2f + w), m);
        Line(lines, new Vector3(0, 0, -HalfLength), new Vector3(HalfWidth * 2f, 0, w), m);
        Line(lines, new Vector3(0, 0, HalfLength), new Vector3(HalfWidth * 2f, 0, w), m);
        Line(lines, Vector3.zero, new Vector3(HalfWidth * 2f, 0, w), m);

        foreach (float s in new[] { -1f, 1f })
        {
            // Penalty box 16 x 8
            float boxZ = s * (HalfLength - 8f);
            Line(lines, new Vector3(0, 0, boxZ), new Vector3(16f, 0, w), m);
            Line(lines, new Vector3(-8f, 0, s * (HalfLength - 4f)), new Vector3(w, 0, 8f), m);
            Line(lines, new Vector3(8f, 0, s * (HalfLength - 4f)), new Vector3(w, 0, 8f), m);
            // Goal area 9 x 3
            Line(lines, new Vector3(0, 0, s * (HalfLength - 3f)), new Vector3(9f, 0, w), m);
            Line(lines, new Vector3(-4.5f, 0, s * (HalfLength - 1.5f)), new Vector3(w, 0, 3f), m);
            Line(lines, new Vector3(4.5f, 0, s * (HalfLength - 1.5f)), new Vector3(w, 0, 3f), m);
            // Penalty spot
            Line(lines, new Vector3(0, 0, s * (HalfLength - 6f)), new Vector3(0.25f, 0, 0.25f), m);
        }

        // Centre circle (radius 5)
        const int segments = 48;
        const float r = 5f;
        float segLen = 2f * Mathf.PI * r / segments + 0.02f;
        for (int i = 0; i < segments; i++)
        {
            float a = (i + 0.5f) / segments * Mathf.PI * 2f;
            var seg = Line(lines, new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), new Vector3(segLen, 0, w), m);
            seg.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
        }
        Line(lines, Vector3.zero, new Vector3(0.3f, 0, 0.3f), m);
    }

    static GameObject Line(Transform parent, Vector3 center, Vector3 size, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Line";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(center.x, 0.006f, center.z);
        go.transform.localScale = new Vector3(size.x, 0.01f, size.z);
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off;
        return go;
    }

    static void BuildBoardsAndStands(Transform parent, Material boardMat, Material standMat)
    {
        var boards = new GameObject("Boards").transform;
        boards.SetParent(parent);
        float bx = HalfWidth + 2.5f;
        float bz = HalfLength + 3f;

        Board(boards, new Vector3(-bx, 0, 0), new Vector3(0.3f, 1f, bz * 2f), boardMat);
        Board(boards, new Vector3(bx, 0, 0), new Vector3(0.3f, 1f, bz * 2f), boardMat);
        Board(boards, new Vector3(0, 0, -bz), new Vector3(bx * 2f, 1f, 0.3f), boardMat);
        Board(boards, new Vector3(0, 0, bz), new Vector3(bx * 2f, 1f, 0.3f), boardMat);

        // Simple stepped stands (visual only)
        var stands = new GameObject("Stands").transform;
        stands.SetParent(parent);
        for (int i = 0; i < 4; i++)
        {
            float off = 4f + i * 2f;
            float h = 1.5f + i * 1.5f;
            Block(stands, new Vector3(-(HalfWidth + off), h / 2f, 0), new Vector3(2f, h, (bz + off) * 2f), standMat);
            Block(stands, new Vector3(HalfWidth + off, h / 2f, 0), new Vector3(2f, h, (bz + off) * 2f), standMat);
            Block(stands, new Vector3(0, h / 2f, -(bz + off - 1f)), new Vector3((bx + off) * 2f, h, 2f), standMat);
            Block(stands, new Vector3(0, h / 2f, bz + off - 1f), new Vector3((bx + off) * 2f, h, 2f), standMat);
        }
    }

    static void Board(Transform parent, Vector3 pos, Vector3 size, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "AdBoard";
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(pos.x, size.y / 2f, pos.z);
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        // Taller invisible wall so the ball stays in play
        var box = go.GetComponent<BoxCollider>();
        box.size = new Vector3(1f, 4f, 1f);
        box.center = new Vector3(0f, 1.5f, 0f);
    }

    static void Block(Transform parent, Vector3 pos, Vector3 size, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Stand";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        go.isStatic = true;
    }

    static void BuildGoal(Transform parent, float sign, Material postMat, Material netMat)
    {
        var g = new GameObject(sign > 0 ? "Goal_North" : "Goal_South").transform;
        g.SetParent(parent);
        float z = sign * HalfLength;
        float back = z + sign * GoalDepth;
        const float t = 0.12f;

        // Frame (posts + crossbar have colliders so the ball can hit the woodwork)
        Bar(g, new Vector3(-GoalHalfWidth, 0, z), new Vector3(-GoalHalfWidth, GoalHeight, z), t, postMat, true);
        Bar(g, new Vector3(GoalHalfWidth, 0, z), new Vector3(GoalHalfWidth, GoalHeight, z), t, postMat, true);
        Bar(g, new Vector3(-GoalHalfWidth, GoalHeight, z), new Vector3(GoalHalfWidth, GoalHeight, z), t, postMat, true);
        Bar(g, new Vector3(-GoalHalfWidth, 0, back), new Vector3(-GoalHalfWidth, GoalHeight, back), t * 0.6f, postMat, false);
        Bar(g, new Vector3(GoalHalfWidth, 0, back), new Vector3(GoalHalfWidth, GoalHeight, back), t * 0.6f, postMat, false);
        Bar(g, new Vector3(-GoalHalfWidth, GoalHeight, back), new Vector3(GoalHalfWidth, GoalHeight, back), t * 0.6f, postMat, false);
        Bar(g, new Vector3(-GoalHalfWidth, GoalHeight, z), new Vector3(-GoalHalfWidth, GoalHeight, back), t * 0.6f, postMat, false);
        Bar(g, new Vector3(GoalHalfWidth, GoalHeight, z), new Vector3(GoalHalfWidth, GoalHeight, back), t * 0.6f, postMat, false);

        // Net visuals
        float midZ = (z + back) / 2f;
        NetQuad(g, new Vector3(0, GoalHeight / 2f, back), Quaternion.identity, new Vector3(GoalHalfWidth * 2f, GoalHeight, 1f), netMat);
        NetQuad(g, new Vector3(-GoalHalfWidth, GoalHeight / 2f, midZ), Quaternion.Euler(0, 90, 0), new Vector3(GoalDepth, GoalHeight, 1f), netMat);
        NetQuad(g, new Vector3(GoalHalfWidth, GoalHeight / 2f, midZ), Quaternion.Euler(0, 90, 0), new Vector3(GoalDepth, GoalHeight, 1f), netMat);
        NetQuad(g, new Vector3(0, GoalHeight, midZ), Quaternion.Euler(90, 0, 0), new Vector3(GoalHalfWidth * 2f, GoalDepth, 1f), netMat);

        // Net colliders (invisible)
        Wall(g, "NetBack", new Vector3(0, GoalHeight / 2f, back + sign * 0.05f), new Vector3(GoalHalfWidth * 2f, GoalHeight, 0.1f));
        Wall(g, "NetLeft", new Vector3(-GoalHalfWidth - 0.05f, GoalHeight / 2f, midZ), new Vector3(0.1f, GoalHeight, GoalDepth));
        Wall(g, "NetRight", new Vector3(GoalHalfWidth + 0.05f, GoalHeight / 2f, midZ), new Vector3(0.1f, GoalHeight, GoalDepth));
        Wall(g, "NetTop", new Vector3(0, GoalHeight + 0.05f, midZ), new Vector3(GoalHalfWidth * 2f, 0.1f, GoalDepth));

        // Goal trigger (behind the goal line)
        var trig = new GameObject("GoalTrigger");
        trig.transform.SetParent(g);
        trig.transform.position = new Vector3(0, GoalHeight / 2f, z + sign * (0.35f + (GoalDepth - 0.35f) / 2f));
        var box = trig.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(GoalHalfWidth * 2f - 0.1f, GoalHeight - 0.05f, GoalDepth - 0.35f);
        trig.AddComponent<GoalTrigger>().scoringTeam = sign > 0 ? 0 : 1;
    }

    static void Bar(Transform parent, Vector3 a, Vector3 b, float thickness, Material m, bool collider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Bar";
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent);
        Vector3 dir = b - a;
        go.transform.position = (a + b) / 2f;
        go.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
        go.transform.localScale = new Vector3(thickness, dir.magnitude / 2f + thickness / 2f, thickness);
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    static void NetQuad(Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Net";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent);
        go.transform.SetPositionAndRotation(pos, rot);
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off;
    }

    static void Wall(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.AddComponent<BoxCollider>().size = size;
    }

    // =====================================================================
    // Players
    // =====================================================================

    static FootballPlayer MakePlayer(Transform parent, string name, int team, Vector3 pos, Material body, Material accent, Material indicatorMat)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(team == 0 ? Vector3.forward : Vector3.back));

        var cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.skinWidth = 0.05f;
        cc.stepOffset = 0.3f;

        BuildBody(root.transform, body, accent);

        GameObject indicator = null;
        if (team == 0)
        {
            indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.name = "ActiveIndicator";
            Object.DestroyImmediate(indicator.GetComponent<Collider>());
            indicator.transform.SetParent(root.transform, false);
            indicator.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            indicator.transform.localScale = new Vector3(1.3f, 0.01f, 1.3f);
            var r = indicator.GetComponent<Renderer>();
            r.sharedMaterial = indicatorMat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            indicator.SetActive(false);
        }

        var fp = root.AddComponent<FootballPlayer>();
        fp.team = team;
        fp.indicator = indicator;
        if (team == 1)
        {
            fp.runSpeed = 5.6f;
            fp.sprintSpeed = 7.8f;
        }
        return fp;
    }

    static Goalkeeper MakeKeeper(Transform parent, string name, int team, Vector3 pos, Material body, Material accent)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(team == 0 ? Vector3.forward : Vector3.back));
        BuildBody(root.transform, body, accent);
        var gk = root.AddComponent<Goalkeeper>();
        gk.team = team;
        return gk;
    }

    static void BuildBody(Transform root, Material body, Material accent)
    {
        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "Body";
        Object.DestroyImmediate(capsule.GetComponent<Collider>());
        capsule.transform.SetParent(root, false);
        capsule.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        capsule.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
        capsule.GetComponent<Renderer>().sharedMaterial = body;

        // White chest stripe shows which way the player is facing
        var front = GameObject.CreatePrimitive(PrimitiveType.Cube);
        front.name = "Front";
        Object.DestroyImmediate(front.GetComponent<Collider>());
        front.transform.SetParent(root, false);
        front.transform.localPosition = new Vector3(0f, 1.3f, 0.3f);
        front.transform.localScale = new Vector3(0.35f, 0.12f, 0.12f);
        front.GetComponent<Renderer>().sharedMaterial = accent;
    }

    // =====================================================================
    // HUD
    // =====================================================================

    static void BuildHUD(MatchManager match)
    {
        var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var c = canvasGo.transform;

        var knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        var barSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // Score + clock
        var scoreBg = UIImage("ScoreBar", c, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(760f, 76f), barSprite, new Color(0f, 0f, 0f, 0.55f), false);
        match.scoreText = UIText("ScoreText", scoreBg.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(740f, 70f), "BLU  0 - 0  RED      00:00", 40);

        // Big centre message (GOAL!, KICK OFF, FULL TIME)
        match.messageText = UIText("Message", c, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1400f, 400f), "", 110);

        // Keyboard hint (editor / PC)
        var hint = UIText("KeyboardHint", c, new Vector2(0f, 1f), new Vector2(360f, -30f), new Vector2(680f, 40f),
            "PC: WASD move · Shift sprint · J pass/switch · K shoot (hold)", 22);
        hint.alignment = TextAnchor.MiddleLeft;
        hint.color = new Color(1f, 1f, 1f, 0.7f);

        // Joystick
        var stickBg = UIImage("Joystick", c, new Vector2(0f, 0f), new Vector2(250f, 250f), new Vector2(300f, 300f), knobSprite, new Color(1f, 1f, 1f, 0.18f), true);
        var knob = UIImage("Knob", stickBg.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 130f), knobSprite, new Color(1f, 1f, 1f, 0.6f), false);
        var stick = stickBg.gameObject.AddComponent<VirtualJoystick>();
        stick.background = stickBg.rectTransform;
        stick.knob = knob.rectTransform;

        // Buttons
        MakeButton(c, "ShootButton", FootballAction.Shoot, "SHOOT", new Vector2(-230f, 230f), 230f, new Color(1f, 0.35f, 0.3f, 0.45f), knobSprite);
        MakeButton(c, "PassButton", FootballAction.Pass, "PASS", new Vector2(-480f, 160f), 170f, new Color(0.3f, 0.6f, 1f, 0.45f), knobSprite);
        MakeButton(c, "SprintButton", FootballAction.Sprint, "SPRINT", new Vector2(-250f, 485f), 170f, new Color(1f, 1f, 1f, 0.3f), knobSprite);

        // Shot power bar
        var barBg = UIImage("ShotPowerBg", c, new Vector2(1f, 0f), new Vector2(-230f, 372f), new Vector2(220f, 20f), barSprite, new Color(0f, 0f, 0f, 0.5f), false);
        var fill = UIImage("ShotPowerFill", barBg.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(216f, 16f), barSprite, new Color(1f, 0.6f, 0.1f, 0.95f), false);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        match.shotPowerFill = fill;

        // Event system (works with old Input Manager or new Input System)
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    static void MakeButton(Transform parent, string name, FootballAction action, string label, Vector2 pos, float size, Color color, Sprite sprite)
    {
        var img = UIImage(name, parent, new Vector2(1f, 0f), pos, new Vector2(size, size), sprite, color, true);
        var btn = img.gameObject.AddComponent<ActionButton>();
        btn.action = action;
        btn.highlight = img;
        UIText("Label", img.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, 60f), label, Mathf.RoundToInt(size * 0.15f));
    }

    static RectTransform UIObj(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static Image UIImage(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Sprite sprite, Color color, bool raycast)
    {
        var rt = UIObj(name, parent, anchor, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    static Text UIText(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string text, int fontSize)
    {
        var rt = UIObj(name, parent, anchor, pos, size);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = UIFont();
        t.text = text;
        t.fontSize = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var shadow = rt.gameObject.AddComponent<Shadow>();
        shadow.effectDistance = new Vector2(2f, -2f);
        return t;
    }

    static Font UIFont()
    {
#if UNITY_2022_2_OR_NEWER
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
    }

    // =====================================================================
    // Assets
    // =====================================================================

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static Shader LitShader()
    {
        Shader s = null;
        if (GraphicsSettings.currentRenderPipeline != null) s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        return s;
    }

    static Material Mat(string name, Color color, float smoothness, Texture tex = null)
    {
        string path = $"{GenFolder}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(LitShader());
            AssetDatabase.CreateAsset(m, path);
        }
        else
        {
            m.shader = LitShader();
        }
        m.color = color;
        if (tex != null) m.mainTexture = tex;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material NetMat(Texture2D tex)
    {
        var m = Mat("Net", new Color(0.95f, 0.95f, 0.95f), 0f, tex);
        m.mainTextureScale = new Vector2(8f, 3f);
        if (m.HasProperty("_AlphaClip"))
        {
            // URP Lit: alpha clipping + double sided
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            m.renderQueue = (int)RenderQueue.AlphaTest;
        }
        else
        {
            // Built-in Standard: cutout mode
            m.SetFloat("_Mode", 1f);
            m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.renderQueue = (int)RenderQueue.AlphaTest;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D PitchTexture()
    {
        string path = GenFolder + "/PitchStripes.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        const int w = 32, stripes = 12, per = 16;
        int h = stripes * per;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Color a = new Color(0.17f, 0.48f, 0.19f);
        Color b = new Color(0.21f, 0.56f, 0.23f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, (y / per) % 2 == 0 ? a : b);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        tex.anisoLevel = 4;
        AssetDatabase.CreateAsset(tex, path);
        return tex;
    }

    static Texture2D NetTexture()
    {
        string path = GenFolder + "/NetGrid.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool line = x % 8 < 1 || y % 8 < 1;
                tex.SetPixel(x, y, line ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        AssetDatabase.CreateAsset(tex, path);
        return tex;
    }

#if UNITY_6000_0_OR_NEWER
    static PhysicsMaterial BallPhysicsMaterial()
    {
        var pm = new PhysicsMaterial("Ball")
        {
            bounciness = 0.5f,
            dynamicFriction = 0.4f,
            staticFriction = 0.4f,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Average
        };
        AssetDatabase.CreateAsset(pm, GenFolder + "/BallPhysics.asset");
        return pm;
    }
#else
    static PhysicMaterial BallPhysicsMaterial()
    {
        var pm = new PhysicMaterial("Ball")
        {
            bounciness = 0.5f,
            dynamicFriction = 0.4f,
            staticFriction = 0.4f,
            bounceCombine = PhysicMaterialCombine.Maximum,
            frictionCombine = PhysicMaterialCombine.Average
        };
        AssetDatabase.CreateAsset(pm, GenFolder + "/BallPhysics.asset");
        return pm;
    }
#endif
}
