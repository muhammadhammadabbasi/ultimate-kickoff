using System.Collections.Generic;
using UnityEngine;

public enum MatchState { KickOff, Play, SetPiece, Goal, HalfTime, FullTime, Paused }
public enum SetPieceType { ThrowIn, Corner, GoalKick }

public class GoalEvent
{
    public int team;
    public string scorer;
    public int minute;
    public bool ownGoal;
}

/// <summary>Runs an 11v11 match: state machine, clock, restarts, goals. AI lives in MatchController.AI.cs.</summary>
public partial class MatchController : MonoBehaviour
{
    public static MatchController I { get; private set; }

    public TeamSheet[] sheets = new TeamSheet[2];
    public readonly List<MatchPlayer>[] teams = { new List<MatchPlayer>(), new List<MatchPlayer>() };
    public readonly List<MatchPlayer> all = new List<MatchPlayer>();
    public MatchPlayer[] keepers = new MatchPlayer[2];
    public Ball ball;
    public MatchHUD hud;
    public CameraRig cam;

    public readonly int[] score = new int[2];
    public readonly List<GoalEvent> goals = new List<GoalEvent>();
    public float minute;
    public MatchState state;
    public int possessionTeam = -1;
    public MatchPlayer controlled;
    public bool secondHalf;
    public float shotCharge;
    public bool charging;

    float stateTimer;
    int kickoffTeam;
    float kickoffGraceUntil;
    float lastUserInput;
    MatchState stateBeforePause;
    GameObject ring;

    // Set piece
    public SetPieceType spType;
    public int spTeam;
    public MatchPlayer spTaker;
    Vector3 spSpot;
    float spAutoTime;

    // Difficulty (applies to the AI team)
    float aiReact, aiTackle, aiError, aiSpeed, aiKeeper;

    System.Action<int, int, List<GoalEvent>> onFinished;

    public bool Running => state != MatchState.Paused && state != MatchState.FullTime;

    // ================================================================== setup

