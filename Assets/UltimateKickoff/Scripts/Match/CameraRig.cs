using UnityEngine;

/// <summary>Menu orbit camera + TV broadcast camera that follows the ball from the side.</summary>
public class CameraRig : MonoBehaviour
{
    public enum Mode { Menu, Broadcast }

    public Mode mode = Mode.Menu;
    public float height = 12.6f;
    public float back = 17.5f;

    Camera cam;
    Transform target;
    Vector3 focus;
    Vector3 focusVel;
    float orbit;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 450f;
        cam.clearFlags = CameraClearFlags.Skybox;
    }

    public void SetMenu()
    {
        mode = Mode.Menu;
        target = null;
        cam.fieldOfView = 45f;
    }

    public void SetBroadcast(Transform follow)
    {
        mode = Mode.Broadcast;
        target = follow;
        cam.fieldOfView = 40f;
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
            Vector3 pos = new Vector3(Mathf.Sin(a) * 62f, 22f, -Mathf.Cos(a) * 62f);
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
        desired.z = Mathf.Clamp(desired.z * 0.72f, -Pitch.HalfW + 8f, Pitch.HalfW - 12f);
        focus = Vector3.SmoothDamp(focus, desired, ref focusVel, 0.38f, 60f, Time.unscaledDeltaTime);
        Apply();
    }

    void Apply()
    {
        transform.position = focus + new Vector3(0f, height, -back);
        transform.LookAt(focus + new Vector3(0f, 0.5f, 1.5f));
    }
}
