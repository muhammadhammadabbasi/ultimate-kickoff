using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Floating analog stick: appears where the thumb touches inside its zone.</summary>
public class VirtualStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform zone;
    public RectTransform baseRing;
    public RectTransform knob;
    public float radius = 120f;

    public Vector2 Value { get; private set; }
    public bool Active { get; private set; }

    Vector2 homePos;

    void Start() { if (baseRing != null) homePos = baseRing.anchoredPosition; }
    void OnEnable() { GameInput.Stick = this; }
    void OnDisable()
    {
        if (GameInput.Stick == this) GameInput.Stick = null;
        Release();
    }

    public void OnPointerDown(PointerEventData e)
    {
        Vector2 local;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)baseRing.parent, e.position, e.pressEventCamera, out local))
            baseRing.localPosition = local;
        Active = true;
        OnDrag(e);
    }

    public void OnDrag(PointerEventData e)
    {
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRing, e.position, e.pressEventCamera, out local)) return;
        Vector2 v = local / radius;
        if (v.magnitude > 1f) v.Normalize();
        Value = v.magnitude < 0.12f ? Vector2.zero : v;
        knob.anchoredPosition = v * radius;
    }

    public void OnPointerUp(PointerEventData e) { Release(); }

    void Release()
    {
        Value = Vector2.zero;
        Active = false;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
        if (baseRing != null) baseRing.anchoredPosition = homePos;
    }
}