    public void Setup(TeamSheet home, TeamSheet away, CameraRig cameraRig, Transform uiRoot, System.Action<int, int, List<GoalEvent>> finished)
    {
        I = this;
        sheets[0] = home;
        sheets[1] = away;
        cam = cameraRig;
        onFinished = finished;

        // Kit clash: away team switches to alternate kit
        if (Squad.ColorDistance(home.kit, away.kit) < 0.6f)
        {
            ClubData club = PlayerDatabase.Clubs.Find(c => c.abbr == away.abbr);
            away.kit = club != null ? club.kitAlt : Color.white;
            if (Squad.ColorDistance(home.kit, away.kit) < 0.6f) away.kit = new Color(0.95f, 0.95f, 0.95f);
            away.shorts = Squad.ColorDistance(away.kit, Color.white) < 0.5f ? new Color(0.1f, 0.1f, 0.12f) : Color.white;
        }

        switch (Squad.difficulty)
        {
            case 0: aiReact = 0.65f; aiTackle = 0.55f; aiError = 1.7f; aiSpeed = 0.92f; aiKeeper = 0.78f; break;
            case 2: aiReact = 0.24f; aiTackle = 1.1f; aiError = 0.75f; aiSpeed = 1.03f; aiKeeper = 1.08f; break;
            default: aiReact = 0.4f; aiTackle = 0.85f; aiError = 1.1f; aiSpeed = 0.98f; aiKeeper = 0.95f; break;
        }

        var boots = new[] { MatLib.Lit(HumanoidRig.BootColors[0], 0.5f), MatLib.Lit(HumanoidRig.BootColors[1], 0.5f),
            MatLib.Lit(HumanoidRig.BootColors[2], 0.5f), MatLib.Lit(HumanoidRig.BootColors[3], 0.5f), MatLib.Lit(HumanoidRig.BootColors[4], 0.5f) };
        for (int t = 0; t < 2; t++)
        {
            var sheet = sheets[t];
            var f = Formations.Get(sheet.formation);
            var shirt = MatLib.Lit(sheet.kit, 0.25f);
            var shorts = MatLib.Lit(sheet.shorts, 0.25f);
            var socks = MatLib.Lit(Color.Lerp(sheet.kit, sheet.shorts, 0.3f), 0.2f);
            var gkShirt = MatLib.Lit(sheet.keeperKit, 0.25f);
            var gkShorts = MatLib.Lit(new Color(0.1f, 0.1f, 0.12f), 0.2f);
            var gloves = MatLib.Lit(new Color(0.95f, 0.95f, 0.95f), 0.3f);
            for (int i = 0; i < 11; i++)
            {
                var go = new GameObject("Player");
                go.transform.SetParent(transform);
                var mp = go.AddComponent<MatchPlayer>();
                bool gk = f.slots[i].pos == Pos.GK;
                mp.Init(sheet.players[i], t, i, f.slots[i], gk ? gkShirt : shirt, gk ? gkShorts : shorts, gk ? gkShirt : socks,
                    boots[sheet.players[i].Hash % boots.Length], gloves);
                if (t == 1)
                {
                    mp.jogSpeed *= aiSpeed;
                    mp.sprintSpeed *= aiSpeed;
                }
                teams[t].Add(mp);
                all.Add(mp);
                if (gk) keepers[t] = mp;
            }
        }

        var ballGo = new GameObject("Ball");
        ballGo.transform.SetParent(transform);
        ball = ballGo.AddComponent<Ball>();
        ball.Init(MatLib.Textured(StadiumBuilder.BallTexture(), Color.white, Vector2.one, 0.45f));
        ball.TeleportFree(new Vector3(0f, Ball.R, 0f));

        ring = new GameObject("ControlRing");
        ring.transform.SetParent(transform);
        ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        ring.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        ring.AddComponent<MeshFilter>().sharedMesh = MeshUtil.DoubleQuad();
        var rr = ring.AddComponent<MeshRenderer>();
        rr.sharedMaterial = MatLib.Cutout(RingTexture(), new Color(1f, 0.9f, 0.15f), Vector2.one);
        rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        cam.SetBroadcast(ball.transform);

        var hudGo = new GameObject("MatchHUD", typeof(RectTransform));
        hudGo.transform.SetParent(uiRoot, false);
        UIKit.Stretch((RectTransform)hudGo.transform);
        hud = hudGo.AddComponent<MatchHUD>();
        hud.Build(this);

        Time.timeScale = 1f;
        StartKickoff(0, "KICK OFF");
    }

    static Texture2D RingTexture()
    {
        const int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, true);
        float r = s * 0.5f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                bool on = d > 0.72f && d < 0.95f;
                tex.SetPixel(x, y, on ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        tex.Apply(true);
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }

    void OnDestroy()
    {
        if (I == this) I = null;
        Time.timeScale = 1f;
    }

    // ================================================================== main loop

    void Update()
    {
        if (state == MatchState.Paused) return;
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        stateTimer += dt;

        switch (state)
        {
            case MatchState.KickOff:
                if (stateTimer > 1.3f) BeginPlayFromKickoff();
                break;
            case MatchState.Play:
            case MatchState.SetPiece:
                AdvanceClock(dt);
                break;
            case MatchState.Goal:
                if (stateTimer > 3.2f) StartKickoff(kickoffTeam, null);
                break;
            case MatchState.HalfTime:
                if (stateTimer > 2.6f)
                {
                    secondHalf = true;
                    StartKickoff(1, "SECOND HALF");
                }
                break;
            case MatchState.FullTime:
                if (stateTimer > 3f && onFinished != null)
                {
                    var cb = onFinished;
                    onFinished = null;
                    cb(score[0], score[1], goals);
                }
                break;
        }

        bool live = state == MatchState.Play || state == MatchState.SetPiece;
        if (live)
        {
            HandleUser(dt);
            ThinkTeam(0);
            ThinkTeam(1);
            if (state == MatchState.SetPiece) UpdateSetPiece();
            ThinkCarrier();
        }

        MoveEveryone(dt, live);
        for (int t = 0; t < 2; t++) UpdateKeeper(keepers[t], dt, live);

        if (live)
        {
            BallContacts();
            CheckBallOut();
            AutoSwitch();
        }

        UpdateRing();
        foreach (var p in all) p.Animate(dt, ball.Owner == p);
        if (hud != null) hud.Refresh();
    }

