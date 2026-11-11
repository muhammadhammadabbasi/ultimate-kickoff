using UnityEngine;

/// <summary>Smooth behind-the-attack camera that follows the ball (team 0 always attacks up the screen).</summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 13f, -13f);
    public float smooth = 4f;
    public float lookAhead = 4f;
    public float maxSideways = 12f;

    Vector3 focus;

    void Start()
    {
        if (target != null) focus = new Vector3(target.position.x, 0f, target.position.z);
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 t = target.position;
        t.y = 0f;
        focus = Vector3.Lerp(focus, t, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        Vector3 f = new Vector3(Mathf.Clamp(focus.x, -maxSideways, maxSideways), 0f, focus.z);
        transform.position = f + offset;
        transform.LookAt(f + Vector3.forward * lookAhead + Vector3.up * 0.5f);
    }
}
