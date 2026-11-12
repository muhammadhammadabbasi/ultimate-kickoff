using UnityEngine;

/// <summary>User controls, team AI, ball carrier AI, goalkeepers, tackles and ball contacts.</summary>
public partial class MatchController
{
    struct PassOption
    {
        public MatchPlayer mate;
        public float score;
        public bool through;
    }

    GameAction queuedAction;
    float queuedUntil;
    readonly float[] nextRunPick = new float[2];

    static Vector3 AttackDir(int team) { return team == 0 ? Vector3.right : Vector3.left; }

    static float Flat(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x, dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    static Vector3 ClampInPitch(Vector3 p, float margin)
    {
        p.x = Mathf.Clamp(p.x, -Pitch.HalfL + margin, Pitch.HalfL - margin);
        p.z = Mathf.Clamp(p.z, -Pitch.HalfW + margin, Pitch.HalfW - margin);
        p.y = 0f;
        return p;
    }

    // ================================================================== user

    void HandleUser(float dt)
    {
        Vector2 mv = GameInput.Move;
        Vector3 dir = new Vector3(mv.x, 0f, mv.y);
        bool sprint = GameInput.Held(GameAction.Sprint);
        bool passDown = GameInput.Pressed(GameAction.Pass);
        bool throughDown = GameInput.Pressed(GameAction.Through);
        bool throughHeld = GameInput.Held(GameAction.Through);
        bool shootDown = GameInput.Pressed(GameAction.Shoot);
        bool shootUp = GameInput.Released(GameAction.Shoot);
        bool shootHeld = GameInput.Held(GameAction.Shoot);
        if (dir.sqrMagnitude > 0.02f) lastUserInput = Time.time;

        var c = controlled;
        if (c == null) { charging = false; return; }

        if (state == MatchState.SetPiece && spTaker == c)
        {
            c.userDir = Vector3.zero;
            c.aiAssist = false;
            if (dir.sqrMagnitude > 0.05f) c.transform.rotation = Quaternion.RotateTowards(c.transform.rotation, Quaternion.LookRotation(dir), 360f * dt);
            if (passDown) TakeSetPiece(c, dir, GameAction.Pass);
            else if (throughDown) TakeSetPiece(c, dir, GameAction.Through);
            else if (shootDown) TakeSetPiece(c, dir, GameAction.Shoot);
            return;
        }

        bool hasBall = ball.Owner == c;
        bool attacking = possessionTeam == 0;
        c.userSprint = sprint;
        c.userDir = dir;
        c.aiAssist = !hasBall && dir.sqrMagnitude < 0.02f && Time.time - lastUserInput > 0.35f;

        if (hasBall)
        {
            c.aiAssist = false;
            if (passDown) { UserPass(c, dir, false); return; }
            if (throughDown) { UserPass(c, dir, true); return; }
            if (shootDown) { charging = true; shotCharge = 0f; }
            if (charging)
            {
                shotCharge = Mathf.Min(1f, shotCharge + dt / 0.9f);
                if (shootUp || !shootHeld)
                {
                    charging = false;
                    UserShoot(c, dir, shotCharge);
                }
            }
            return;
        }

        charging = false;
        if (attacking && ball.IsFree)
        {
            // First-time pass / shot: queued until the ball arrives
            if (passDown) Queue(GameAction.Pass);
            if (throughDown) Queue(GameAction.Through);
            if (shootDown) Queue(GameAction.Shoot);
        }
        else
        {
            if (passDown) SwitchToBest();
            if (shootDown) UserTackle(controlled);
            var carrier = ball.Owner;
            if (throughHeld && carrier != null && carrier.team == 1 && controlled != null)
            {
                var cc = controlled;
                cc.aiAssist = false;
                Vector3 to = carrier.Pos + carrier.velocity * 0.2f - cc.Pos;
                to.y = 0f;
                cc.userDir = to.magnitude > 0.4f ? to.normalized : Vector3.zero;
                cc.userSprint = sprint || to.magnitude > 4f;
                lastUserInput = Time.time;
            }
        }
    }

    void Queue(GameAction a)
    {
        queuedAction = a;
        queuedUntil = Time.time + 0.9f;
    }

    void SwitchToBest()
    {
        Vector3 bp = ball.IsFree ? ball.Predict(0.4f) : ball.Pos;
        var next = ClosestTo(0, bp, controlled);
        if (next != null)
        {
            SetControlled(next);
            lastUserInput = Time.time;
        }
    }

    void UserPass(MatchPlayer c, Vector3 dir, bool through)
    {
        Vector3 aim = dir.sqrMagnitude > 0.09f ? dir.normalized : c.transform.forward;
        var mate = BestMateInDirection(c, aim, 45f);
        if (mate == null)
        {
            Vector3 pt = ClampInPitch(c.Pos + aim * (through ? 18f : 10f), 0.5f);
            KickToPoint(c, pt, through, 3f, Ball.R, null);
            return;
        }
        DoPass(c, mate, through);
    }

    void UserShoot(MatchPlayer c, Vector3 dir, float charge)
    {
        Vector3 goal = Pitch.AttackGoal(0);
        float side;
        if (Mathf.Abs(dir.z) > 0.35f) side = Mathf.Sign(dir.z);
        else if (Mathf.Abs(c.Pos.z) > 0.5f) side = -Mathf.Sign(c.Pos.z);
        else side = Random.value < 0.5f ? -1f : 1f;
        ShootTowards(c, goal, side, charge);
    }

    void ShootTowards(MatchPlayer c, Vector3 goal, float side, float charge)
    {
        float tz = side * Random.Range(1.9f, 3.1f);
        float ty = Mathf.Lerp(0.35f, 1.95f, charge) + Random.Range(-0.2f, 0.3f);
        float dist = Flat(c.Pos, goal);
        float err = ((1f - c.shotSkill / 100f) * 2.4f + charge * charge * 0.8f + dist * 0.035f) * ErrorScale(c);
        var target = new Vector3(goal.x + 0.4f * Mathf.Sign(goal.x), ty + Random.Range(-1f, 1f) * err * 0.5f, tz + Random.Range(-1f, 1f) * err);
        float speed = Mathf.Lerp(15f, 29f, charge) * (0.85f + c.shotSkill * 0.0018f);
        ShootAt(c, target, speed);
    }

    void UserTackle(MatchPlayer c)
    {
        if (c == null || Time.time < c.nextTackleTime || c.Stunned) return;
        c.nextTackleTime = Time.time + 0.6f;
        var carrier = ball.Owner;
        if (carrier == null || carrier.team == 0) return;
        Vector3 to = ball.Pos - c.Pos;
        to.y = 0f;
        float d = to.magnitude;
        if (d > 0.01f) c.velocity = to / d * c.sprintSpeed;
        if (d > 2.3f) return; // lunge only
        if (Time.time - ball.PossessTime < 0.25f) return;
        ResolveTackle(c, carrier, 0.08f);
    }

    // ================================================================== kicking helpers

    MatchPlayer BestMateInDirection(MatchPlayer from, Vector3 aim, float maxAngle)
    {
        MatchPlayer best = null;
        float bestScore = float.MaxValue;
        foreach (var m in teams[from.team])
        {
            if (m == from) continue;
            Vector3 v = m.Pos - from.Pos;
            v.y = 0f;
            float d = v.magnitude;
            if (d < 2.5f || d > 55f) continue;
            float ang = Vector3.Angle(aim, v);
            if (ang > maxAngle) continue;
            if (m.isGK && ang > 20f) continue;
            float score = ang * 1.3f + d * 0.6f;
            if (score < bestScore) { bestScore = score; best = m; }
        }
        return best;
    }

    void DoPass(MatchPlayer from, MatchPlayer mate, bool through)
    {
        float d = Flat(from.Pos, mate.Pos);
        Vector3 point;
        bool lofted;
        if (through)
        {
            Vector3 runDir = mate.velocity.sqrMagnitude > 4f ? mate.velocity.normalized : AttackDir(from.team);
            runDir = (runDir + AttackDir(from.team) * 0.8f).normalized;
            point = ClampInPitch(mate.Pos + runDir * (5f + d * 0.15f), 1.5f);
            lofted = d > 28f || LaneClearance(from.team, ball.Pos, point) < 1.4f;
            mate.makingRun = true;
            mate.runUntil = Time.time + 2.5f;
        }
        else
        {
            float t = d / 14f;
            point = mate.Pos + mate.velocity * Mathf.Min(t, 1.1f);
            lofted = d > 32f || (d > 18f && LaneClearance(from.team, ball.Pos, point) < 1.2f);
        }
        float err = (1f - from.passSkill / 100f) * (0.5f + d * 0.06f) * ErrorScale(from);
        point += new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * err;
        mate.expectBallUntil = Time.time + 3f;
        KickToPoint(from, point, lofted, through ? 2.5f : 4.5f, Ball.R, mate);
        if (from.team == 0) SetControlled(mate);
    }

    void KickToPoint(MatchPlayer from, Vector3 point, bool lofted, float arriveSpeed, float height, MatchPlayer receiver, bool anim = true)
    {
        Vector3 p0 = ball.Pos;
        Vector3 d = point - p0;
        d.y = 0f;
        float dist = Mathf.Max(d.magnitude, 0.5f);
        Vector3 dir = d / dist;
        Vector3 vel;
        if (!lofted)
        {
            float v0 = Mathf.Clamp(arriveSpeed + Ball.GroundDamping * dist, 5f, 32f);
            vel = dir * v0 + Vector3.up * 0.5f;
        }
        else
        {
            float T = Mathf.Clamp(0.55f + dist * 0.032f, 0.7f, 2.3f);
            float vh = dist / T * 1.04f;
            float vy = (height - p0.y) / T + 0.5f * 9.81f * T;
            vel = dir * vh + Vector3.up * vy;
        }
        if (anim) from.rig.PlayKick();
        ball.Kick(vel, from, receiver, false);
        possessionTeam = from.team;
    }

    void ShootAt(MatchPlayer from, Vector3 target, float speed)
    {
        Vector3 p0 = ball.Pos;
        Vector3 d = target - p0;
        Vector3 dh = new Vector3(d.x, 0f, d.z);
        float D = Mathf.Max(dh.magnitude, 0.5f);
        float t = D / speed;
        float vy = (target.y - p0.y) / t + 0.5f * 9.81f * t;
        Vector3 vel = dh / D * speed * 1.02f + Vector3.up * vy;
        from.rig.PlayKick();
        ball.Kick(vel, from, null, true);
        possessionTeam = from.team;
    }

    float LaneClearance(int team, Vector3 a, Vector3 b)
    {
        float best = 99f;
        Vector3 ab = b - a;
        ab.y = 0f;
        float len2 = ab.sqrMagnitude;
        if (len2 < 0.01f) return best;
        foreach (var o in teams[1 - team])
        {
            Vector3 ao = o.Pos - a;
            ao.y = 0f;
            float t = Vector3.Dot(ao, ab) / len2;
            if (t < 0.05f || t > 1.05f) continue;
            Vector3 closest = a + ab * t;
            float dd = Flat(o.Pos, closest);
            if (dd < best) best = dd;
        }
        return best;
    }

    bool BlockerAhead(MatchPlayer p, Vector3 goal, float width)
    {
        Vector3 ab = goal - p.Pos;
        ab.y = 0f;
        float len2 = ab.sqrMagnitude;
        foreach (var o in teams[1 - p.team])
        {
            if (o.isGK) continue;
            Vector3 ao = o.Pos - p.Pos;
            ao.y = 0f;
            float t = Vector3.Dot(ao, ab) / len2;
            if (t < 0f || t > 1f) continue;
            if (Flat(o.Pos, p.Pos + ab * t) < width) return true;
        }
        return false;
    }

    Vector3 InterceptPoint(MatchPlayer p)
    {
        for (float t = 0.1f; t <= 3f; t += 0.1f)
        {
            Vector3 bp = ball.Predict(t);
            bp.y = 0f;
            if (Flat(p.Pos, bp) <= p.sprintSpeed * t + 0.6f) return bp;
        }
        Vector3 end = ball.Predict(3f);
        end.y = 0f;
        return end;
    }

    float OpponentLineDepth(int team, float ballDepth)
    {
        float max = 0f;
        foreach (var o in teams[1 - team])
        {
            if (o.isGK) continue;
            float d = Pitch.DepthOf(team, o.Pos);
            if (d > max) max = d;
        }
        return Mathf.Max(max, ballDepth, 0.5f);
    }

    // ================================================================== team shape

    void ThinkTeam(int t)
    {
        var carrier = ball.Owner;
        Vector3 bp = ball.IsFree ? ball.Predict(0.4f) : ball.Pos;
        float bd = Mathf.Clamp01(Pitch.DepthOf(t, bp));
        float bs = Mathf.Clamp(Pitch.SideOf(t, bp), -1f, 1f);
        bool attack = possessionTeam == t;
        bool grace = Time.time < kickoffGraceUntil && t != kickoffTeam;
        bool corner = state == MatchState.SetPiece && spType == SetPieceType.Corner;

        // Who presses the ball
        MatchPlayer chaser = null, cover = null;
        bool receiverComing = ball.IsFree && ball.IntendedReceiver != null && ball.IntendedReceiver.team == t;
        if (state == MatchState.Play && !grace && !receiverComing && (!attack || ball.IsFree) && ball.Holder == null)
        {
            float b1 = float.MaxValue, b2 = float.MaxValue;
            foreach (var p in teams[t])
            {
                if (p.isGK || p.Stunned) continue;
                if (p.userControlled && !p.aiAssist) continue;
                Vector3 target = carrier != null ? carrier.Pos : InterceptPoint(p);
                float time = Flat(p.Pos, target) / p.sprintSpeed;
                if (time < b1) { b2 = b1; cover = chaser; b1 = time; chaser = p; }
                else if (time < b2) { b2 = time; cover = p; }
            }
            // When the user is actively defending, his own player is the presser
            if (t == 0 && controlled != null && !controlled.aiAssist && !attack)
            {
                cover = chaser;
                chaser = null;
            }
        }

        // Forward runs in behind
        if (attack && carrier != null && Time.time > nextRunPick[t] && Pitch.DepthOf(t, carrier.Pos) > 0.35f)
        {
            nextRunPick[t] = Time.time + Random.Range(2.5f, 5f);
            MatchPlayer pick = null;
            int n = 0;
            foreach (var p in teams[t])
            {
                if (p == carrier || p.isGK || p.group == PosGroup.DEF) continue;
                if (p.group == PosGroup.MID && p.slotInfo.depth < 0.42f) continue;
                n++;
                if (Random.Range(0, n) == 0) pick = p;
            }
            if (pick != null)
            {
                pick.makingRun = true;
                pick.runUntil = Time.time + 2.8f;
            }
        }

        float lineDepth = OpponentLineDepth(t, bd);

        foreach (var p in teams[t])
        {
            if (p.isGK || p == carrier) continue;
            if (p.userControlled && !p.aiAssist) continue;
            if (p.locked) continue;

            FormationSlot s = p.slotInfo;
            float d, sd;
            bool sprint = false;
            if (attack)
            {
                d = s.depth + (bd - 0.5f) * 0.55f + 0.13f;
                if (p.group == PosGroup.ATT) d += 0.04f;
                if (p.group == PosGroup.DEF) d = Mathf.Min(d, bd + 0.05f);
                sd = s.side * 0.95f + bs * 0.2f;
                if (p.makingRun && Time.time < p.runUntil)
                {
                    d = Mathf.Max(d, Mathf.Min(lineDepth + 0.05f, 0.93f));
                    sprint = true;
                }
                else
                {
                    p.makingRun = false;
                    d = Mathf.Min(d, lineDepth);
                }
                d = Mathf.Clamp(d, 0.05f, 0.93f);
            }
            else
            {
                d = s.depth * 0.95f + (bd - 0.5f) * 0.62f + 0.02f;
                sd = s.side * 0.72f + bs * 0.32f;
                if (p.group == PosGroup.DEF) d = Mathf.Min(d, bd - 0.035f);
                else if (p.group == PosGroup.MID) d = Mathf.Min(d, bd + 0.06f);
                d = Mathf.Clamp(d, 0.035f, 0.78f);
            }
            sd = Mathf.Clamp(sd, -0.94f, 0.94f);
            Vector3 target = Pitch.ToWorld(t, d, sd);

            if (corner) target = CornerSpot(p, t, target);

            if (p == chaser)
            {
                target = carrier != null ? carrier.Pos + carrier.velocity * 0.25f : InterceptPoint(p);
                sprint = true;
            }
            else if (p == cover)
            {
                Vector3 own = Pitch.GoalCenter(t);
                Vector3 from = carrier != null ? carrier.Pos : bp;
                Vector3 toGoal = own - from;
                toGoal.y = 0f;
                target = from + toGoal.normalized * 5f;
                sprint = Flat(p.Pos, target) > 6f;
            }

            if (ball.IsFree && (ball.IntendedReceiver == p || (Time.time < p.expectBallUntil && ball.LastTouchTeam == t)))
            {
                target = InterceptPoint(p);
                sprint = true;
            }

            if (Flat(p.Pos, target) > 10f) sprint = true;
            p.aiTarget = target;
            p.aiSprint = sprint;
        }
    }

    Vector3 CornerSpot(MatchPlayer p, int t, Vector3 fallback)
    {
        if (t == spTeam)
        {
            bool goes = p.group == PosGroup.ATT || p.slotInfo.pos == Pos.CB || p.slotInfo.pos == Pos.CAM || (p.slotInfo.pos == Pos.CM && p.slot % 2 == 0);
            if (!goes) return fallback;
            Vector3 goal = Pitch.AttackGoal(t);
            float inward = -Mathf.Sign(goal.x);
            float depth = 5f + (p.slot * 3.7f) % 7f;
            float z = (p.slot * 5.3f) % 12f - 6f;
            return new Vector3(goal.x + inward * depth, 0f, z);
        }
        else
        {
            if (p.group == PosGroup.ATT && p.slotInfo.depth > 0.6f) return fallback;
            Vector3 goal = Pitch.GoalCenter(t);
            float inward = -Mathf.Sign(goal.x);
            float depth = 2.5f + (p.slot * 2.9f) % 8f;
            float z = (p.slot * 4.1f) % 14f - 7f;
            return new Vector3(goal.x + inward * depth, 0f, z);
        }
    }

    // ================================================================== ball carrier AI (opponent)

    void ThinkCarrier()
    {
        var p = ball.Owner;
        if (p == null || p.team != 1) return;

        Vector3 goal = Pitch.AttackGoal(p.team);
        Vector3 toGoal = goal - p.Pos;
        toGoal.y = 0f;
        float distG = toGoal.magnitude;
        float pressure = NearestOpponentDistance(p.team, p.Pos);

        if (Time.time >= p.nextDecision)
        {
            p.nextDecision = Time.time + aiReact * Random.Range(0.7f, 1.3f);
            bool fromKickoff = Time.time < kickoffGraceUntil;

            if (!fromKickoff && distG < 26f)
            {
                float chance = distG < 11f ? 0.9f : distG < 17f ? 0.55f : 0.16f;
                if (Mathf.Abs(p.Pos.z) > 18f) chance *= 0.3f;
                if (BlockerAhead(p, goal, 1.8f)) chance *= 0.45f;
                if (Random.value < chance)
                {
                    var gk = keepers[0];
                    float side = gk != null && Mathf.Abs(gk.Pos.z) > 0.3f ? -Mathf.Sign(gk.Pos.z) : (Random.value < 0.5f ? -1f : 1f);
                    ShootTowards(p, goal, side, Random.Range(0.35f, 0.85f));
                    return;
                }
            }

            var opt = EvaluatePasses(p);
            float dribbleValue = pressure > 6f ? 9f : pressure > 3f ? 4f : -4f;
            if (p.group == PosGroup.DEF && Pitch.DepthOf(p.team, p.Pos) < 0.3f) dribbleValue -= 4f;
            if (fromKickoff) dribbleValue = -100f;
            if (opt.mate != null && (opt.score > dribbleValue || (pressure < 2.2f && opt.score > -6f)))
            {
                DoPass(p, opt.mate, opt.through);
                return;
            }
        }

        // Dribble towards goal, avoiding defenders
        Vector3 steer = toGoal / Mathf.Max(distG, 0.01f);
        foreach (var o in teams[1 - p.team])
        {
            Vector3 away = p.Pos - o.Pos;
            away.y = 0f;
            float d = away.magnitude;
            if (d > 7f || d < 0.01f) continue;
            float ahead = Vector3.Dot(-away / d, steer);
            if (ahead < 0.2f) continue;
            Vector3 lat = away - Vector3.Project(away, steer);
            if (lat.sqrMagnitude < 0.01f) lat = Vector3.Cross(Vector3.up, steer) * (p.slot % 2 == 0 ? 1f : -1f);
            steer += lat.normalized * (7f - d) / 7f * 1.3f * ahead;
        }
        if (Mathf.Abs(p.Pos.z) > Pitch.HalfW - 4f) steer.z -= Mathf.Sign(p.Pos.z) * 0.8f;
        steer.y = 0f;
        steer.Normalize();
        p.aiTarget = p.Pos + steer * 4f;
        p.aiSprint = pressure > 3f && distG > 18f;
    }

    PassOption EvaluatePasses(MatchPlayer p)
    {
        var best = new PassOption { mate = null, score = float.MinValue, through = false };
        float myDepth = Pitch.DepthOf(p.team, p.Pos);
        Vector3 attack = AttackDir(p.team);
        foreach (var m in teams[p.team])
        {
            if (m == p || m.isGK) continue;
            Vector3 target = m.Pos + m.velocity * 0.5f;
            float d = Flat(p.Pos, target);
            if (d < 5f || d > 42f) continue;
            float lane = LaneClearance(p.team, ball.Pos, target);
            float open = NearestOpponentDistance(p.team, target);
            bool lofted = d > 28f;
            if (!lofted && lane < 1.6f) continue;
            if (open < 1.8f) continue;
            float progress = (Pitch.DepthOf(p.team, target) - myDepth) * Pitch.L;
            float s = progress * 0.55f + Mathf.Min(open, 8f) * 1.1f + Mathf.Min(lane, 5f) * 0.5f - (lofted ? 4f : 0f) + Random.Range(0f, 3f);
            if (s > best.score) { best.mate = m; best.score = s; best.through = false; }

            if (m.group != PosGroup.DEF && Pitch.DepthOf(p.team, m.Pos) > myDepth + 0.04f)
            {
                Vector3 run = m.velocity.sqrMagnitude > 1f ? m.velocity.normalized * 0.5f : Vector3.zero;
                Vector3 tp = ClampInPitch(m.Pos + (attack + run).normalized * 7f, 3f);
                float openT = NearestOpponentDistance(p.team, tp);
                float mine = Flat(m.Pos, tp);
                if (openT > mine + 1.5f)
                {
                    float laneT = LaneClearance(p.team, ball.Pos, tp);
                    float st = (Pitch.DepthOf(p.team, tp) - myDepth) * Pitch.L * 0.7f + 5f + Random.Range(0f, 3f) - (laneT < 1.5f ? 4f : 0f);
                    if (st > best.score) { best.mate = m; best.score = st; best.through = true; }
                }
            }
        }
        return best;
    }

    // ================================================================== ball contacts

    void BallContacts()
    {
        if (ball.Frozen || ball.Holder != null) return;
        if (ball.Owner != null)
        {
            AITackles();
            return;
        }

        Vector3 bp = ball.Pos;
        float speed = ball.Speed;
        MatchPlayer best = null;
        float bestD = 999f;
        foreach (var p in all)
        {
            if (p.isGK || p.Stunned) continue;
            if (p == ball.LastKicker && Time.time - ball.KickTime < 0.3f) continue;
            float hd = Flat(p.Pos, bp);
            if (hd < 0.85f && bp.y < 0.95f && hd < bestD) { best = p; bestD = hd; }
        }
        if (best != null && !(best.lastTriedKick == ball.KickId && speed > 8f))
        {
            best.lastTriedKick = ball.KickId;
            float chance;
            if (speed < 9f) chance = 1f;
            else if (best == ball.IntendedReceiver) chance = 0.96f;
            else if (best.team == ball.LastTouchTeam) chance = 0.75f;
            else chance = Mathf.Clamp(0.12f + best.defendSkill * 0.004f - (speed - 9f) * 0.03f, 0.05f, 0.75f) * (ball.IsShot ? 0.5f : 1f);
            if (Random.value < chance)
            {
                GainBall(best);
                return;
            }
            Vector3 v = Quaternion.Euler(0f, Random.Range(-30f, 30f), 0f) * ball.Velocity * Random.Range(0.45f, 0.75f);
            ball.Deflect(v, best.team);
            return;
        }

        // Balls in the air: headers / chest control
        if (bp.y < 0.95f || bp.y > 2.3f) return;
        foreach (var p in all)
        {
            if (p.isGK || p.Stunned || p.lastHeaderKick == ball.KickId) continue;
            if (p == ball.LastKicker && Time.time - ball.KickTime < 0.4f) continue;
            if (Flat(p.Pos, bp) > 0.95f) continue;
            p.lastHeaderKick = ball.KickId;
            if (Pitch.InBox(1 - p.team, p.Pos))
            {
                Header(p);
                return;
            }
            if (Pitch.InBox(p.team, p.Pos) && p.team != ball.LastTouchTeam)
            {
                Vector3 v = AttackDir(p.team) * Random.Range(9f, 14f) + Vector3.up * Random.Range(5f, 8f) + Vector3.forward * Random.Range(-5f, 5f);
                ball.Kick(v, p, null, false);
                possessionTeam = p.team;
                return;
            }
            if (ball.Speed < 17f)
            {
                GainBall(p);
                return;
            }
        }
    }

    void Header(MatchPlayer p)
    {
        Vector3 goal = Pitch.AttackGoal(p.team);
        float side = Random.value < 0.5f ? -1f : 1f;
        float err = (1f - p.shotSkill / 100f) * 2.5f * ErrorScale(p);
        var target = new Vector3(goal.x + 0.4f * Mathf.Sign(goal.x), Random.Range(0.3f, 1.8f), side * Random.Range(0.8f, 3.1f) + Random.Range(-1f, 1f) * err);
        float speed = Random.Range(12f, 17f) * (0.85f + p.shotSkill * 0.002f);
        Vector3 p0 = ball.Pos;
        Vector3 dh = new Vector3(target.x - p0.x, 0f, target.z - p0.z);
        float D = Mathf.Max(dh.magnitude, 0.5f);
        float t = D / speed;
        float vy = (target.y - p0.y) / t + 0.5f * 9.81f * t;
        ball.Kick(dh / D * speed + Vector3.up * vy, p, null, true);
        possessionTeam = p.team;
        hud.Toast("HEADER!");
    }

    void GainBall(MatchPlayer p)
    {
        ball.Possess(p);
        possessionTeam = p.team;
        p.expectBallUntil = 0f;
        p.makingRun = false;
        p.nextDecision = Time.time + aiReact * 0.6f;
        if (p.team == 0)
        {
            SetControlled(p);
            p.aiAssist = false;
            if (Time.time < queuedUntil)
            {
                queuedUntil = 0f;
                Vector3 dir = new Vector3(GameInput.Move.x, 0f, GameInput.Move.y);
                if (queuedAction == GameAction.Pass) UserPass(p, dir, false);
                else if (queuedAction == GameAction.Through) UserPass(p, dir, true);
                else UserShoot(p, dir, 0.55f);
            }
        }
    }

    void AITackles()
    {
        var c = ball.Owner;
        if (Time.time - ball.PossessTime < 0.45f) return;
        foreach (var o in teams[1 - c.team])
        {
            if (o.isGK || o.Stunned || Time.time < o.nextTackleTime) continue;
            if (o.userControlled && !o.aiAssist) continue;
            if (Flat(o.Pos, ball.Pos) > 1.05f) continue;
            o.nextTackleTime = Time.time + Random.Range(0.8f, 1.4f);
            float aggression = o.team == 1 ? aiTackle : 0.6f;
            if (Random.value > 0.55f * aggression + 0.1f) continue;
            ResolveTackle(o, c, 0f);
            return;
        }
    }

    void ResolveTackle(MatchPlayer tackler, MatchPlayer carrier, float bonus)
    {
        float pr = 0.45f + bonus + (tackler.defendSkill - carrier.dribbleSkill) * 0.012f + (tackler.strength - carrier.strength) * 0.004f;
        pr = Mathf.Clamp(pr, 0.12f, 0.88f);
        if (Random.value < pr)
        {
            carrier.stunUntil = Time.time + 0.5f;
            carrier.rig.Stumble();
            if (Random.value < 0.7f) GainBall(tackler);
            else
            {
                Vector3 bp = ball.Pos;
                ball.TeleportFree(new Vector3(bp.x, Ball.R, bp.z));
                Vector3 dir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
                ball.Deflect(dir * Random.Range(3f, 6f), tackler.team);
            }
        }
        else
        {
            tackler.stunUntil = Time.time + 0.8f;
            tackler.rig.Stumble();
        }
    }

    void AutoSwitch()
    {
        if (state != MatchState.Play) return;
        if (controlled == null)
        {
            SetControlled(ClosestTo(0, ball.Pos, null));
            return;
        }
        if (ball.Owner != null && ball.Owner.team == 0) return;
        if (ball.IsFree && ball.IntendedReceiver != null && ball.IntendedReceiver.team == 0) return;
        if (Time.time - lastUserInput < 0.9f) return;
        Vector3 bp = ball.IsFree ? ball.Predict(0.3f) : ball.Pos;
        var closest = ClosestTo(0, bp, null);
        if (closest != null && closest != controlled && Flat(controlled.Pos, bp) > Flat(closest.Pos, bp) + 5f)
            SetControlled(closest);
    }

    // ================================================================== goalkeepers

    void UpdateKeeper(MatchPlayer gk, float dt, bool live)
    {
        if (gk == null) return;
        Vector3 goal = Pitch.GoalCenter(gk.team);
        float inward = gk.team == 0 ? 1f : -1f;
        Vector3 bp = ball.Pos;

        if (!live)
        {
            Vector3 home = goal + Vector3.right * inward * 1f;
            Vector3 toHome = home - gk.Pos;
            toHome.y = 0f;
            gk.gkDiving = false;
            gk.Step(toHome.magnitude > 0.2f ? toHome.normalized * Mathf.Min(gk.jogSpeed, toHome.magnitude * 2f) : Vector3.zero, dt, bp, true);
            return;
        }

        if (ball.Holder == gk)
        {
            gk.Step(Vector3.zero, dt, goal + Vector3.right * inward * 50f, true);
            if (Time.time >= gk.gkHoldUntil) KeeperDistribute(gk);
            return;
        }

        if (gk.locked)
        {
            gk.Step(Vector3.zero, dt, goal + Vector3.right * inward * 50f, true);
            return;
        }

        if (gk.gkDiving)
        {
            Vector3 to = gk.gkDiveTarget - gk.Pos;
            to.y = 0f;
            float m = to.magnitude;
            gk.velocity = m > 0.05f ? to / m * Mathf.Min(7.5f, m / Mathf.Max(dt, 0.001f)) : Vector3.zero;
            gk.transform.position += gk.velocity * dt;
            if (gk.gkSaveDecision && ball.IsFree)
            {
                if (Vector3.Distance(ball.Pos, gk.Pos + Vector3.up) < 2f || Time.time >= gk.gkSaveAt) PerformSave(gk);
            }
            if (Time.time > gk.gkDiveUntil) gk.gkDiving = false;
            return;
        }

        // React to shots
        if (ball.IsFree && ball.KickId != gk.lastShotEval && ball.LastTouchTeam != gk.team)
        {
            Vector3 v = ball.Velocity;
            if (v.x * inward < -6f)
            {
                gk.lastShotEval = ball.KickId;
                EvaluateSave(gk);
                if (gk.gkDiving) return;
            }
        }

        Vector3 target;
        bool sprint = false;
        float ballDist = Flat(bp, goal);
        bool inBox = Pitch.InBox(gk.team, bp);
        var carrier = ball.Owner;

        if (ball.IsFree && inBox && ball.Speed < 10f && bp.y < 2.3f && KeeperFirst(gk))
        {
            target = new Vector3(bp.x, 0f, bp.z);
            sprint = true;
            if (Flat(gk.Pos, bp) < 1.1f)
            {
                CatchBall(gk);
                return;
            }
        }
        else if (carrier != null && carrier.team != gk.team && inBox && ballDist < 15f)
        {
            Vector3 toGoal = goal - carrier.Pos;
            toGoal.y = 0f;
            target = carrier.Pos + toGoal.normalized * 1.3f;
            sprint = true;
            if (Flat(gk.Pos, bp) < 1.4f && Time.time > gk.gkSmotherCooldown)
            {
                gk.gkSmotherCooldown = Time.time + 1.2f;
                float pr = 0.35f + (gk.data.ovr - carrier.dribbleSkill) * 0.01f;
                if (gk.team == 1) pr *= aiKeeper;
                if (Random.value < pr)
                {
                    carrier.rig.Stumble();
                    CatchBall(gk);
                    return;
                }
                gk.rig.PlayDive(Random.value < 0.5f ? -1f : 1f);
                gk.stunUntil = Time.time + 1f;
            }
        }
        else
        {
            Vector3 dirToBall = new Vector3(bp.x - goal.x, 0f, bp.z - goal.z);
            if (dirToBall.sqrMagnitude < 0.01f) dirToBall = Vector3.right * inward;
            float outDist = Mathf.Clamp(ballDist * 0.065f, 0.8f, 4.5f);
            if (ballDist > 45f) outDist = Mathf.Min(outDist + 4f, 9f);
            target = goal + dirToBall.normalized * outDist;
            target.z = Mathf.Clamp(target.z, -3f, 3f);
        }

        Vector3 delta = target - gk.Pos;
        delta.y = 0f;
        float dd = delta.magnitude;
        float sp = sprint ? gk.sprintSpeed : Mathf.Min(gk.sprintSpeed * 0.8f, dd * 3f);
        gk.Step(dd > 0.1f ? delta / dd * sp : Vector3.zero, dt, bp, true);
    }

    bool KeeperFirst(MatchPlayer gk)
    {
        float mine = Flat(gk.Pos, ball.Pos) / gk.sprintSpeed;
        foreach (var o in teams[1 - gk.team])
            if (Flat(o.Pos, ball.Pos) / o.sprintSpeed < mine - 0.2f) return false;
        return true;
    }

    void EvaluateSave(MatchPlayer gk)
    {
        Vector3 p = ball.Pos, v = ball.Velocity;
        float goalX = Pitch.GoalCenter(gk.team).x;
        if (Mathf.Abs(v.x) < 0.1f) return;
        float t = (goalX - p.x) / v.x;
        if (t <= 0f || t > 3f) return;
        var cross = new Vector3(goalX, p.y + v.y * t - 4.905f * t * t, p.z + v.z * t);
        if (Mathf.Abs(cross.z) > Pitch.GoalHalfWidth + 0.5f || cross.y > Pitch.GoalHeight + 0.5f) return; // off target
        if (cross.y < Ball.R) cross.y = 0.3f;

        bool realGk = gk.data.IsGK;
        float div = realGk ? gk.data.pac : 30f;
        float refl = realGk ? gk.data.dri : 30f;
        float lateral = Mathf.Abs(cross.z - gk.Pos.z);
        float high = Mathf.Max(0f, cross.y - 1.3f);
        float need = Mathf.Sqrt(lateral * lateral + high * high * 0.7f);
        float react = 0.26f - refl * 0.0014f;
        float avail = Mathf.Max(0f, t - react);
        float reach = 1.1f + div * 0.016f + avail * 4.5f;
        float chance;
        if (need < 0.9f) chance = 0.93f;
        else if (need <= reach) chance = Mathf.Lerp(0.88f, 0.3f, (need - 0.9f) / Mathf.Max(0.1f, reach - 0.9f));
        else chance = 0.03f;
        chance *= Mathf.Lerp(1.05f, 0.72f, Mathf.InverseLerp(14f, 32f, v.magnitude));
        chance *= 0.7f + gk.data.ovr * 0.0035f;
        if (gk.team == 1) chance *= aiKeeper;

        float inward = gk.team == 0 ? 1f : -1f;
        gk.gkSaveDecision = Random.value < Mathf.Clamp01(chance);
        gk.gkDiveTarget = new Vector3(goalX + inward * 0.5f, 0f, Mathf.Clamp(cross.z, -Pitch.GoalHalfWidth, Pitch.GoalHalfWidth));
        gk.gkSaveAt = Time.time + t * 0.88f;
        gk.gkDiving = true;
        gk.gkDiveUntil = Time.time + Mathf.Max(1.2f, t + 0.6f);
        if (need > 0.9f)
        {
            float side = Vector3.Dot(cross - gk.Pos, gk.transform.right) > 0f ? 1f : -1f;
            gk.rig.PlayDive(side);
        }
    }

    void PerformSave(MatchPlayer gk)
    {
        gk.gkSaveDecision = false;
        float han = gk.data.IsGK ? gk.data.sho : 30f;
        float spd = ball.Speed;
        bool catchIt = spd < 25f && Random.value < (han / 100f) * (spd < 15f ? 1.1f : 0.75f);
        if (catchIt)
        {
            CatchBall(gk);
            return;
        }
        float inward = gk.team == 0 ? 1f : -1f;
        Vector3 bp = ball.Pos;
        float side = Mathf.Sign(bp.z - gk.Pos.z);
        if (side == 0f) side = 1f;
        float goalX = Pitch.GoalCenter(gk.team).x;
        ball.transform.position = new Vector3(goalX + inward * Mathf.Max(0.6f, Mathf.Abs(bp.x - goalX)), Mathf.Max(bp.y, 0.3f), bp.z);
        var v = new Vector3(inward * Random.Range(3f, 8f), Random.Range(2f, 5f), side * Random.Range(5f, 10f));
        ball.Deflect(v, gk.team);
        hud.Toast("SAVE!");
    }

    void CatchBall(MatchPlayer gk)
    {
        bool wasShot = ball.IsShot;
        ball.Hold(gk);
        possessionTeam = gk.team;
        gk.gkHoldUntil = Time.time + Random.Range(1.2f, 2f);
        if (wasShot) hud.Toast("SAVE!");
    }

    void KeeperDistribute(MatchPlayer gk)
    {
        var target = PickRestartTarget(gk, 45f, Random.value < 0.4f);
        if (target == null) target = ClosestTo(gk.team, gk.Pos + AttackDir(gk.team) * 20f, null);
        if (target == null) return;
        Vector3 aim = target.Pos + target.velocity * 0.6f;
        float d = Flat(ball.Pos, aim);
        bool throwIt = d < 30f;
        if (throwIt) gk.rig.PlayThrow();
        target.expectBallUntil = Time.time + 3f;
        KickToPoint(gk, aim, d > 14f, 4f, Ball.R, target, !throwIt);
        if (target.team == 0) SetControlled(target);
    }
}
