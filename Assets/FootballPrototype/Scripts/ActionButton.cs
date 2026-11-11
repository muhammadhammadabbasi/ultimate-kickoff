using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>On-screen action button (Pass / Shoot / Sprint). Supports multi-touch with the joystick.</summary>
public class ActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public FootballAction action;
    public Graphic highlight;

    public bool IsHeld { get; private set; }

    bool pressed;
    bool released;
    Color baseColor;

    void Awake()
    {
        if (highlight == null) highlight = GetComponent<Graphic>();
        if (highlight != null) baseColor = highlight.color;
    }

    void OnEnable() { FootballInput.Register(this); }

    void OnDisable()
    {
        FootballInput.Unregister(this);
        IsHeld = false;
    }

    public void OnPointerDown(PointerEventData e)
    {
        IsHeld = true;
        pressed = true;
        Tint(true);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!IsHeld) return;
        IsHeld = false;
        released = true;
        Tint(false);
    }

    public bool ConsumePress() { bool p = pressed; pressed = false; return p; }
    public bool ConsumeRelease() { bool r = released; released = false; return r; }

    void Tint(bool down)
    {
        if (highlight == null) return;
        highlight.color = down
            ? new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Min(1f, baseColor.a + 0.35f))
            : baseColor;
    }
}