    void AdvanceClock(float dt)
    {
        float realHalf = Squad.HalfMinutesOptions[Squad.halfIndex] * 60f;
        minute += dt * 45f / realHalf;
        if (!secondHalf && minute >= 45f && state == MatchState.Play)
        {
            minute = 45f;
            SetState(MatchState.HalfTime);
            hud.Flash("HALF TIME", $"{sheets[0].abbr} {score[0]} - {score[1]} {sheets[1].abbr}");
            Freeze();
        }
        else if (secondHalf && minute >= 90f && state == MatchState.Play)
        {
            minute = 90f;
            SetState(MatchState.FullTime);
            hud.Flash("FULL TIME", $"{sheets[0].abbr} {score[0]} - {score[1]} {sheets[1].abbr}");
            Freeze();
        }
    }

    void SetState(MatchState s)
    {
        state = s;
        stateTimer = 0f;
    }

    void Freeze()
    {
        charging = false;
        foreach (var p in all)
        {
            p.velocity = Vector3.zero;
            p.locked = false;
        }
    }

    public void Pause()
    {
        if (state == MatchState.Paused || state == MatchState.FullTime) return;
        stateBeforePause = state;
        state = MatchState.Paused;
        Time.timeScale = 0f;
        GameInput.ClearAll();
    }

    public void Resume()
    {
        if (state != MatchState.Paused) return;
        state = stateBeforePause;
        Time.timeScale = 1f;
        GameInput.ClearAll();
    }

    // ================================================================== movement

    void MoveEveryone(float dt, bool live)
    {
        Vector3 bp = ball.Pos;
        foreach (var p in all)
        {
            if (p.isGK) continue;
            Vector3 desired = Vector3.zero;
            if (live || state == MatchState.Goal || state == MatchState.KickOff)
            {
                if (p.userControlled && !p.aiAssist && live)
                {
                    float spd = p.userSprint ? p.sprintSpeed : p.jogSpeed;
                    if (ball.Owner == p) spd *= 0.86f + p.dribbleSkill * 0.0012f;
                    desired = p.userDir * spd;
                }
                else
                {
                    Vector3 to = p.aiTarget - p.Pos;
                    to.y = 0f;
                    float d = to.magnitude;
                    if (d > 0.35f)
                    {
                        float spd = p.aiSprint ? p.sprintSpeed : p.jogSpeed;
                        if (ball.Owner == p) spd *= 0.86f + p.dribbleSkill * 0.0012f;
                        spd = Mathf.Min(spd, d * 2.2f + 0.6f);
                        desired = to / d * spd;
                    }
                }
            }
            p.Step(desired, dt, bp);
        }

        // Soft separation so players don't overlap
        for (int i = 0; i < all.Count; i++)
        {
            for (int j = i + 1; j < all.Count; j++)
            {
                var a = all[i];
                var b = all[j];
                Vector3 d = b.Pos - a.Pos;
                d.y = 0f;
                float m = d.magnitude;
                if (m < 0.75f && m > 0.0001f)
                {
                    Vector3 push = d / m * (0.75f - m) * 0.5f;
                    if (!a.isGK || !a.gkDiving) a.transform.position -= push;
                    if (!b.isGK || !b.gkDiving) b.transform.position += push;
                }
            }
        }
    }

    void UpdateRing()
    {
        if (controlled != null && state != MatchState.FullTime)
        {
            ring.SetActive(true);
            ring.transform.position = controlled.Pos + Vector3.up * 0.02f;
        }
        else ring.SetActive(false);
    }

