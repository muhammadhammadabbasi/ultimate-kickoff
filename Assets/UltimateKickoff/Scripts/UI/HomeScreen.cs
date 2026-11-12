using UnityEngine;
using UnityEngine.UI;

/// <summary>Main menu.</summary>
public static class HomeScreen
{
    public static void Build(RectTransform root)
    {
        Squad.Load();
        Shade(root);

        // Top bar
        var top = UIKit.Img(root, "TopBar", UIKit.Gradient, new Color(0f, 0f, 0f, 0.65f));
        top.rectTransform.anchorMin = new Vector2(0f, 1f);
        top.rectTransform.anchorMax = new Vector2(1f, 1f);
        top.rectTransform.pivot = new Vector2(0.5f, 1f);
        top.rectTransform.sizeDelta = new Vector2(0f, 130f);
        top.rectTransform.anchoredPosition = Vector2.zero;

        var badge = UIKit.Img(root, "Badge", UIKit.Rounded, Squad.KitColors[Squad.kitIndex], new Vector2(0f, 1f), new Vector2(36f, -22f), new Vector2(92f, 92f));
        UIKit.Outlined(badge, UIKit.Gold, 3f);
        UIKit.FillText(badge.transform, Squad.abbr, 30, Color.white);
        UIKit.Txt(root, Squad.teamName, 42, Color.white, new Vector2(0f, 1f), new Vector2(148f, -24f), new Vector2(600f, 54f), TextAnchor.MiddleLeft);
        UIKit.Txt(root, "TEAM OVR  " + Squad.UserOvr() + "   •   " + Formations.Get(Squad.formation).name, 28, UIKit.Gold,
            new Vector2(0f, 1f), new Vector2(148f, -76f), new Vector2(700f, 40f), TextAnchor.MiddleLeft);

        string record = $"W {Squad.wins}   D {Squad.draws}   L {Squad.losses}   •   GOALS {Squad.goalsFor}:{Squad.goalsAgainst}";
        var chip = UIKit.Img(root, "Record", UIKit.RoundedSmall, new Color(0.08f, 0.12f, 0.22f, 0.9f), new Vector2(1f, 1f), new Vector2(-36f, -34f), new Vector2(620f, 66f));
        UIKit.FillText(chip.transform, record, 28, Color.white);

        // Title
        var t1 = UIKit.Txt(root, "ULTIMATE", 120, Color.white, new Vector2(0f, 0.5f), new Vector2(90f, 170f), new Vector2(900f, 140f), TextAnchor.MiddleLeft, FontStyle.BoldAndItalic);
        UIKit.Outlined(t1, new Color(0.05f, 0.1f, 0.3f, 0.9f), 4f);
        var t2 = UIKit.Txt(root, "KICKOFF", 150, UIKit.Gold, new Vector2(0f, 0.5f), new Vector2(90f, 30f), new Vector2(900f, 170f), TextAnchor.MiddleLeft, FontStyle.BoldAndItalic);
        UIKit.Outlined(t2, new Color(0.25f, 0.12f, 0f, 0.9f), 4f);
        UIKit.Txt(root, "11 v 11  •  WORLD STARS & LEGENDS  •  SEASON 1", 30, UIKit.TextDim, new Vector2(0f, 0.5f), new Vector2(96f, -80f), new Vector2(1000f, 44f), TextAnchor.MiddleLeft);

        // Tiles
        Tile(root, "PLAY", "Quick Match vs AI clubs", new Color(0.08f, 0.6f, 0.32f), new Vector2(1f, 0f), new Vector2(-40f, 150f), new Vector2(560f, 300f), 78,
            () => AppController.I.ShowPlay());
        var myTeam = Tile(root, "MY TEAM", "Lineup, formation & players", new Color(0.2f, 0.25f, 0.65f), new Vector2(1f, 0f), new Vector2(-40f, 475f), new Vector2(560f, 230f), 60,
            () => AppController.I.ShowMyTeam());
        var ovr = UIKit.Img(myTeam.transform, "Ovr", UIKit.Rounded, UIKit.Gold, new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(130f, 130f));
        UIKit.Txt(ovr.transform, "OVR", 24, new Color(0.3f, 0.15f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(120f, 30f));
        UIKit.Txt(ovr.transform, Squad.UserOvr().ToString(), 62, new Color(0.25f, 0.1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(120f, 80f));

        // Bottom tips
        var tip = UIKit.Img(root, "Tips", UIKit.RoundedSmall, new Color(0f, 0f, 0f, 0.5f), new Vector2(0f, 0f), new Vector2(36f, 36f), new Vector2(1080f, 70f));
        UIKit.FillText(tip.transform, "TIP: Hold SHOOT for power  •  THROUGH plays into space  •  PRESS & TACKLE to win the ball", 24, UIKit.TextDim);
    }

    public static void Shade(RectTransform root)
    {
        var shade = UIKit.Img(root, "Shade", null, new Color(0.01f, 0.02f, 0.06f, 0.55f));
        UIKit.Stretch(shade.rectTransform);
        var grad = UIKit.Img(root, "Grad", UIKit.Gradient, new Color(0.01f, 0.02f, 0.06f, 0.75f));
        UIKit.Stretch(grad.rectTransform);
        grad.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
    }

    public static Image Tile(RectTransform root, string title, string sub, Color color, Vector2 anchor, Vector2 pos, Vector2 size, int titleSize, System.Action onClick)
    {
        var img = UIKit.Img(root, "Tile_" + title, UIKit.Rounded, color, anchor, pos, size, true);
        UIKit.Outlined(img, new Color(1f, 1f, 1f, 0.35f), 2f);
        var gloss = UIKit.Img(img.transform, "Gloss", UIKit.Gradient, new Color(1f, 1f, 1f, 0.22f));
        UIKit.Stretch(gloss.rectTransform, 4, 4, 4, size.y * 0.4f);
        var glow = UIKit.Img(img.transform, "Glow", UIKit.Glow, new Color(1f, 1f, 1f, 0.15f), new Vector2(0f, 1f), new Vector2(-80f, 80f), new Vector2(360f, 360f));
        glow.raycastTarget = false;
        var t = UIKit.Txt(img.transform, title, titleSize, Color.white, new Vector2(0f, 0f), new Vector2(34f, 64f), new Vector2(size.x - 60f, titleSize + 20f), TextAnchor.MiddleLeft, FontStyle.BoldAndItalic);
        UIKit.Shadowed(t, 3f, 0.6f);
        UIKit.Txt(img.transform, sub, 26, new Color(1f, 1f, 1f, 0.85f), new Vector2(0f, 0f), new Vector2(36f, 26f), new Vector2(size.x - 60f, 36f), TextAnchor.MiddleLeft, FontStyle.Normal);
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick());
        return img;
    }

    public static void Header(RectTransform root, string title, System.Action back)
    {
        var bb = UIKit.Img(root, "Back", UIKit.Circle, new Color(0.15f, 0.18f, 0.28f, 0.95f), new Vector2(0f, 1f), new Vector2(30f, -24f), new Vector2(92f, 92f), true);
        UIKit.FillText(bb.transform, "<", 52, Color.white);
        var btn = bb.gameObject.AddComponent<Button>();
        btn.targetGraphic = bb;
        btn.onClick.AddListener(() => back());
        var t = UIKit.Txt(root, title, 58, Color.white, new Vector2(0f, 1f), new Vector2(145f, -28f), new Vector2(900f, 84f), TextAnchor.MiddleLeft, FontStyle.BoldAndItalic);
        UIKit.Shadowed(t, 3f, 0.6f);
    }
}
