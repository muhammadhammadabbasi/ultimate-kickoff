using UnityEngine;

/// <summary>
/// Procedural footballer body (low-poly capsules) with code-driven animation:
/// run cycle, kick, throw-in, goalkeeper stance/dive, celebration.
/// Local +Z is the facing direction.
/// </summary>
public class HumanoidRig : MonoBehaviour
{
    public static readonly Color[] SkinTones =
    {
        new Color(0.96f, 0.8f, 0.67f), new Color(0.88f, 0.69f, 0.53f), new Color(0.74f, 0.53f, 0.38f),
        new Color(0.55f, 0.38f, 0.26f), new Color(0.38f, 0.26f, 0.18f)
    };
    public static readonly Color[] HairColors =
    {
        new Color(0.08f, 0.06f, 0.05f), new Color(0.25f, 0.16f, 0.08f), new Color(0.55f, 0.4f, 0.22f), new Color(0.85f, 0.75f, 0.5f)
    };
    /// <summary>Hair colour for a player hash (darker skin tones get dark hair).</summary>
    public static Color HairFor(int h)
    {
        int skin = h % SkinTones.Length;
        int idx = (h / 7) % HairColors.Length;
        if (skin >= 2) idx %= 2;
        return HairColors[idx];
    }

    public static readonly Color[] BootColors =
    {
        new Color(0.08f, 0.08f, 0.1f), Color.white, new Color(0.95f, 0.4f, 0.1f), new Color(0.1f, 0.7f, 0.95f), new Color(0.95f, 0.85f, 0.2f)
    };

    Transform body, torso, shoulderL, shoulderR, elbowL, elbowR, hipL, hipR, kneeL, kneeR;

    float phase;
    float kickT = -1f;
    float throwT = -1f;
    float diveT = -1f;
    float diveSide;
    float celebrateT = -1f;
    float stumbleT = -1f;

    public bool keeperStance;
    public bool IsDiving => diveT >= 0f;

    public void Build(Material shirt, Material shorts, Material socks, Material skin, Material hair, Material boots, Material hands,
                      float scale, Material torsoMat, Material headMat, int hairStyle, bool keeper)
    {
        var sph = MeshUtil.SphereHi;
        Material armMat = keeper ? shirt : skin;

        body = new GameObject("Body").transform;
        body.SetParent(transform, false);
        body.localPosition = new Vector3(0f, 0.95f, 0f);
        body.localScale = Vector3.one * scale;

        torso = new GameObject("Torso").transform;
        torso.SetParent(body, false);
        MeshUtil.Part("Shirt", torso, BodyMesh.Torso, torsoMat, Vector3.zero, Vector3.one);
        MeshUtil.Part("Neck", torso, BodyMesh.Neck, skin, new Vector3(0f, 0.60f, 0f), Vector3.one, false);
        MeshUtil.Part("Head", torso, BodyMesh.Head, headMat, new Vector3(0f, 0.80f, 0.012f), Vector3.one);
        MeshUtil.Part("Hair", torso, BodyMesh.HairCap(hairStyle), hair, new Vector3(0f, 0.80f, 0.012f), Vector3.one, false);

        shoulderL = Joint("ShoulderL", torso, new Vector3(-0.245f, 0.54f, 0f));
        shoulderR = Joint("ShoulderR", torso, new Vector3(0.245f, 0.54f, 0f));
        elbowL = Arm(shoulderL, shirt, armMat, hands, keeper);
        elbowR = Arm(shoulderR, shirt, armMat, hands, keeper);

        MeshUtil.Part("Shorts", body, BodyMesh.Shorts, shorts, Vector3.zero, Vector3.one);

        hipL = Joint("HipL", body, new Vector3(-0.10f, -0.02f, 0f));
        hipR = Joint("HipR", body, new Vector3(0.10f, -0.02f, 0f));
        kneeL = Leg(hipL, shorts, skin, socks, boots);
        kneeR = Leg(hipR, shorts, skin, socks, boots);
    }

