using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum GameAction { Pass = 0, Through = 1, Shoot = 2, Sprint = 3 }

/// <summary>
/// Merges on-screen controls with keyboard (for testing in the Unity editor).
/// Keyboard: WASD/Arrows move, Shift sprint, J pass, L through ball, K shoot (hold).
/// </summary>
public static class GameInput
{
    public static VirtualStick Stick;
    static readonly TouchButton[] buttons = new TouchButton[4];

    public static void Register(TouchButton b) { buttons[(int)b.action] = b; }
    public static void Unregister(TouchButton b) { if (buttons[(int)b.action] == b) buttons[(int)b.action] = null; }

    public static Vector2 Move
    {
        get
        {
            Vector2 touch = Stick != null ? Stick.Value : Vector2.zero;
            Vector2 keys = KeyboardMove();
            return Vector2.ClampMagnitude(keys.sqrMagnitude > touch.sqrMagnitude ? keys : touch, 1f);
        }
    }

    public static bool Held(GameAction a)
    {
        var b = buttons[(int)a];
        return (b != null && b.IsHeld) || KeyHeld(a);
    }

    public static bool Pressed(GameAction a)
    {
        var b = buttons[(int)a];
        bool ui = b != null && b.ConsumePress();
        return ui | KeyDown(a);
    }

    public static bool Released(GameAction a)
    {
        var b = buttons[(int)a];
        bool ui = b != null && b.ConsumeRelease();
        return ui | KeyUp(a);
    }

    public static void ClearAll()
    {
        foreach (var b in buttons) if (b != null) b.Clear();
    }

    public static void SetLabel(GameAction a, string label)
    {
        var b = buttons[(int)a];
        if (b != null && b.label != null && b.label.text != label) b.label.text = label;
    }

#if ENABLE_LEGACY_INPUT_MANAGER
    static KeyCode Code(GameAction a)
    {
        switch (a)
        {
            case GameAction.Pass: return KeyCode.J;
            case GameAction.Through: return KeyCode.L;
            case GameAction.Shoot: return KeyCode.K;
            default: return KeyCode.LeftShift;
        }
    }
    static bool KeyHeld(GameAction a) { return Input.GetKey(Code(a)); }
    static bool KeyDown(GameAction a) { return Input.GetKeyDown(Code(a)); }
    static bool KeyUp(GameAction a) { return Input.GetKeyUp(Code(a)); }
    static Vector2 KeyboardMove()
    {
        float x = 0f, y = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
        return new Vector2(x, y).normalized;
    }
    public static bool BackPressed() { return Input.GetKeyDown(KeyCode.Escape); }
#else
    static bool KeyHeld(GameAction a) { return false; }
    static bool KeyDown(GameAction a) { return false; }
    static bool KeyUp(GameAction a) { return false; }
    static Vector2 KeyboardMove() { return Vector2.zero; }
    public static bool BackPressed() { return false; }
#endif
}
