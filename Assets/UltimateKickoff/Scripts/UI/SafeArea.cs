using UnityEngine;

/// <summary>Keeps UI inside the phone's safe area (notches, rounded corners).</summary>
public class SafeArea : MonoBehaviour
{
    RectTransform rt;
    Rect last;
    Vector2Int lastRes;

    void Awake()
    {
        rt = (RectTransform)transform;
        Apply();
    }

    void Update()
    {
        if (Screen.safeArea != last || lastRes.x != Screen.width || lastRes.y != Screen.height) Apply();
    }

    void Apply()
    {
        Rect sa = Screen.safeArea;
        last = sa;
        lastRes = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width <= 0 || Screen.height <= 0) return;
        Vector2 min = sa.position, max = sa.position + sa.size;
        min.x /= Screen.width; max.x /= Screen.width;
        min.y /= Screen.height; max.y /= Screen.height;
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
