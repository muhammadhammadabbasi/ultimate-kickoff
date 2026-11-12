using UnityEngine;

/// <summary>One footballer on the pitch: stats, movement and body.</summary>
public class MatchPlayer : MonoBehaviour
{
    public PlayerData data;
    public int team;
    public int slot;
    public FormationSlot slotInfo;
    public bool isGK;
    public PosGroup group;
    public HumanoidRig rig;

    public Vector3 velocity;
    public Vector3 Pos => transform.position;
    public float PlanarSpeed { get { return new Vector3(velocity.x, 0f, velocity.z).magnitude; } }

    // Derived stats
    public float jogSpeed, sprintSpeed, accel, turnRate;
    public float passSkill, shotSkill, dribbleSkill, defendSkill, strength;

    // Control / AI state
    public bool userControlled;
    public bool aiAssist;              // user-controlled player currently driven by AI (thumb off the stick)
    public Vector3 userDir;
    public bool userSprint;
    public Vector3 aiTarget;
    public bool aiSprint;
    public bool locked;                // set-piece taker standing still
    public float stunUntil;
    public float nextTackleTime;
    public float nextDecision;
    public float expectBallUntil;
    public bool makingRun;
    public float runUntil;
    public int lastTriedKick = -1;
    public int lastHeaderKick = -1;

    // Goalkeeper state
    public int lastShotEval = -1;
    public bool gkDiving;
    public float gkDiveUntil;
    public bool gkSaveDecision;
    public float gkSaveAt;
    public Vector3 gkDiveTarget;
    public float gkHoldUntil;
    public float gkSmotherCooldown;

    public bool Stunned => Time.time < stunUntil;

    public void Init(PlayerData d, int team, int slot, FormationSlot fs, Material shirt, Material shorts, Material socks, Material boots, Material gloves)
    {
        data = d;
        this.team = team;
        this.slot = slot;
        slotInfo = fs;
        isGK = fs.pos == Pos.GK;
        group = PlayerData.Group(fs.pos);
        gameObject.name = (team == 0 ? "H_" : "A_") + d.name;

        if (isGK && d.IsGK)
        {
            // GK stats: pac=DIV, sho=HAN, pas=KIC, dri=REF, def=SPD, phy=POS
            jogSpeed = 4.4f;
            sprintSpeed = 6.0f + d.def * 0.012f;
            accel = 14f;
            passSkill = d.pas;
            shotSkill = 45f;
            dribbleSkill = 45f;
            defendSkill = d.phy;
            strength = 70f;
        }
        else
        {
            int pac = d.IsGK ? 40 : d.pac;
            jogSpeed = 4.7f + pac * 0.012f;
            sprintSpeed = 6.3f + pac * 0.031f;
            accel = 9f + pac * 0.05f;
            passSkill = d.IsGK ? 40 : d.pas;
            shotSkill = d.IsGK ? 25 : d.sho;
            dribbleSkill = d.IsGK ? 35 : d.dri;
            defendSkill = d.IsGK ? 35 : d.def;
            strength = d.IsGK ? 60 : d.phy;
        }
        turnRate = 720f;

        int h = d.Hash;
        var skin = MatLib.Lit(HumanoidRig.SkinTones[h % HumanoidRig.SkinTones.Length], 0.25f);
        var hair = MatLib.Lit(HumanoidRig.HairColors[(h / 7) % HumanoidRig.HairColors.Length], 0.2f);
        float scale = 0.95f + ((h / 13) % 10) * 0.011f;

        var rigGo = new GameObject("Rig");
        rigGo.transform.SetParent(transform, false);
        rig = rigGo.AddComponent<HumanoidRig>();
        rig.Build(shirt, shorts, socks, skin, hair, boots, isGK ? gloves : skin, scale);
        rig.keeperStance = isGK;
    }

    /// <summary>Accelerate towards a desired planar velocity and move.</summary>
    public void Step(Vector3 desiredVel, float dt, Vector3 lookAt, bool faceLook = false)
    {
        desiredVel.y = 0f;
        if (locked || Stunned) desiredVel = Vector3.zero;
        float a = desiredVel.sqrMagnitude > velocity.sqrMagnitude ? accel : accel * 1.7f;
        if (Stunned) a = accel * 0.6f;
        velocity = Vector3.MoveTowards(velocity, desiredVel, a * dt);
        Vector3 p = transform.position + velocity * dt;
        p.y = 0f;
        p.x = Mathf.Clamp(p.x, -Pitch.HalfL - 4f, Pitch.HalfL + 4f);
        p.z = Mathf.Clamp(p.z, -Pitch.HalfW - 3f, Pitch.HalfW + 3f);
        transform.position = p;

        Vector3 face;
        if (PlanarSpeed > 1.2f && !faceLook) face = velocity;
        else face = lookAt - transform.position;
        face.y = 0f;
        if (face.sqrMagnitude > 0.0001f)
        {
            Quaternion want = Quaternion.LookRotation(face);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnRate * dt);
        }
    }

    public void Teleport(Vector3 pos, Vector3 faceDir)
    {
        pos.y = 0f;
        transform.position = pos;
        faceDir.y = 0f;
        if (faceDir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(faceDir);
        velocity = Vector3.zero;
        stunUntil = 0f;
        makingRun = false;
        expectBallUntil = 0f;
        gkDiving = false;
        if (rig != null) rig.ResetPose();
    }

    public void Animate(float dt, bool withBall)
    {
        if (rig != null) rig.Animate(PlanarSpeed, dt, withBall);
    }
}
