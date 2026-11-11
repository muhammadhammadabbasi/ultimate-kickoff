using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One outfield player. If userControlled = true it reads FootballInput, otherwise it runs simple AI.
/// Team 0 attacks +Z (the user's team), team 1 attacks -Z.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FootballPlayer : MonoBehaviour
{
    public static readonly List<FootballPlayer> All = new List<FootballPlayer>();
    static int lastSwitchFrame = -1;

    [Header("Team")]
    public int team;
    public bool userControlled;
    public GameObject indicator;

    [Header("Movement")]
    public float runSpeed = 6f;
    public float sprintSpeed = 8.5f;
    public float turnSpeed = 900f;
    public float withBallMultiplier = 0.85f;

    [Header("Ball")]
    public float controlRadius = 0.85f;
    [Range(0f, 1f)] public float aiTackleChance = 0.4f;

    [Header("Kicking")]
    public float minShotPower = 14f;
    public float maxShotPower = 30f;
    public float chargeTime = 0.9f;

    public Vector3 HomePosition { get; set; }
    public Vector3 Velocity => cc != null ? cc.velocity : Vector3.zero;
    public float PlanarSpeed { get { Vector3 v = Velocity; v.y = 0f; return v.magnitude; } }
    public bool HasBall => Ball != null && Ball.Owner == this;
    public float ShotCharge => charging ? shotCharge : 0f;

    static BallController Ball => BallController.Instance;
    static MatchManager Match => MatchManager.Instance;

    CharacterController cc;
    float verticalSpeed;
    bool charging;
    float shotCharge;
    float expectPassUntil;
    float nextTackleTime;
    float aiDecisionTimer;
    float aiSeed;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        HomePosition = transform.position;
        aiSeed = Random.value * 10f;
    }

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Update()
    {
        bool playing = Match == null || Match.Playing;
        Vector3 move = Vector3.zero;
        bool sprint = false;

        if (playing)
        {
            if (userControlled)
            {
                Vector2 m = FootballInput.Move;
                move = new Vector3(m.x, 0f, m.y);
                sprint = FootballInput.Held(FootballAction.Sprint);
                if (Time.frameCount != lastSwitchFrame) HandleUserActions(move);
            }
            else
            {
                move = ComputeAIMove(out sprint);
            }
        }
        else
        {
            charging = false;
            if (userControlled)
            {
                // drain button presses made during goal celebrations
                FootballInput.Pressed(FootballAction.Pass);
                FootballInput.Pressed(FootballAction.Shoot);
                FootballInput.Released(FootballAction.Shoot);
            }
        }

        float speed = sprint ? sprintSpeed : runSpeed;
        if (HasBall) speed *= withBallMultiplier;

        if (move.sqrMagnitude > 0.0025f)
        {
            Quaternion look = Quaternion.LookRotation(new Vector3(move.x, 0f, move.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        verticalSpeed = cc.isGrounded ? -2f : verticalSpeed + Physics.gravity.y * Time.deltaTime;
        cc.Move((move * speed + Vector3.up * verticalSpeed) * Time.deltaTime);

        if (playing)
        {
            TryPickup();
            TryTackle();
        }

        if (indicator != null && indicator.activeSelf != userControlled)
            indicator.SetActive(userControlled);
    }

    // ---------------- User ----------------

    void HandleUserActions(Vector3 move)
    {
        bool passPressed = FootballInput.Pressed(FootballAction.Pass);
        bool shootPressed = FootballInput.Pressed(FootballAction.Shoot);
        bool shootReleased = FootballInput.Released(FootballAction.Shoot);
        Vector3 aim = move.sqrMagnitude > 0.09f ? move.normalized : transform.forward;

        if (passPressed)
        {
            if (HasBall) Pass(aim);
            else SwitchToTeammateNearestBall();
        }

        if (shootPressed) { charging = true; shotCharge = 0f; }
        if (charging) shotCharge = Mathf.Min(1f, shotCharge + Time.deltaTime / chargeTime);
        if (shootReleased && charging)
        {
            charging = false;
            if (HasBall) Shoot(aim, shotCharge, true);
        }
    }

    void SwitchToTeammateNearestBall()
    {
        if (Ball == null) return;
        FootballPlayer best = null;
        float bestDist = float.MaxValue;
        foreach (var p in All)
        {
            if (p == this || p.team != team) continue;
            float d = (p.transform.position - Ball.transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = p; }
        }
        if (best != null) SetUserControlled(best);
    }

    // ---------------- Kicking ----------------

    public void Pass(Vector3 dir)
    {
        if (!PassToTeammate(dir))
            Ball.Kick(dir.normalized * 12f + Vector3.up * 0.3f, this);
    }

    bool PassToTeammate(Vector3 dir)
    {
        FootballPlayer target = FindPassTarget(dir);
        if (target == null) return false;
        Vector3 from = Ball.transform.position;
        Vector3 lead = target.transform.position + target.Velocity * 0.35f;
        Vector3 d = lead - from;
        d.y = 0f;
        float dist = d.magnitude;
        float power = Mathf.Clamp(dist * 0.9f + 6f, 9f, 24f);
        Ball.Kick(d.normalized * power + Vector3.up * (dist > 18f ? 3f : 0.3f), this);
        target.ExpectPass();
        return true;
    }

    FootballPlayer FindPassTarget(Vector3 dir)
    {
        FootballPlayer best = null;
        float bestScore = float.MaxValue;
        foreach (var p in All)
        {
            if (p == this || p.team != team) continue;
            Vector3 v = p.transform.position - transform.position;
            v.y = 0f;
            float dist = v.magnitude;
            if (dist < 2.5f || dist > 35f) continue;
            float ang = Vector3.Angle(dir, v);
            if (ang > 55f) continue;
            float score = ang * 1.5f + dist;
            if (score < bestScore) { bestScore = score; best = p; }
        }
        return best;
    }

    public void Shoot(Vector3 dir, float charge, bool assist)
    {
        Vector3 from = Ball.transform.position;
        dir.y = 0f;
        dir.Normalize();
        if (assist && Match != null)
        {
            Vector3 toGoal = Match.AttackGoal(team) - from;
            toGoal.y = 0f;
            if (Vector3.Angle(dir, toGoal) < 65f)
                dir = Vector3.Slerp(dir, toGoal.normalized, 0.55f);
        }
        float power = Mathf.Lerp(minShotPower, maxShotPower, charge);
        float lift = Mathf.Lerp(1.5f, 6f, charge);
        Ball.Kick(dir * power + Vector3.up * lift, this);
    }

    public void ExpectPass() { expectPassUntil = Time.time + 2.5f; }

    // ---------------- Ball contact ----------------

    void TryPickup()
    {
        if (Ball == null || !Ball.CanBePickedBy(this)) return;
        Vector3 d = Ball.transform.position - transform.position;
        if (d.y > 0.9f) return;
        d.y = 0f;
        if (d.sqrMagnitude < controlRadius * controlRadius)
            Ball.TakePossession(this);
    }

    void TryTackle()
    {
        if (Ball == null || HasBall || Ball.Owner == null || Ball.Owner.team == team) return;
        if (!Ball.CanBeTackled || Time.time < nextTackleTime) return;
        Vector3 d = Ball.transform.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.9f * 0.9f) return;
        nextTackleTime = Time.time + 0.8f;
        float chance = userControlled ? 0.7f : aiTackleChance;
        if (Random.value < chance) Ball.TakePossession(this);
    }

    // ---------------- AI ----------------

    Vector3 ComputeAIMove(out bool sprint)
    {
        sprint = false;
        if (Ball == null || Match == null) return Vector3.zero;
        aiDecisionTimer -= Time.deltaTime;

        if (HasBall) return AIWithBall(out sprint);

        Vector3 ballPos = Ball.transform.position;

        if (Ball.IsFree && Time.time < expectPassUntil)
        {
            sprint = true;
            return Arrive(ballPos + Ball.Velocity * 0.3f, 0.1f);
        }

        bool ourBall = Ball.Owner != null && Ball.Owner.team == team;
        bool keeperHasIt = Ball.KeeperOwner != null;

        // Opponent team: the closest player presses the ball. (User's team: the human does that.)
        if (!ourBall && !keeperHasIt && team != 0 && IsClosestOfTeamTo(ballPos))
        {
            sprint = true;
            return Arrive(ballPos, 0.1f);
        }

        // Formation: keep shape and shift with the ball
        float attackSign = team == 0 ? 1f : -1f;
        Vector3 t = HomePosition + new Vector3(ballPos.x * 0.35f, 0f, ballPos.z * 0.5f);
        if (ourBall) t.z += 7f * attackSign;
        t.x = Mathf.Clamp(t.x, -Match.halfWidth + 2f, Match.halfWidth - 2f);
        t.z = Mathf.Clamp(t.z, -Match.halfLength + 3f, Match.halfLength - 3f);
        return Arrive(t, 0.5f);
    }

    Vector3 AIWithBall(out bool sprint)
    {
        sprint = false;
        Vector3 goal = Match.AttackGoal(team);
        Vector3 toGoal = goal - transform.position;
        toGoal.y = 0f;
        float dist = toGoal.magnitude;
        Vector3 dir = toGoal / Mathf.Max(dist, 0.01f);

        if (aiDecisionTimer <= 0f)
        {
            aiDecisionTimer = 0.35f;
            if (dist < 15f)
            {
                float gx = Match.goalHalfWidth - 0.5f;
                Vector3 aimPoint = goal + new Vector3(Random.Range(-gx, gx), 0f, 0f);
                Shoot(aimPoint - Ball.transform.position, Random.Range(0.35f, 0.8f), false);
                return Vector3.zero;
            }
            if (IsPressured(transform.forward) && Random.value < 0.5f)
            {
                if (PassToTeammate(dir) || PassToTeammate(transform.forward)) return Vector3.zero;
            }
        }

        Vector3 side = Vector3.Cross(Vector3.up, dir);
        float weave = Mathf.Sin(Time.time * 1.3f + aiSeed) * 0.35f;
        sprint = dist > 20f;
        return (dir + side * weave).normalized;
    }

    bool IsPressured(Vector3 forward)
    {
        foreach (var p in All)
        {
            if (p.team == team) continue;
            Vector3 v = p.transform.position - transform.position;
            v.y = 0f;
            if (v.sqrMagnitude < 2.5f * 2.5f && Vector3.Dot(v.normalized, forward) > 0.2f) return true;
        }
        return false;
    }

    bool IsClosestOfTeamTo(Vector3 point)
    {
        float mine = (transform.position - point).sqrMagnitude;
        foreach (var p in All)
        {
            if (p == this || p.team != team) continue;
            if ((p.transform.position - point).sqrMagnitude < mine) return false;
        }
        return true;
    }

    Vector3 Arrive(Vector3 target, float stopDist)
    {
        Vector3 d = target - transform.position;
        d.y = 0f;
        float dist = d.magnitude;
        if (dist < stopDist) return Vector3.zero;
        return d / dist * Mathf.Clamp01(dist / 2f);
    }

    // ---------------- Team helpers ----------------

    public void Teleport(Vector3 pos, Quaternion rot)
    {
        cc.enabled = false;
        transform.SetPositionAndRotation(pos, rot);
        cc.enabled = true;
        verticalSpeed = 0f;
        charging = false;
        expectPassUntil = 0f;
    }

    public static void OnPossessionChanged(FootballPlayer p)
    {
        p.aiDecisionTimer = 0.4f;
        p.expectPassUntil = 0f;
        if (p.team == 0) SetUserControlled(p);
    }

    public static void SetUserControlled(FootballPlayer target)
    {
        foreach (var p in All)
        {
            if (p.team != target.team) continue;
            p.userControlled = p == target;
            if (p != target) p.charging = false;
        }
        lastSwitchFrame = Time.frameCount;
    }
}