    static Transform Joint(string name, Transform parent, Vector3 pos)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        return t;
    }

    static Transform Arm(Transform shoulder, Material sleeve, Material armSkin, Material hands, bool keeper)
    {
        MeshUtil.Part("Shoulder", shoulder, MeshUtil.SphereHi, sleeve, Vector3.zero, new Vector3(0.13f, 0.13f, 0.13f), false);
        MeshUtil.Part("UpperArm", shoulder, BodyMesh.UpperArm, armSkin, Vector3.zero, Vector3.one, false);
        MeshUtil.Part("Sleeve", shoulder, BodyMesh.Sleeve, sleeve, Vector3.zero, Vector3.one, false);
        var elbow = Joint("Elbow", shoulder, new Vector3(0f, -0.28f, 0f));
        MeshUtil.Part("Forearm", elbow, BodyMesh.ForeArm, armSkin, Vector3.zero, Vector3.one, false);
        if (keeper) MeshUtil.Part("Glove", elbow, MeshUtil.SphereHi, hands, new Vector3(0f, -0.31f, 0.005f), new Vector3(0.11f, 0.13f, 0.095f), false);
        return elbow;
    }

    static Transform Leg(Transform hip, Material shorts, Material skin, Material socks, Material boots)
    {
        MeshUtil.Part("Thigh", hip, BodyMesh.Thigh, skin, Vector3.zero, Vector3.one);
        MeshUtil.Part("ShortLeg", hip, BodyMesh.ShortLeg, shorts, Vector3.zero, Vector3.one);
        var knee = Joint("Knee", hip, new Vector3(0f, -0.46f, 0f));
        MeshUtil.Part("Shin", knee, BodyMesh.Shin, socks, Vector3.zero, Vector3.one);
        MeshUtil.Part("Boot", knee, MeshUtil.SphereHi, boots, new Vector3(0f, -0.425f, 0.045f), new Vector3(0.115f, 0.10f, 0.29f));
        return knee;
    }

    // ------------------------------------------------------------------ triggers

    public void PlayKick() { kickT = 0f; }
    public void PlayThrow() { throwT = 0f; }
    public void PlayDive(float side) { diveT = 0f; diveSide = Mathf.Sign(side == 0f ? 1f : side); }
    public void Celebrate() { celebrateT = 0f; }
    public void Stumble() { stumbleT = 0f; }

    public void ResetPose()
    {
        kickT = throwT = diveT = celebrateT = stumbleT = -1f;
        if (body != null)
        {
            body.localRotation = Quaternion.identity;
            body.localPosition = new Vector3(0f, 0.95f, 0f);
        }
    }

    // ------------------------------------------------------------------ animation

    /// <param name="speed">Ground speed in m/s.</param>
    /// <param name="sideways">Local sideways speed component (for strafing lean).</param>
    public void Animate(float speed, float dt, bool withBall)
    {
        if (body == null) return;
        float a = Mathf.Clamp01(speed / 7.5f);
        float stride = 1.0f + a * 1.1f;
        if (speed > 0.2f) phase += speed / stride * Mathf.PI * dt;
        else phase = Mathf.MoveTowards(phase, Mathf.Round(phase / Mathf.PI) * Mathf.PI, dt * 4f);

        float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
        float amp = 8f + 42f * a;

        float thighL = -s * amp, thighR = s * amp;
        float kneeLb = 4f + a * 75f * Mathf.Max(0f, c) + a * 10f;
        float kneeRb = 4f + a * 75f * Mathf.Max(0f, -c) + a * 10f;
        float armL = s * amp * 0.75f, armR = -s * amp * 0.75f;
        float elbow = -(15f + 65f * a);
        float spread = 6f + 4f * a;
        float lean = a * 11f + (withBall ? 4f : 0f);
        float bodyY = 0.95f - a * 0.035f + a * 0.045f * Mathf.Abs(c);
        float roll = 0f;
        float armUpL = 0f, armUpR = 0f;

        if (keeperStance && speed < 1.5f && diveT < 0f)
        {
            kneeLb = kneeRb = 28f;
            thighL = thighR = -14f;
            bodyY = 0.88f;
            lean = 14f;
            armL = armR = -38f;
            spread = 22f;
            elbow = -35f;
        }

        // Kick (right leg)
        if (kickT >= 0f)
        {
            kickT += dt;
            float t = kickT;
            if (t < 0.12f) { thighR = Mathf.Lerp(thighR, 45f, t / 0.12f); kneeRb = 75f; }
            else if (t < 0.3f) { float k = (t - 0.12f) / 0.18f; thighR = Mathf.Lerp(45f, -75f, k); kneeRb = Mathf.Lerp(75f, 5f, k); }
            else if (t < 0.48f) { float k = (t - 0.3f) / 0.18f; thighR = Mathf.Lerp(-75f, thighR, k); kneeRb = Mathf.Lerp(5f, kneeRb, k); }
            else kickT = -1f;
            armL = -30f; armR = 25f; spread = 30f;
            lean -= 6f;
        }

        // Throw-in
        if (throwT >= 0f)
        {
            throwT += dt;
            float k = Mathf.Clamp01(throwT / 0.35f);
            armUpL = armUpR = Mathf.Lerp(-200f, -150f, k);
            elbow = Mathf.Lerp(-80f, 0f, k);
            if (throwT > 0.6f) throwT = -1f;
        }

        // Celebration
        if (celebrateT >= 0f)
        {
            celebrateT += dt;
            armUpL = armUpR = -160f + Mathf.Sin(celebrateT * 12f) * 15f;
            elbow = -10f;
            spread = 25f;
            if (celebrateT > 3f) celebrateT = -1f;
        }

        // Stumble after a failed tackle / being tackled
        if (stumbleT >= 0f)
        {
            stumbleT += dt;
            lean += Mathf.Sin(stumbleT * 9f) * 18f * (1f - stumbleT);
            if (stumbleT > 1f) stumbleT = -1f;
        }

        // Goalkeeper dive
        if (diveT >= 0f)
        {
            diveT += dt;
            float t = diveT;
            float k = t < 0.25f ? t / 0.25f : (t < 1.1f ? 1f : Mathf.Clamp01(1f - (t - 1.1f) / 0.4f));
            roll = -diveSide * 78f * k;
            bodyY = 0.95f + 0.35f * Mathf.Sin(Mathf.Clamp01(t / 0.6f) * Mathf.PI) * (t < 0.6f ? 1f : 0f) - 0.45f * (t > 0.5f && t < 1.3f ? k : 0f);
            armUpL = armUpR = -170f * k;
            elbow = 0f;
            thighL = thighR = 0f;
            kneeLb = kneeRb = 10f;
            lean = 0f;
            if (t > 1.5f) diveT = -1f;
        }

        body.localPosition = new Vector3(0f, bodyY, 0f);
        body.localRotation = Quaternion.Euler(0f, 0f, roll);
        torso.localRotation = Quaternion.Euler(lean, 0f, 0f);
        hipL.localRotation = Quaternion.Euler(thighL, 0f, 0f);
        hipR.localRotation = Quaternion.Euler(thighR, 0f, 0f);
        kneeL.localRotation = Quaternion.Euler(kneeLb, 0f, 0f);
        kneeR.localRotation = Quaternion.Euler(kneeRb, 0f, 0f);
        shoulderL.localRotation = Quaternion.Euler(armUpL != 0f ? armUpL : armL, 0f, -spread);
        shoulderR.localRotation = Quaternion.Euler(armUpR != 0f ? armUpR : armR, 0f, spread);
        elbowL.localRotation = Quaternion.Euler(elbow, 0f, 0f);
        elbowR.localRotation = Quaternion.Euler(elbow, 0f, 0f);
    }
}
