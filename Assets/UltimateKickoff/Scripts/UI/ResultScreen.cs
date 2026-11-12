using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>Full-time result screen.</summary>
public static class ResultScreen
{
    public static void Build(RectTransform root, TeamSheet home, TeamSheet away, int a, int b, List<GoalEvent> goals, ClubData opponent)
    {
        HomeScreen.Shade(root);

        string verdict = a > b ? "VICTORY!" : a == b ? "DRAW" : "DEFEAT";
        Color vColor = a > b ? UIKit.Gold : a == b ? Color.white : new Color(1f, 0.45f, 0.4f);
        var v = UIKit.Txt(root, verdict, 110, vColor, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1400f, 140f), TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
        UIKit.Outlined(v, new Color(0f, 0f, 0f, 0.8f), 4f);
        UIKit.Txt(root, "FULL TIME", 34, UIKit.TextDim, new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(600f, 44f));

        var panel = UIKit.Img(root, "Score", UIKit.Rounded, UIKit.Panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1300f, 520f));
        panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        UIKit.Outlined(panel, new Color(0.3f, 0.9f, 0.9f, 0.5f), 2f);
        var pt = panel.transform;

        TeamBlock(pt, home, new Vector2(-400f, 150f));
        TeamBlock(pt, away, new Vector2(400f, 150f));
        UIKit.Txt(pt, a + "  -  " + b, 130, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(500f, 160f));

        var left = new StringBuilder();
        var right = new StringBuilder();
        foreach (var g in goals)
        {
            string line = g.scorer + (g.ownGoal ? " (OG)" : "") + "  " + g.minute + "'";
            if (g.team == 0) left.AppendLine(line); else right.AppendLine(line);
        }
        var lt = UIKit.Txt(pt, left.ToString(), 30, Color.white, new Vector2(0.5f, 0f), new Vector2(-330f, 40f), new Vector2(560f, 250f), TextAnchor.UpperCenter, FontStyle.Normal);
        lt.verticalOverflow = UnityEngine.VerticalWrapMode.Truncate;
        var rt = UIKit.Txt(pt, right.ToString(), 30, Color.white, new Vector2(0.5f, 0f), new Vector2(330f, 40f), new Vector2(560f, 250f), TextAnchor.UpperCenter, FontStyle.Normal);
        rt.verticalOverflow = UnityEngine.VerticalWrapMode.Truncate;

        UIKit.Txt(root, $"RECORD   W {Squad.wins}   D {Squad.draws}   L {Squad.losses}", 30, UIKit.Gold,
            new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(1000f, 44f));
        UIKit.Btn(root, "REMATCH", UIKit.Blue, new Vector2(0.5f, 0f), new Vector2(-250f, 50f), new Vector2(420f, 110f), () => AppController.I.Rematch(), 40);
        UIKit.Btn(root, "HOME", UIKit.Green, new Vector2(0.5f, 0f), new Vector2(250f, 50f), new Vector2(420f, 110f), () => AppController.I.ShowHome(), 40);
    }

    static void TeamBlock(Transform parent, TeamSheet t, Vector2 pos)
    {
        var kit = UIKit.Img(parent, "Kit", UIKit.Rounded, t.kit, new Vector2(0.5f, 1f), new Vector2(pos.x, -40f), new Vector2(140f, 140f));
        UIKit.Outlined(kit, new Color(1f, 1f, 1f, 0.6f), 2f);
        bool light = t.kit.r + t.kit.g + t.kit.b > 2f;
        UIKit.FillText(kit.transform, t.abbr, 40, light ? new Color(0.1f, 0.1f, 0.15f) : Color.white);
        UIKit.Txt(parent, t.name.ToUpperInvariant(), 32, Color.white, new Vector2(0.5f, 1f), new Vector2(pos.x, -190f), new Vector2(500f, 44f));
    }
}