    // ================================================================== kickoff / goals

    void StartKickoff(int team, string message)
    {
        kickoffTeam = team;
        SetState(MatchState.KickOff);
        possessionTeam = team;
        ball.TeleportFree(new Vector3(0f, Ball.R, 0f));
        ball.Place(Vector3.zero);
        spTaker = null;

        for (int t = 0; t < 2; t++)
        {
            Vector3 face = t == 0 ? Vector3.right : Vector3.left;
            foreach (var p in teams[t])
            {
                float depth = Mathf.Min(p.slotInfo.depth * 0.9f, 0.45f);
                if (p.isGK) depth = 0.01f;
                Vector3 pos = Pitch.ToWorld(t, depth, p.slotInfo.side * 0.85f);
                p.Teleport(pos, face);
                p.aiTarget = pos;
                p.locked = false;
                p.userControlled = false;
                p.aiAssist = false;
            }
        }

        // Kicker: most advanced player of the kicking team
        MatchPlayer kicker = null;
        float best = -1f;
        foreach (var p in teams[team])
        {
            if (p.isGK) continue;
            if (p.slotInfo.depth > best) { best = p.slotInfo.depth; kicker = p; }
        }
        Vector3 dir = team == 0 ? Vector3.right : Vector3.left;
        kicker.Teleport(-dir * 0.6f, dir);
        kicker.aiTarget = kicker.Pos;
        spTaker = kicker;

        controlled = null;
        if (team == 0) SetControlled(kicker);
        else SetControlled(ClosestTo(0, Vector3.zero, null));

        if (message != null) hud.Flash(message, null);
    }

    void BeginPlayFromKickoff()
    {
        SetState(MatchState.Play);
        ball.Possess(spTaker);
        possessionTeam = spTaker.team;
        kickoffGraceUntil = Time.time + 1.8f;
        spTaker.nextDecision = Time.time + 0.6f;
        spTaker = null;
    }

    void OnGoal(int scoringTeam)
    {
        score[scoringTeam]++;
        var kicker = ball.LastKicker;
        bool og = kicker != null && kicker.team != scoringTeam;
        var ev = new GoalEvent
        {
            team = scoringTeam,
            scorer = kicker != null ? kicker.data.name : "-",
            minute = Mathf.Clamp(Mathf.CeilToInt(minute), 1, 90),
            ownGoal = og
        };
        goals.Add(ev);
        SetState(MatchState.Goal);
        kickoffTeam = 1 - scoringTeam;
        charging = false;
        hud.Flash("GOAL!", ev.scorer + (og ? " (OG)" : "") + "  " + ev.minute + "'");
        foreach (var p in teams[scoringTeam])
        {
            if (p == kicker || Vector3.Distance(p.Pos, ball.Pos) < 25f) p.rig.Celebrate();
            p.aiTarget = kicker != null ? kicker.Pos : p.Pos;
            p.aiSprint = false;
        }
        foreach (var p in teams[1 - scoringTeam]) p.aiTarget = p.Pos;
    }

    // ================================================================== out of play / set pieces

    void CheckBallOut()
    {
        if (state != MatchState.Play) return;
        if (ball.Holder != null) return;
        Vector3 b = ball.Pos;
        if (Mathf.Abs(b.x) > Pitch.HalfL + Ball.R)
        {
            int side = b.x > 0f ? 1 : -1;
            if (Mathf.Abs(b.z) < Pitch.GoalHalfWidth - Ball.R && b.y < Pitch.GoalHeight - Ball.R)
            {
                OnGoal(side > 0 ? 0 : 1);
                return;
            }
            int defending = side > 0 ? 1 : 0;
            int last = ball.LastTouchTeam;
            if (last == defending)
            {
                float cz = Mathf.Sign(b.z == 0f ? 1f : b.z) * (Pitch.HalfW - 0.4f);
                StartSetPiece(SetPieceType.Corner, 1 - defending, new Vector3(side * (Pitch.HalfL - 0.4f), 0f, cz));
            }
            else
            {
                StartSetPiece(SetPieceType.GoalKick, defending, new Vector3(side * (Pitch.HalfL - 5.5f), 0f, Mathf.Sign(b.z == 0f ? 1f : b.z) * 5f));
            }
        }
        else if (Mathf.Abs(b.z) > Pitch.HalfW + Ball.R)
        {
            int team = ball.LastTouchTeam == 0 ? 1 : 0;
            float x = Mathf.Clamp(b.x, -Pitch.HalfL + 1f, Pitch.HalfL - 1f);
            StartSetPiece(SetPieceType.ThrowIn, team, new Vector3(x, 0f, Mathf.Sign(b.z) * Pitch.HalfW));
        }
    }

