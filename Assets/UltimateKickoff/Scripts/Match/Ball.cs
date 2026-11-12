using UnityEngine;

/// <summary>Match ball: rigidbody physics when free, follows the dribbler's feet when owned.</summary>
public class Ball : MonoBehaviour
{
    public const float R = 0.11f;
    public const float GroundDamping = 0.42f;
    public const float AirDamping = 0.05f;

    public MatchPlayer Owner { get; private set; }
    public MatchPlayer Holder { get; private set; }          // goalkeeper holding in hands
    public MatchPlayer LastKicker { get; private set; }
    public MatchPlayer IntendedReceiver { get; set; }
    public int LastTouchTeam { get; private set; } = -1;
    public int KickId { get; private set; }
    public float KickTime { get; private set; }
    public float PossessTime { get; private set; }
    public bool IsShot { get; private set; }
    public bool Frozen { get; private set; }

    public bool IsFree => Owner == null && Holder == null && !Frozen;
    public Vector3 Pos => transform.position;

    public Vector3 Velocity
    {
        get
        {
            if (Owner != null) return Owner.velocity;
            if (rb == null || rb.isKinematic) return Vector3.zero;
            return rb.linearVelocity;
        }
    }

    public float Speed => Velocity.magnitude;

    Rigidbody rb;
    SphereCollider col;
    float dribblePhase;

    public void Init(Material mat)
    {
        gameObject.name = "Ball";
        var mf = gameObject.AddComponent<MeshFilter>();
        mf.sharedMesh = MeshUtil.Sphere;
        var mr = gameObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        transform.localScale = Vector3.one * (R * 2f);

        col = gameObject.AddComponent<SphereCollider>();
        col.radius = 0.5f;
        var pm = new PhysicsMaterial("Ball")
        {
            bounciness = 0.55f,
            dynamicFriction = 0.5f,
            staticFriction = 0.5f,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Average
        };
        col.sharedMaterial = pm;

        rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = 0.43f;
        rb.maxAngularVelocity = 100f;
        rb.linearDamping = GroundDamping;
        rb.angularDamping = 0.8f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void FixedUpdate()
    {
        if (rb.isKinematic) return;
        bool airborne = transform.position.y > R + 0.08f;
        rb.linearDamping = airborne ? AirDamping : GroundDamping;
    }

    void SetKinematic(bool k)
    {
        if (k)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
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

    public void Possess(MatchPlayer p)
    {
        SetKinematic(true);
        Owner = p;
        Holder = null;
        Frozen = false;
        IsShot = false;
        IntendedReceiver = null;
        LastTouchTeam = p.team;
        PossessTime = Time.time;
        dribblePhase = 0f;
    }

    public void Hold(MatchPlayer keeper)
    {
        SetKinematic(true);
        Owner = null;
        Holder = keeper;
        Frozen = false;
        IsShot = false;
        IntendedReceiver = null;
        LastTouchTeam = keeper.team;
        PossessTime = Time.time;
    }

    /// <summary>Freeze the ball on a spot for a set piece.</summary>
    public void Place(Vector3 spot)
    {
        SetKinematic(true);
        Owner = null;
        Holder = null;
        Frozen = true;
        IsShot = false;
        IntendedReceiver = null;
        transform.position = new Vector3(spot.x, R, spot.z);
        transform.rotation = Quaternion.identity;
    }

    public void Kick(Vector3 velocity, MatchPlayer by, MatchPlayer receiver, bool shot)
    {
        Vector3 start = transform.position;
        Owner = null;
        Holder = null;
        Frozen = false;
        SetKinematic(false);
        rb.position = start;
        rb.linearVelocity = velocity;
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
        rb.angularVelocity = Vector3.Cross(Vector3.up, flat) / R;
        LastKicker = by;
        if (by != null) LastTouchTeam = by.team;
        IntendedReceiver = receiver;
        IsShot = shot;
        KickId++;
        KickTime = Time.time;
    }

    /// <summary>Change the direction/speed of a free ball (deflections, parries).</summary>
    public void Deflect(Vector3 velocity, int touchTeam)
    {
        if (rb.isKinematic) SetKinematic(false);
        rb.linearVelocity = velocity;
        LastTouchTeam = touchTeam;
        IntendedReceiver = null;
        IsShot = false;
        KickId++;
        KickTime = Time.time;
    }

    public void TeleportFree(Vector3 pos)
    {
        Owner = null;
        Holder = null;
        Frozen = false;
        SetKinematic(false);
        transform.position = pos;
        rb.position = pos;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        IntendedReceiver = null;
        IsShot = false;
        KickId++;
    }

    /// <summary>Where a free ball will be in t seconds (ignores bounces).</summary>
    public Vector3 Predict(float t)
    {
        Vector3 p = transform.position;
        if (!IsFree) return p;
        Vector3 v = Velocity;
        bool air = p.y > R + 0.08f;
        float k = air ? AirDamping : GroundDamping;
        float f = (1f - Mathf.Exp(-k * t)) / k;
        Vector3 res = p + new Vector3(v.x, 0f, v.z) * f;
        if (air)
        {
            float y = p.y + v.y * t - 0.5f * 9.81f * t * t;
            res.y = Mathf.Max(R, y);
        }
        else res.y = R;
        return res;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (Owner != null)
        {
            float spd = Owner.PlanarSpeed;
            dribblePhase += spd * dt * 1.8f;
            float reach = 0.48f + Mathf.Clamp01(spd / 9f) * 0.45f;
            float touch = 0.18f * Mathf.Sin(dribblePhase) * Mathf.Clamp01(spd / 4f);
            Vector3 target = Owner.transform.position + Owner.transform.forward * (reach + touch) + Vector3.up * R;
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-18f * dt));
            if (spd > 0.05f) transform.Rotate(Owner.transform.right, spd * dt / R * Mathf.Rad2Deg, Space.World);
        }
        else if (Holder != null)
        {
            Vector3 hand = Holder.transform.position + Holder.transform.forward * 0.35f + Vector3.up * 1.05f;
            transform.position = Vector3.Lerp(transform.position, hand, 1f - Mathf.Exp(-20f * dt));
        }
    }
}
