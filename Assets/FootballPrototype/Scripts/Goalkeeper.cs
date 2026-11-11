using UnityEngine;

/// <summary>
/// Simple goalkeeper: tracks the ball along the goal line, rushes loose balls in the box,
/// and tries to save shots. Harder (faster) shots are harder to catch.
/// </summary>
public class Goalkeeper : MonoBehaviour
{
    public int team;
    public float moveSpeed = 5f;
    public float reach = 1.5f;
    public float holdTime = 1.2f;

    public Vector3 HomePosition { get; set; }
    public Vector3 HandPoint => transform.position + transform.forward * 0.5f + Vector3.up * 1.1f;
    public bool Holding => BallController.Instance != null && BallController.Instance.KeeperOwner == this;

    int lastEvaluatedKick = -1;
    float holdStart;
    float catchBlockedUntil;

    void Awake() { HomePosition = transform.position; }

    public void ResetToHome()
    {
        transform.position = HomePosition;
        lastEvaluatedKick = -1;
        catchBlockedUntil = 0f;
    }

    void Update()
    {
        var ball = BallController.Instance;
        var match = MatchManager.Instance;
        if (ball == null || match == null || !match.Playing) return;

        if (Holding)
        {
            if (Time.time - holdStart >= holdTime) Distribute(ball);
            return;
        }

        float goalZ = match.DefendGoal(team).z;
        float inward = Mathf.Sign(-goalZ);
        Vector3 bp = ball.transform.position;

        float gx = match.goalHalfWidth - 0.4f;
        Vector3 target = new Vector3(Mathf.Clamp(bp.x * 0.5f, -gx, gx), 0f, HomePosition.z);

        bool looseInBox = ball.IsFree && ball.Speed < 7f && Mathf.Abs(bp.z - goalZ) < 8f && Mathf.Abs(bp.x) < 8f;
        if (looseInBox)
            target = new Vector3(bp.x, 0f, bp.z);
        else if (ball.Owner != null && ball.Owner.team != team && Mathf.Abs(bp.z - goalZ) < 12f)
            target.z = goalZ + inward * 2f; // come off the line to narrow the angle

        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        TryCatch(ball, goalZ);
    }

    void TryCatch(BallController ball, float goalZ)
    {
        if (!ball.IsFree || Time.time < catchBlockedUntil) return;

        Vector3 bp = ball.transform.position;
        Vector3 d = bp - HandPoint;
        float horiz = new Vector2(d.x, d.z).magnitude;
        if (horiz > reach || bp.y > 2.6f) return;

        Vector3 v = ball.Velocity;
        float speed = v.magnitude;
        if (speed < 5f) { Catch(ball); return; }

        bool towardGoal = (goalZ - bp.z) * v.z > 0f;
        if (!towardGoal || lastEvaluatedKick == ball.KickId) return;
        lastEvaluatedKick = ball.KickId;

        float chance = Mathf.Lerp(0.95f, 0.25f, Mathf.InverseLerp(12f, 30f, speed));
        float roll = Random.value;
        if (roll < chance)
        {
            Catch(ball);
        }
        else if (roll < chance + (1f - chance) * 0.5f)
        {
            // Parry the ball wide
            float side = Mathf.Sign(bp.x - transform.position.x);
            if (side == 0f) side = 1f;
            Vector3 inwardDir = new Vector3(0f, 0f, Mathf.Sign(-goalZ));
            ball.Kick(new Vector3(side * 7f, 2.5f, 0f) + inwardDir * 4f, null);
            catchBlockedUntil = Time.time + 0.8f;
        }
    }

    void Catch(BallController ball)
    {
        ball.HoldByKeeper(this);
        holdStart = Time.time;
    }

    void Distribute(BallController ball)
    {
        FootballPlayer best = null;
        float bestScore = float.MaxValue;
        foreach (var p in FootballPlayer.All)
        {
            if (p.team != team) continue;
            float s = (p.transform.position - transform.position).sqrMagnitude * Random.Range(0.7f, 1.3f);
            if (s < bestScore) { bestScore = s; best = p; }
        }

        Vector3 from = ball.transform.position;
        if (best != null)
        {
            Vector3 dir = best.transform.position - from;
            dir.y = 0f;
            float dist = Mathf.Max(dir.magnitude, 0.01f);
            ball.Kick(dir / dist * Mathf.Clamp(dist * 0.8f + 4f, 8f, 20f) + Vector3.up * 4f, null);
            best.ExpectPass();
        }
        else
        {
            ball.Kick(transform.forward * 12f + Vector3.up * 4f, null);
        }
        catchBlockedUntil = Time.time + 1.2f;
    }
}
