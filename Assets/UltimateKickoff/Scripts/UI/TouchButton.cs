using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Round on-screen action button.</summary>
public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public GameAction action;
    public Image face;
    public Text label;

    public bool IsHeld { get; private set; }
    bool pressed, released;
    Color baseColor;

    void Start() { if (face != null) baseColor = face.color; }
    void OnEnable() { GameInput.Register(this); }
    void OnDisable()
    {
        GameInput.Unregister(this);
        IsHeld = false;
    }

    public void OnPointerDown(PointerEventData e)
    {
        IsHeld = true;
        pressed = true;
        if (face != null) face.color = Color.Lerp(baseColor, Color.white, 0.35f);
        transform.localScale = Vector3.one * 0.94f;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!IsHeld) return;
        IsHeld = false;
        released = true;
        if (face != null) face.color = baseColor;
        transform.localScale = Vector3.one;
    }

    public bool ConsumePress() { bool p = pressed; pressed = false; return p; }
    public bool ConsumeRelease() { bool r = released; released = false; return r; }
    public void Clear() { pressed = released = false; }
}