    void StartSetPiece(SetPieceType type, int team, Vector3 spot)
    {
        SetState(MatchState.SetPiece);
        spType = type;
        spTeam = team;
        spSpot = spot;
        possessionTeam = team;
        charging = false;
        ball.Place(spot);

        MatchPlayer taker;
        if (type == SetPieceType.GoalKick) taker = keepers[team];
        else taker = ClosestTo(team, spot, null);
        spTaker = taker;

        Vector3 toCenter = new Vector3(-spot.x * 0.3f, 0f, -spot.z);
        if (type == SetPieceType.GoalKick) toCenter = team == 0 ? Vector3.right : Vector3.left;
        toCenter.y = 0f;
        toCenter.Normalize();
        Vector3 stand = spot - toCenter * (type == SetPieceType.ThrowIn ? 0.3f : 0.7f);
        if (type == SetPieceType.ThrowIn) stand.z = Mathf.Sign(spot.z) * (Pitch.HalfW + 0.4f);
        taker.Teleport(stand, toCenter);
        taker.locked = true;
        taker.aiTarget = stand;

        // Opponents keep their distance
        float keep = type == SetPieceType.ThrowIn ? 3f : 9.15f;
        foreach (var p in teams[1 - team])
        {
            if (p.isGK) continue;
            Vector3 d = p.Pos - spot;
            d.y = 0f;
            if (d.magnitude < keep)
            {
                if (d.sqrMagnitude < 0.01f) d = -toCenter;
                p.Teleport(spot + d.normalized * keep, -d);
            }
        }

        if (team == 0 && !taker.isGK)
        {
            SetControlled(taker);
            spAutoTime = Time.time + 8f;
        }
        else
        {
            spAutoTime = Time.time + Random.Range(1.3f, 2f);
            if (team == 1 && controlled != null && controlled == taker) controlled = null;
        }

        string label = type == SetPieceType.ThrowIn ? "THROW-IN" : type == SetPieceType.Corner ? "CORNER" : "GOAL KICK";
        hud.Toast(label);
    }

    void UpdateSetPiece()
    {
        if (spTaker == null) return;
        if (stateTimer < 0.8f) return;
        bool userTaker = spTeam == 0 && !spTaker.isGK && controlled == spTaker;
        if (userTaker && Time.time < spAutoTime) return; // waiting for the user's button
        TakeSetPiece(spTaker, Vector3.zero, GameAction.Pass);
    }

