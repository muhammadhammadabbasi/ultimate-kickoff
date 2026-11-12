using UnityEngine;
using UnityEngine.UI;

/// <summary>In-match HUD: scoreboard, clock, minimap, joystick, action buttons, name tags, messages, pause menu.</summary>
public class MatchHUD : MonoBehaviour
{
    MatchController m;
    RectTransform root;
    Text scoreText, clockText, msgMain, msgSub, toastText, nameTag, receiverTag, powerLabel;
    Image powerFill, marker;
    GameObject powerBar, pauseOverlay, msgGroup;
    RectTransform minimap;
    Image[] dots;
    Image ballDot;
    float msgUntil, toastUntil;
    int lastScoreA = -1, lastScoreB = -1, lastMinute = -1;

    public void Build(MatchController match)
    {
        m = match;
        root = (RectTransform)transform;

        // Scoreboard
        var board = UIKit.Img(root, "Scoreboard", UIKit.RoundedSmall, new Color(0.04f, 0.05f, 0.09f, 0.88f), new Vector2(0f, 1f), new Vector2(28f, -22f), new Vector2(560f, 78f));
        var k0 = UIKit.Img(board.transform, "Kit0", UIKit.RoundedSmall, m.sheets[0].kit, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(14f, 58f));
        UIKit.Outlined(k0, new Color(1f, 1f, 1f, 0.4f), 1f);
        UIKit.Txt(board.transform, m.sheets[0].abbr, 34, Color.white, new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(110f, 70f), TextAnchor.MiddleLeft);
        scoreText = UIKit.Txt(board.transform, "0 - 0", 40, Color.white, new Vector2(0f, 0.5f), new Vector2(150f, 0f), new Vector2(130f, 70f));
        var k1 = UIKit.Img(board.transform, "Kit1", UIKit.RoundedSmall, m.sheets[1].kit, new Vector2(0f, 0.5f), new Vector2(286f, 0f), new Vector2(14f, 58f));
        UIKit.Outlined(k1, new Color(1f, 1f, 1f, 0.4f), 1f);
        UIKit.Txt(board.transform, m.sheets[1].abbr, 34, Color.white, new Vector2(0f, 0.5f), new Vector2(308f, 0f), new Vector2(110f, 70f), TextAnchor.MiddleLeft);
        var clockBg = UIKit.Img(board.transform, "Clock", UIKit.RoundedSmall, new Color(0.1f, 0.85f, 0.35f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(128f, 62f));
        clockText = UIKit.FillText(clockBg.transform, "00:00", 36, new Color(0.02f, 0.1f, 0.04f));

        // Pause
        UIKit.Btn(root, "II", new Color(0.05f, 0.07f, 0.12f, 0.85f), new Vector2(1f, 1f), new Vector2(-28f, -22f), new Vector2(96f, 78f), () => OpenPause(), 38);

        // Minimap
        var mm = UIKit.Img(root, "Minimap", UIKit.RoundedSmall, new Color(0.1f, 0.4f, 0.15f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(300f, 192f));
        UIKit.Outlined(mm, new Color(1f, 1f, 1f, 0.5f), 2f);
        minimap = mm.rectTransform;
        UIKit.Img(mm.transform, "Half", null, new Color(1f, 1f, 1f, 0.45f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 192f));
        UIKit.Img(mm.transform, "Circle", UIKit.Ring, new Color(1f, 1f, 1f, 0.45f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50f, 50f));
        UIKit.Img(mm.transform, "BoxL", null, new Color(1f, 1f, 1f, 0.25f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(46f, 114f));
        UIKit.Img(mm.transform, "BoxR", null, new Color(1f, 1f, 1f, 0.25f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(46f, 114f));
        dots = new Image[m.all.Count];
        for (int i = 0; i < m.all.Count; i++)
        {
            var p = m.all[i];
            Color c = p.team == 0 ? new Color(0.35f, 0.95f, 1f) : new Color(1f, 0.35f, 0.3f);
            dots[i] = UIKit.Img(mm.transform, "Dot", p.team == 0 ? UIKit.Circle : UIKit.Triangle, c, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(13f, 13f));
        }
        ballDot = UIKit.Img(mm.transform, "BallDot", UIKit.Circle, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f));
        UIKit.Outlined(ballDot, Color.black, 1f);

        // Name tags (follow players)
        marker = UIKit.Img(root, "Marker", UIKit.Triangle, new Color(0.3f, 0.95f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 28f));
        nameTag = UIKit.Txt(root, "", 26, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 40f));
        UIKit.Shadowed(nameTag, 2f, 0.9f);
        receiverTag = UIKit.Txt(root, "", 22, new Color(1f, 0.95f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 36f));
        UIKit.Shadowed(receiverTag, 2f, 0.9f);

        BuildControls();

        // Messages
        msgGroup = UIKit.Rect("Messages", root).gameObject;
        UIKit.Stretch((RectTransform)msgGroup.transform);
        var band = UIKit.Img(msgGroup.transform, "Band", UIKit.Gradient, new Color(0f, 0f, 0f, 0.55f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(2600f, 250f));
        band.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        msgMain = UIKit.Txt(msgGroup.transform, "", 120, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, 175f), new Vector2(1600f, 160f));
        UIKit.Outlined(msgMain, new Color(0.05f, 0.1f, 0.3f, 0.9f), 4f);
        msgSub = UIKit.Txt(msgGroup.transform, "", 44, UIKit.Gold, new Vector2(0.5f, 0.5f), new Vector2(0f, 75f), new Vector2(1600f, 60f));
        UIKit.Shadowed(msgSub, 3f, 0.8f);
        msgGroup.SetActive(false);

        toastText = UIKit.Txt(root, "", 48, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(900f, 70f));
        UIKit.Outlined(toastText, new Color(0f, 0f, 0f, 0.8f), 3f);

        BuildPause();
    }

    void BuildControls()
    {
        // Joystick zone (left part of the screen)
        var zone = UIKit.Img(root, "StickZone", null, new Color(0f, 0f, 0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(760f, 640f), true);
        var baseRing = UIKit.Img(zone.transform, "StickBase", UIKit.Circle, new Color(1f, 1f, 1f, 0.12f), new Vector2(0f, 0f), new Vector2(110f, 90f), new Vector2(270f, 270f));
        baseRing.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        baseRing.rectTransform.anchoredPosition = new Vector2(245f, 225f);
        var ringOutline = UIKit.Img(baseRing.transform, "Outline", UIKit.Ring, new Color(1f, 1f, 1f, 0.45f));
        UIKit.Stretch(ringOutline.rectTransform);
        var knob = UIKit.Img(baseRing.transform, "Knob", UIKit.Circle, new Color(1f, 0.85f, 0.25f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 120f));
        knob.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        var stick = zone.gameObject.AddComponent<VirtualStick>();
        stick.zone = zone.rectTransform;
        stick.baseRing = baseRing.rectTransform;
        stick.knob = knob.rectTransform;
        stick.radius = 120f;

        MakeButton(GameAction.Sprint, "SPRINT", new Vector2(-170f, 150f), 205f, new Color(0.12f, 0.55f, 0.32f, 0.8f));
        MakeButton(GameAction.Shoot, "SHOOT", new Vector2(-165f, 395f), 180f, new Color(0.75f, 0.32f, 0.16f, 0.8f));
        MakeButton(GameAction.Through, "THROUGH", new Vector2(-385f, 300f), 165f, new Color(0.62f, 0.5f, 0.12f, 0.8f));
        MakeButton(GameAction.Pass, "PASS", new Vector2(-410f, 115f), 170f, new Color(0.13f, 0.35f, 0.62f, 0.8f));

        powerBar = UIKit.Img(root, "PowerBg", UIKit.RoundedSmall, new Color(0f, 0f, 0f, 0.7f), new Vector2(1f, 0f), new Vector2(-300f, 560f), new Vector2(380f, 34f)).gameObject;
        powerBar.GetComponent<Image>().rectTransform.pivot = new Vector2(0.5f, 0.5f);
        powerFill = UIKit.Img(powerBar.transform, "Fill", UIKit.RoundedSmall, new Color(1f, 0.6f, 0.1f));
        UIKit.Stretch(powerFill.rectTransform, 4, 4, 4, 4);
        powerFill.type = Image.Type.Filled;
        powerFill.fillMethod = Image.FillMethod.Horizontal;
        powerLabel = UIKit.Txt(powerBar.transform, "POWER", 26, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, 24f), new Vector2(380f, 36f));
        UIKit.Shadowed(powerLabel, 2f, 0.9f);
        powerBar.SetActive(false);
    }

    void MakeButton(GameAction a, string label, Vector2 pos, float size, Color color)
    {
        var face = UIKit.Img(root, "Btn_" + label, UIKit.Circle, color, new Vector2(1f, 0f), pos, new Vector2(size, size), true);
        face.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        var ring = UIKit.Img(face.transform, "Ring", UIKit.Ring, new Color(1f, 1f, 1f, 0.55f));
        UIKit.Stretch(ring.rectTransform, -6, -6, -6, -6);
        var shine = UIKit.Img(face.transform, "Shine", UIKit.Glow, new Color(1f, 1f, 1f, 0.18f), new Vector2(0.5f, 0.5f), new Vector2(0f, size * 0.15f), new Vector2(size * 0.7f, size * 0.5f));
        shine.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        var t = UIKit.FillText(face.transform, label, Mathf.RoundToInt(size * 0.17f), Color.white);
        UIKit.Shadowed(t, 2f, 0.7f);
        var b = face.gameObject.AddComponent<TouchButton>();
        b.Setup(a, face, t);
    }

    void BuildPause()
    {
        pauseOverlay = UIKit.Img(root, "Pause", null, new Color(0f, 0f, 0f, 0.7f), true).gameObject;
        UIKit.Stretch((RectTransform)pauseOverlay.transform);
        var panel = UIKit.Img(pauseOverlay.transform, "Panel", UIKit.Rounded, UIKit.Panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 520f));
        panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        UIKit.Txt(panel.transform, "PAUSED", 64, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(560f, 90f));
        UIKit.Btn(panel.transform, "RESUME", UIKit.Green, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(420f, 110f), () => ClosePause(), 40);
        UIKit.Btn(panel.transform, "QUIT MATCH", UIKit.Red, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(420f, 100f), () =>
        {
            Time.timeScale = 1f;
            AppController.I.QuitMatch();
        }, 36);
        pauseOverlay.SetActive(false);
    }

    void OpenPause()
    {
        m.Pause();
        pauseOverlay.SetActive(true);
        pauseOverlay.transform.SetAsLastSibling();
    }

    void ClosePause()
    {
        pauseOverlay.SetActive(false);
        m.Resume();
    }

    public void Flash(string main, string sub)
    {
        msgGroup.SetActive(true);
        msgGroup.transform.SetAsLastSibling();
        msgMain.text = main;
        msgSub.text = sub ?? "";
        msgUntil = Time.unscaledTime + 2.6f;
        msgMain.transform.localScale = Vector3.one * 1.6f;
    }

    public void Toast(string text)
    {
        toastText.text = text;
        toastUntil = Time.unscaledTime + 1.6f;
    }

    public void Refresh()
    {
        if (GameInput.BackPressed())
        {
            if (pauseOverlay.activeSelf) ClosePause();
            else OpenPause();
        }

        if (m.score[0] != lastScoreA || m.score[1] != lastScoreB)
        {
            lastScoreA = m.score[0];
            lastScoreB = m.score[1];
            scoreText.text = lastScoreA + " - " + lastScoreB;
        }
        int secs = Mathf.FloorToInt(m.minute * 60f);
        if (secs / 10 != lastMinute)
        {
            lastMinute = secs / 10;
            clockText.text = (secs / 60).ToString("00") + ":" + (secs % 60 / 10 * 10).ToString("00");
        }

        // Messages
        if (msgGroup.activeSelf)
        {
            msgMain.transform.localScale = Vector3.Lerp(msgMain.transform.localScale, Vector3.one, Time.unscaledDeltaTime * 10f);
            if (Time.unscaledTime > msgUntil) msgGroup.SetActive(false);
        }
        toastText.enabled = Time.unscaledTime < toastUntil;

        // Button labels
        bool setPieceTaker = m.state == MatchState.SetPiece && m.spTaker != null && m.spTaker == m.controlled;
        if (setPieceTaker)
        {
            bool corner = m.spType == SetPieceType.Corner;
            bool throwIn = m.spType == SetPieceType.ThrowIn;
            GameInput.SetLabel(GameAction.Pass, corner ? "SHORT" : throwIn ? "THROW" : "PASS");
            GameInput.SetLabel(GameAction.Through, "LOB");
            GameInput.SetLabel(GameAction.Shoot, corner ? "CROSS" : "LONG");
        }
        else if (m.possessionTeam == 0)
        {
            GameInput.SetLabel(GameAction.Pass, "PASS");
            GameInput.SetLabel(GameAction.Through, "THROUGH");
            GameInput.SetLabel(GameAction.Shoot, "SHOOT");
        }
        else
        {
            GameInput.SetLabel(GameAction.Pass, "SWITCH");
            GameInput.SetLabel(GameAction.Through, "PRESS");
            GameInput.SetLabel(GameAction.Shoot, "TACKLE");
        }
        GameInput.SetLabel(GameAction.Sprint, "SPRINT");

        powerBar.SetActive(m.charging);
        if (m.charging)
        {
            float v = m.shotCharge;
            bool tap = v < 0.17f;
            powerFill.fillAmount = Mathf.Max(v, 0.04f);
            powerFill.color = v < 0.5f ? Color.Lerp(new Color(0.3f, 0.9f, 0.3f), new Color(1f, 0.85f, 0.15f), v * 2f)
                                       : Color.Lerp(new Color(1f, 0.85f, 0.15f), new Color(1f, 0.25f, 0.15f), (v - 0.5f) * 2f);
            string what = m.chargeKind == 3 ? "SHOT" : m.chargeKind == 2 ? "THROUGH" : "PASS";
            powerLabel.text = tap ? what + " POWER (hold)" : what + " POWER " + Mathf.RoundToInt(v * 100f) + "%";
        }

        // Minimap
        Vector2 size = minimap.rect.size;
        for (int i = 0; i < dots.Length; i++)
        {
            var p = m.all[i];
            dots[i].rectTransform.anchoredPosition = MapPos(p.Pos, size);
            bool ctrl = p == m.controlled;
            dots[i].color = ctrl ? new Color(1f, 0.9f, 0.15f) : (p.team == 0 ? new Color(0.35f, 0.95f, 1f) : new Color(1f, 0.35f, 0.3f));
            dots[i].rectTransform.sizeDelta = ctrl ? new Vector2(18f, 18f) : new Vector2(13f, 13f);
        }
        ballDot.rectTransform.anchoredPosition = MapPos(m.ball.Pos, size);
        ballDot.transform.SetAsLastSibling();

        // Name tags
        var cam = Camera.main;
        var c = m.controlled;
        if (cam != null && c != null && m.state != MatchState.FullTime)
        {
            marker.enabled = nameTag.enabled = true;
            marker.rectTransform.anchoredPosition = WorldToHud(cam, c.Pos + Vector3.up * 2.35f);
            nameTag.rectTransform.anchoredPosition = WorldToHud(cam, c.Pos) + new Vector2(0f, -26f);
            nameTag.text = c.data.name;
        }
        else marker.enabled = nameTag.enabled = false;

        var r = (m.charging && m.previewMate != null) ? m.previewMate : m.ball.IntendedReceiver;
        if (cam != null && r != null && r != c && (m.charging || m.ball.IsFree) && r.team == 0)
        {
            receiverTag.enabled = true;
            receiverTag.rectTransform.anchoredPosition = WorldToHud(cam, r.Pos) + new Vector2(0f, -24f);
            receiverTag.text = r.data.name;
        }
        else receiverTag.enabled = false;
    }

    static Vector2 MapPos(Vector3 world, Vector2 size)
    {
        float x = Mathf.Clamp(world.x / Pitch.L, -0.5f, 0.5f) * size.x;
        float y = Mathf.Clamp(world.z / Pitch.W, -0.5f, 0.5f) * size.y;
        return new Vector2(x, y);
    }

    Vector2 WorldToHud(Camera cam, Vector3 world)
    {
        Vector3 sp = cam.WorldToScreenPoint(world);
        if (sp.z < 0f) return new Vector2(-5000f, -5000f);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, null, out local);
        return local;
    }
}
