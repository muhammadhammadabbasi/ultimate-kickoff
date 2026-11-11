using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>On-screen analog stick (bottom-left of the screen).</summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform background;
    public RectTransform knob;
    public float deadZone = 0.1f;

    public Vector2 Value { get; private set; }

    Canvas canvas;

    void Awake()
    {
        if (background == null) background = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
    }

    void OnEnable() { FootballInput.Joystick = this; }

    void OnDisable()
    {
        if (FootballInput.Joystick == this) FootballInput.Joystick = null;
        Value = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData e) { OnDrag(e); }

    public void OnDrag(PointerEventData e)
    {
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, e.position, cam, out Vector2 local)) return;
        float radius = background.rect.width * 0.5f;
        Vector2 v = local / radius;
        if (v.magnitude > 1f) v.Normalize();
        Value = v.magnitude < deadZone ? Vector2.zero : v;
        if (knob != null) knob.anchoredPosition = v * radius;
    }

    public void OnPointerUp(PointerEventData e)
    {
        Value = Vector2.zero;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
    }
}
