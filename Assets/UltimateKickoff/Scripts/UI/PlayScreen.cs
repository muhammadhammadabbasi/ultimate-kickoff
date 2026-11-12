using UnityEngine;
using UnityEngine.UI;

/// <summary>Opponent selection + match settings.</summary>
public static class PlayScreen
{
    public static void Build(RectTransform root)
    {
        HomeScreen.Shade(root);
        HomeScreen.Header(root, "SELECT OPPONENT", () => AppController.I.ShowHome());

        // Settings
        Button diff = null, half = null;
        diff = UIKit.Btn(root, "DIFFICULTY: " + Squad.DifficultyNames[Squad.difficulty], new Color(0.45f, 0.2f, 0.55f), new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(470f, 80f), () =>
        {
            Squad.difficulty = (Squad.difficulty + 1) % Squad.DifficultyNames.Length;
            Squad.Save();
            UIKit.ButtonText(diff).text = "DIFFICULTY: " + Squad.DifficultyNames[Squad.difficulty];
        }, 30);
        half = UIKit.Btn(root, HalfLabel(), new Color(0.15f, 0.35f, 0.6f), new Vector2(1f, 1f), new Vector2(-530f, -30f), new Vector2(380f, 80f), () =>
        {
            Squad.halfIndex = (Squad.halfIndex + 1) % Squad.HalfMinutesOptions.Length;
            Squad.Save();
            UIKit.ButtonText(half).text = HalfLabel();
        }, 30);

        // Your team summary
        var mine = UIKit.Img(root, "Mine", UIKit.RoundedSmall, new Color(0f, 0f, 0f, 0.45f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(900f, 64f));
        UIKit.FillText(mine.transform, Squad.teamName + "   OVR " + Squad.UserOvr() + "   •   " + Formations.Get(Squad.formation).name, 30, UIKit.Gold);

        // Club grid
        var clubs = PlayerDatabase.Clubs;
        const int cols = 5;
        float w = 330f, h = 250f, gap = 26f;
        int rows = Mathf.CeilToInt(clubs.Count / (float)cols);
        float totalW = cols * w + (cols - 1) * gap;
        float startY = 130f + (rows - 1) * (h + gap) * 0.5f;
        for (int i = 0; i < clubs.Count; i++)
        {
            var club = clubs[i];
            int r = i / cols, c = i % cols;
            float x = -totalW / 2f + w / 2f + c * (w + gap);
            float y = startY - r * (h + gap) - 160f;
            ClubTile(root, club, new Vector2(x, y), new Vector2(w, h));
        }
    }

    static string HalfLabel() { return "HALF: " + Squad.HalfMinutesOptions[Squad.halfIndex] + " MIN"; }

    static void ClubTile(RectTransform root, ClubData club, Vector2 pos, Vector2 size)
    {
        var sheet = Squad.ClubSheet(club);
        int ovr = sheet.Ovr;
        var bg = UIKit.Img(root, "Club_" + club.abbr, UIKit.Rounded, new Color(0.08f, 0.11f, 0.2f, 0.95f), new Vector2(0.5f, 0.5f), pos, size, true);
        bg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        UIKit.Outlined(bg, new Color(1f, 1f, 1f, 0.25f), 2f);

        var stripe = UIKit.Img(bg.transform, "Kit", UIKit.Rounded, club.kit, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(size.x - 24f, 92f));
        var shine = UIKit.Img(stripe.transform, "Shine", UIKit.Gradient, new Color(1f, 1f, 1f, 0.3f));
        UIKit.Stretch(shine.rectTransform, 4, 4, 4, 40);
        bool lightKit = club.kit.r + club.kit.g + club.kit.b > 2f;
        var ab = UIKit.FillText(stripe.transform, club.abbr, 54, lightKit ? new Color(0.1f, 0.1f, 0.15f) : Color.white, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
        UIKit.Shadowed(ab, 2f, 0.4f);

        UIKit.Txt(bg.transform, club.name.ToUpperInvariant(), 28, Color.white, new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(size.x - 20f, 40f));
        UIKit.Txt(bg.transform, "OVR " + ovr + "   " + Formations.Get(club.formation).name, 26, UIKit.Gold, new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(size.x - 20f, 36f));
        int stars = Mathf.Clamp((ovr - 80) / 2, 1, 5);
        UIKit.Txt(bg.transform, new string('*', stars), 34, new Color(1f, 0.85f, 0.3f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(size.x - 20f, 40f));

        var btn = bg.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(() => AppController.I.StartMatch(club));
    }
}
