using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Entry point: builds the stadium + UI and switches between Home, My Team, Play and Match.</summary>
public class AppController : MonoBehaviour
{
    public static AppController I { get; private set; }

    [Header("Template materials (assigned by the scene builder)")]
    public Material litTemplate;
    public Material cutoutTemplate;

    RectTransform uiRoot;
    CameraRig camRig;
    GameObject screen;
    MatchController match;
    ClubData lastOpponent;

    void Awake()
    {
        I = this;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
        Input.multiTouchEnabled = true;

        MatLib.Init(litTemplate, cutoutTemplate);
        Squad.Load();

        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
        }
        camRig = cam.GetComponent<CameraRig>();
        if (camRig == null) camRig = cam.gameObject.AddComponent<CameraRig>();
        camRig.SetMenu();

        StadiumBuilder.Build();
        BuildCanvas();
        ShowHome();
    }

    void BuildCanvas()
    {
        var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        var safe = UIKit.Rect("SafeArea", canvasGo.transform);
        UIKit.Stretch(safe);
        safe.gameObject.AddComponent<SafeArea>();
        uiRoot = safe;

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<StandaloneInputModule>();
        }
    }

    RectTransform NewScreen(string name)
    {
        if (screen != null) Destroy(screen);
        var rt = UIKit.Rect(name, uiRoot);
        UIKit.Stretch(rt);
        screen = rt.gameObject;
        return rt;
    }

    public void ShowHome() { HomeScreen.Build(NewScreen("Home")); }

    public void ShowMyTeam()
    {
        var rt = NewScreen("MyTeam");
        rt.gameObject.AddComponent<MyTeamScreen>().Build();
    }

    public void ShowPlay() { PlayScreen.Build(NewScreen("Play")); }

    public void StartMatch(ClubData club)
    {
        lastOpponent = club;
        if (screen != null) Destroy(screen);
        screen = null;
        var go = new GameObject("Match");
        match = go.AddComponent<MatchController>();
        match.Setup(Squad.UserSheet(), Squad.ClubSheet(club), camRig, uiRoot, OnMatchFinished);
    }

    void OnMatchFinished(int ours, int theirs, List<GoalEvent> goals)
    {
        Squad.RecordResult(ours, theirs);
        var sheets = match.sheets;
        EndMatchObjects();
        ResultScreen.Build(NewScreen("Result"), sheets[0], sheets[1], ours, theirs, goals, lastOpponent);
    }

    public void QuitMatch()
    {
        EndMatchObjects();
        ShowHome();
    }

    public void Rematch()
    {
        if (lastOpponent != null) StartMatch(lastOpponent);
        else ShowPlay();
    }

    void EndMatchObjects()
    {
        Time.timeScale = 1f;
        if (match != null)
        {
            if (match.hud != null) Destroy(match.hud.gameObject);
            Destroy(match.gameObject);
            match = null;
        }
        camRig.SetMenu();
    }
}
