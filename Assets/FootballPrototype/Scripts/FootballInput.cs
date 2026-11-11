using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public enum FootballAction { Pass = 0, Shoot = 1, Sprint = 2 }

/// <summary>
/// Single place to read player input. Merges on-screen controls (phone) with keyboard (editor / PC).
/// Keyboard: WASD or Arrows = move, Left Shift = sprint, J = pass, K = shoot (hold to charge, release to shoot).
/// Works with both the old Input Manager and the new Input System package.
/// </summary>
public static class FootballInput
{
    public static VirtualJoystick Joystick;
    static readonly ActionButton[] buttons = new ActionButton[3];

    public static void Register(ActionButton b) { if (b) buttons[(int)b.action] = b; }
    public static void Unregister(ActionButton b) { if (b && buttons[(int)b.action] == b) buttons[(int)b.action] = null; }

    public static Vector2 Move
    {
        get
        {
            Vector2 touch = Joystick != null ? Joystick.Value : Vector2.zero;
            Vector2 keys = KeyboardMove();
            return Vector2.ClampMagnitude(keys.sqrMagnitude > touch.sqrMagnitude ? keys : touch, 1f);
        }
    }

    public static bool Held(FootballAction a)
    {
        var b = buttons[(int)a];
        return (b != null && b.IsHeld) || KeyHeld(a);
    }

    /// <summary>True once per press. Call once per frame.</summary>
    public static bool Pressed(FootballAction a)
    {
        var b = buttons[(int)a];
        bool ui = b != null && b.ConsumePress();
        return ui | KeyPressed(a);
    }

    /// <summary>True once per release. Call once per frame.</summary>
    public static bool Released(FootballAction a)
    {
        var b = buttons[(int)a];
        bool ui = b != null && b.ConsumeRelease();
        return ui | KeyReleased(a);
    }

#if ENABLE_INPUT_SYSTEM
    static KeyControl Key(FootballAction a)
    {
        var kb = Keyboard.current;
        if (kb == null) return null;
        switch (a)
        {
            case FootballAction.Pass: return kb.jKey;
            case FootballAction.Shoot: return kb.kKey;
            default: return kb.leftShiftKey;
        }
    }

    static bool KeyHeld(FootballAction a) { var k = Key(a); return k != null && k.isPressed; }
    static bool KeyPressed(FootballAction a) { var k = Key(a); return k != null && k.wasPressedThisFrame; }
    static bool KeyReleased(FootballAction a) { var k = Key(a); return k != null && k.wasReleasedThisFrame; }

    static Vector2 KeyboardMove()
    {
        var kb = Keyboard.current;
        if (kb == null) return Vector2.zero;
        float x = 0f, y = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
        return new Vector2(x, y).normalized;
    }
#elif ENABLE_LEGACY_INPUT_MANAGER
    static KeyCode Code(FootballAction a)
    {
        if (a == FootballAction.Pass) return KeyCode.J;
        if (a == FootballAction.Shoot) return KeyCode.K;
        return KeyCode.LeftShift;
    }

    static bool KeyHeld(FootballAction a) { return Input.GetKey(Code(a)); }
    static bool KeyPressed(FootballAction a) { return Input.GetKeyDown(Code(a)); }
    static bool KeyReleased(FootballAction a) { return Input.GetKeyUp(Code(a)); }

    static Vector2 KeyboardMove()
    {
        float x = 0f, y = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
        return new Vector2(x, y).normalized;
    }
#else
    static bool KeyHeld(FootballAction a) { return false; }
    static bool KeyPressed(FootballAction a) { return false; }
    static bool KeyReleased(FootballAction a) { return false; }
    static Vector2 KeyboardMove() { return Vector2.zero; }
#endif
}