    /// <summary>Execute the restart. action: Pass = short, Through = lofted, Shoot = cross/long.</summary>
    void TakeSetPiece(MatchPlayer taker, Vector3 aim, GameAction action)
    {
        int team = taker.team;
        Vector3 attackDir = team == 0 ? Vector3.right : Vector3.left;
        Vector3 from = ball.Pos;
        MatchPlayer target = null;
        Vector3 point;
        bool lofted;
        float height = Ball.R;

        if (spType == SetPieceType.Corner && (action != GameAction.Pass || team == 1 || aim == Vector3.zero))
        {
            // Cross into the box towards the best header
            Vector3 goal = Pitch.AttackGoal(team);
            float bestD = float.MaxValue;
            foreach (var p in teams[team])
            {
                if (p == taker || p.isGK) continue;
                float d = Vector3.Distance(p.Pos, goal + (-attackDir) * 9f);
                if (d < bestD) { bestD = d; target = p; }
            }
            point = target != null ? target.Pos : goal - attackDir * 8f;
            point += new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * ErrorScale(taker) * 1.5f;
            lofted = true;
            height = 1.7f;
        }
        else if (spType == SetPieceType.GoalKick)
        {
            target = PickRestartTarget(taker, action == GameAction.Pass ? 30f : 60f, action != GameAction.Pass);
            point = target != null ? target.Pos + target.velocity * 0.8f : from + attackDir * 40f;
            lofted = Vector3.Distance(from, point) > 22f;
        }
        else
        {
            Vector3 dir = aim.sqrMagnitude > 0.05f ? aim.normalized : Vector3.zero;
            target = dir != Vector3.zero ? BestMateInDirection(taker, dir, 35f) : PickRestartTarget(taker, 25f, false);
            if (target == null) target = PickRestartTarget(taker, 40f, false);
            point = target != null ? target.Pos + target.velocity * 0.5f : from + attackDir * 10f;
            lofted = spType == SetPieceType.ThrowIn || action == GameAction.Through || Vector3.Distance(from, point) > 25f;
        }

        if (spType == SetPieceType.ThrowIn)
        {
            ball.transform.position = new Vector3(from.x, 2.0f, from.z);
            taker.rig.PlayThrow();
        }
        else taker.rig.PlayKick();

        if (target != null) target.expectBallUntil = Time.time + 3f;
        KickToPoint(taker, point, lofted, 4.5f, height, target);
        taker.locked = false;
        spTaker = null;
        SetState(MatchState.Play);
        if (target != null && target.team == 0) SetControlled(target);
    }

    MatchPlayer PickRestartTarget(MatchPlayer taker, float maxDist, bool preferForward)
    {
        MatchPlayer best = null;
        float bestScore = float.MinValue;
        foreach (var p in teams[taker.team])
        {
            if (p == taker || p.isGK) continue;
            float d = Vector3.Distance(p.Pos, ball.Pos);
            if (d < 5f || d > maxDist) continue;
            float open = NearestOpponentDistance(p.team, p.Pos);
            float fwd = (Pitch.DepthOf(p.team, p.Pos) - Pitch.DepthOf(p.team, ball.Pos)) * Pitch.L;
            float s = open * 1.5f + (preferForward ? fwd * 0.4f : -Mathf.Abs(fwd) * 0.05f) - d * 0.1f + Random.Range(0f, 3f);
            if (s > bestScore) { bestScore = s; best = p; }
        }
        return best;
    }

    // ================================================================== helpers

    public MatchPlayer ClosestTo(int team, Vector3 point, MatchPlayer exclude)
    {
        MatchPlayer best = null;
        float bd = float.MaxValue;
        foreach (var p in teams[team])
        {
            if (p.isGK || p == exclude) continue;
            float d = (p.Pos - point).sqrMagnitude;
            if (d < bd) { bd = d; best = p; }
        }
        return best;
    }

    public float NearestOpponentDistance(int team, Vector3 point)
    {
        float best = 999f;
        foreach (var p in teams[1 - team])
        {
            float d = Vector3.Distance(new Vector3(p.Pos.x, 0, p.Pos.z), new Vector3(point.x, 0, point.z));
            if (d < best) best = d;
        }
        return best;
    }

    public void SetControlled(MatchPlayer p)
    {
        if (p != null && (p.team != 0 || p.isGK)) return;
        if (controlled == p) return;
        if (controlled != null)
        {
            controlled.userControlled = false;
            controlled.aiAssist = false;
            controlled.userDir = Vector3.zero;
        }
        controlled = p;
        charging = false;
        if (p != null)
        {
            p.userControlled = true;
            p.aiAssist = true;
        }
    }

    public float ErrorScale(MatchPlayer p)
    {
        float baseErr = p.team == 1 ? aiError : 0.8f;
        return baseErr;
    }
}
