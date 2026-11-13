using UnityEngine;

/// <summary>Menu orbit camera + TV broadcast camera that follows the ball from the side.</summary>
public class CameraRig : MonoBehaviour
{
    public enum Mode { Menu, Broadcast }

    public Mode mode = Mode.Menu;
    public float height = 21f;
    public float back = 29f;

    static readonly float[] Heights = { 13f, 21f, 31f };
    static readonly float[] Backs = { 18f, 29f, 42f };

    /// <summary>Reads the NEAR / MEDIUM / FAR camera setting.</summary>
    public void ApplyZoom()
    {
        int i = Mathf.Clamp(Squad.cameraIndex, 0, Heights.Length - 1);
        height = Heights[i];
        back = Backs[i];
        QualitySettings.shadowDistance = 60f + back * 1.6f;
    }

    Camera cam;
    Transform target;
    Vector3 focus;
    Vector3 focusVel;
    float orbit;

    void Awake() { Ensure(); }

    /// <summary>Other scripts may call us before our own Awake has run.</summary>
    void Ensure()
    {
        if (cam != null) return;
        cam = GetComponent<Camera>();
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 450f;
        cam.clearFlags = CameraClearFlags.Skybox;
    }

    public void SetMenu()
    {
        Ensure();
        mode = Mode.Menu;
        target = null;
        cam.fieldOfView = 45f;
    }

    public void SetBroadcast(Transform follow)
    {
        Ensure();
        mode = Mode.Broadcast;
        target = follow;
        cam.fieldOfView = 38f;
        ApplyZoom();
        focus = follow != null ? Flatten(follow.position) : Vector3.zero;
        focusVel = Vector3.zero;
        Apply();
    }

    static Vector3 Flatten(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

    void LateUpdate()
    {
        if (mode == Mode.Menu)
        {
            orbit += Time.unscaledDeltaTime * 3f;
            float a = orbit * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Sin(a) * 56f, 19f, -Mathf.Cos(a) * 38f);
            transform.position = pos;
            transform.LookAt(new Vector3(0f, 2f, 0f));
            return;
        }

        if (target == null) return;
        Vector3 desired = Flatten(target.position);
        var m = MatchController.I;
        if (m != null && m.ball != null)
        {
            if (m.possessionTeam == 0) desired.x += 5f;
            else if (m.possessionTeam == 1) desired.x -= 5f;
            if (m.ball.IsFree) desired += Flatten(m.ball.Velocity) * 0.25f;
        }
        desired.x = Mathf.Clamp(desired.x, -Pitch.HalfL + 15f, Pitch.HalfL - 15f);
        desired.z = Mathf.Clamp(desired.z * 0.6f, -Pitch.HalfW + 10f, Pitch.HalfW - 16f);
        focus = Vector3.SmoothDamp(focus, desired, ref focusVel, 0.38f, 60f, Time.unscaledDeltaTime);
        Apply();
    }

    void Apply()
    {
        transform.position = focus + new Vector3(0f, height, -back);
        transform.LookAt(focus + new Vector3(0f, 0.5f, 1.5f));
    }
}
