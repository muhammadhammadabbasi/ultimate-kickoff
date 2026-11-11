using UnityEngine;

/// <summary>Small helper so the same code compiles on Unity 2022 and Unity 6 (renamed Rigidbody APIs).</summary>
public static class RbCompat
{
    public static Vector3 GetVelocity(Rigidbody rb)
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    public static void SetVelocity(Rigidbody rb, Vector3 v)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
    }

    public static void SetDamping(Rigidbody rb, float linear, float angular)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = linear;
        rb.angularDamping = angular;
#else
        rb.drag = linear;
        rb.angularDrag = angular;
#endif
    }
}

/// <summary>
/// The match ball. Free ball = normal physics. Owned ball = follows the owner's feet (dribbling).
/// Held by keeper = stays in the keeper's hands.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class BallController : MonoBehaviour
{
    public static BallController Instance { get; private set; }

    [Header("Physics")]
    public float radius = 0.11f;
    public float rollingDamping = 0.35f;

    [Header("Dribbling")]
    public float dribbleDistance = 0.6f;
    public float touchAmount = 0.12f;

    public FootballPlayer Owner { get; private set; }
    public Goalkeeper KeeperOwner { get; private set; }
    public FootballPlayer LastKicker { get; private set; }
    public int KickId { get; private set; }
    public float PossessionStartTime { get; private set; }

    public bool IsFree => Owner == null && KeeperOwner == null;
    public bool CanBeTackled => Owner != null && Time.time - PossessionStartTime > 0.6f;

    public Vector3 Velocity
    {
        get
        {
            if (Owner != null) return Owner.Velocity;
            if (rb == null || rb.isKinematic) return Vector3.zero;
            return RbCompat.GetVelocity(rb);
        }
    }

    public float Speed => Velocity.magnitude;

    Rigidbody rb;
    SphereCollider col;
    float kickerBlockedUntil;
    float dribblePhase;

    void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody>();
        col = GetComponent<SphereCollider>();
        rb.mass = 0.43f;
        rb.maxAngularVelocity = 80f;
        RbCompat.SetDamping(rb, rollingDamping, 0.6f);
        SetHeld(false);
    }

    public bool CanBePickedBy(FootballPlayer p)
    {
        if (!IsFree) return false;
        if (p == LastKicker && Time.time < kickerBlockedUntil) return false;
        return true;
    }

    public void TakePossession(FootballPlayer p)
    {
        SetHeld(true);
        Owner = p;
        KeeperOwner = null;
        PossessionStartTime = Time.time;
        dribblePhase = 0f;
        FootballPlayer.OnPossessionChanged(p);
    }

    public void HoldByKeeper(Goalkeeper k)
    {
        SetHeld(true);
        Owner = null;
        KeeperOwner = k;
    }

    public void Kick(Vector3 velocity, FootballPlayer by)
    {
        Owner = null;
        KeeperOwner = null;
        SetHeld(false);
        rb.position = transform.position;
        RbCompat.SetVelocity(rb, velocity);
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
        rb.angularVelocity = Vector3.Cross(Vector3.up, flat) / radius;
        LastKicker = by;
        kickerBlockedUntil = Time.time + 0.45f;
        KickId++;
    }

    public void ResetTo(Vector3 pos)
    {
        Owner = null;
        KeeperOwner = null;
        SetHeld(false);
        transform.position = pos;
        rb.position = pos;
        RbCompat.SetVelocity(rb, Vector3.zero);
        rb.angularVelocity = Vector3.zero;
        LastKicker = null;
        KickId++;
    }

    void SetHeld(bool held)
    {
        if (held)
        {
            if (!rb.isKinematic)
            {
                RbCompat.SetVelocity(rb, Vector3.zero);
                rb.angularVelocity = Vector3.zero;
            }
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.None;
            col.enabled = false;
        }
        else
        {
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            col.enabled = true;
        }
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (Owner != null)
        {
            float spd = Owner.PlanarSpeed;
            dribblePhase += spd * dt * 2f;
            float touch = touchAmount * Mathf.Sin(dribblePhase) * Mathf.Clamp01(spd / 3f);
            Vector3 target = Owner.transform.position + Owner.transform.forward * (dribbleDistance + touch) + Vector3.up * radius;
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-20f * dt));
            if (spd > 0.05f)
                transform.Rotate(Owner.transform.right, spd * dt / radius * Mathf.Rad2Deg, Space.World);
        }
        else if (KeeperOwner != null)
        {
            transform.position = Vector3.Lerp(transform.position, KeeperOwner.HandPoint, 1f - Mathf.Exp(-20f * dt));
        }
    }
}
