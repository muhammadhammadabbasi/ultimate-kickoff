using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Lineup editor: formation, kit, cards on a pitch, and a player picker with stats.</summary>
public class MyTeamScreen : MonoBehaviour
{
    RectTransform root, cardsLayer, picker;
    Text ovrText, formationText, kitText;
    Image badge;
    int pickSlot = -1;
    bool showAll;

    public void Build()
    {
        Squad.Load();
        root = (RectTransform)transform;
        HomeScreen.Shade(root);
        HomeScreen.Header(root, "MY TEAM", () => AppController.I.ShowHome());

        // ---------------- Left panel
        var panel = UIKit.Img(root, "Panel", UIKit.Rounded, UIKit.Panel, new Vector2(0f, 0f), new Vector2(30f, 30f), new Vector2(480f, 880f));
        UIKit.Outlined(panel, new Color(0.3f, 0.9f, 0.9f, 0.5f), 2f);
        var pt = panel.transform;

        UIKit.Txt(pt, Squad.teamName, 40, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(440f, 56f));
        badge = UIKit.Img(pt, "Badge", UIKit.Rounded, Squad.KitColors[Squad.kitIndex], new Vector2(0.5f, 1f), new Vector2(-95f, -100f), new Vector2(140f, 140f));
        UIKit.Outlined(badge, UIKit.Gold, 3f);
        UIKit.FillText(badge.transform, Squad.abbr, 40, Color.white);
        var ovrBox = UIKit.Img(pt, "Ovr", UIKit.Rounded, new Color(0.75f, 0.15f, 0.15f), new Vector2(0.5f, 1f), new Vector2(95f, -100f), new Vector2(140f, 140f));
        UIKit.Outlined(ovrBox, UIKit.Gold, 3f);
        UIKit.Txt(ovrBox.transform, "OVR", 24, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(130f, 30f));
        ovrText = UIKit.Txt(ovrBox.transform, "", 66, Color.white, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(130f, 80f));

        UIKit.Txt(pt, "FORMATION", 26, UIKit.TextDim, new Vector2(0.5f, 1f), new Vector2(0f, -268f), new Vector2(440f, 36f));
        formationText = Selector(pt, -320f, () => ChangeFormation(-1), () => ChangeFormation(1));
        UIKit.Txt(pt, "KIT COLOUR", 26, UIKit.TextDim, new Vector2(0.5f, 1f), new Vector2(0f, -410f), new Vector2(440f, 36f));
        kitText = Selector(pt, -462f, () => ChangeKit(-1), () => ChangeKit(1));

        UIKit.Btn(pt, "AUTO BUILD", new Color(0.15f, 0.4f, 0.75f), new Vector2(0.5f, 1f), new Vector2(0f, -590f), new Vector2(400f, 96f), () =>
        {
            Squad.AutoBuildUser();
            Refresh();
        }, 36);
        UIKit.Btn(pt, "PLAY MATCH", UIKit.Green, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(400f, 110f), () => AppController.I.ShowPlay(), 42);
        UIKit.Txt(pt, "Tap a card to change the player", 22, UIKit.TextDim, new Vector2(0.5f, 0f), new Vector2(0f, 160f), new Vector2(440f, 32f), TextAnchor.MiddleCenter, FontStyle.Normal);

        // ---------------- Pitch
        var pitch = UIKit.Img(root, "Pitch", UIKit.RoundedSmall, new Color(0.17f, 0.5f, 0.2f, 0.95f));
        UIKit.Stretch(pitch.rectTransform, 540f, 30f, 140f, 30f);
        var pr = pitch.rectTransform;
        for (int i = 0; i < 10; i += 2)
        {
            var s = UIKit.Img(pr, "Stripe", null, new Color(1f, 1f, 1f, 0.05f));
            s.rectTransform.anchorMin = new Vector2(0f, i / 10f);
            s.rectTransform.anchorMax = new Vector2(1f, (i + 1) / 10f);
            s.rectTransform.offsetMin = s.rectTransform.offsetMax = Vector2.zero;
        }
        Color line = new Color(1f, 1f, 1f, 0.55f);
        LineRect(pr, 0.02f, 0.02f, 0.98f, 0.98f, line);
        HLine(pr, 0.02f, 0.98f, 0.5f, line);
        var circle = UIKit.Img(pr, "Circle", UIKit.Ring, line, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 200f));
        circle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        LineRect(pr, 0.28f, 0.02f, 0.72f, 0.17f, line);
        LineRect(pr, 0.28f, 0.83f, 0.72f, 0.98f, line);

        cardsLayer = UIKit.Rect("Cards", pr);
        UIKit.Stretch(cardsLayer);

        Refresh();
    }

    Text Selector(Transform parent, float y, System.Action prev, System.Action next)
    {
        UIKit.Btn(parent, "<", new Color(0.2f, 0.24f, 0.38f), new Vector2(0.5f, 1f), new Vector2(-175f, y), new Vector2(80f, 80f), prev, 44);
        UIKit.Btn(parent, ">", new Color(0.2f, 0.24f, 0.38f), new Vector2(0.5f, 1f), new Vector2(175f, y), new Vector2(80f, 80f), next, 44);
        var bg = UIKit.Img(parent, "Value", UIKit.RoundedSmall, new Color(0f, 0f, 0f, 0.4f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(250f, 80f));
        return UIKit.FillText(bg.transform, "", 36, Color.white);
    }

    static void HLine(RectTransform parent, float x0, float x1, float y, Color c)
    {
        var img = UIKit.Img(parent, "Line", null, c);
        img.rectTransform.anchorMin = new Vector2(x0, y);
        img.rectTransform.anchorMax = new Vector2(x1, y);
        img.rectTransform.sizeDelta = new Vector2(0f, 3f);
        img.rectTransform.anchoredPosition = Vector2.zero;
    }

    static void VLine(RectTransform parent, float x, float y0, float y1, Color c)
    {
        var img = UIKit.Img(parent, "Line", null, c);
        img.rectTransform.anchorMin = new Vector2(x, y0);
        img.rectTransform.anchorMax = new Vector2(x, y1);
        img.rectTransform.sizeDelta = new Vector2(3f, 0f);
        img.rectTransform.anchoredPosition = Vector2.zero;
    }

    static void LineRect(RectTransform parent, float x0, float y0, float x1, float y1, Color c)
    {
        HLine(parent, x0, x1, y0, c);
        HLine(parent, x0, x1, y1, c);
        VLine(parent, x0, y0, y1, c);
        VLine(parent, x1, y0, y1, c);
    }

    void ChangeFormation(int delta)
    {
        int n = Formations.All.Length;
        var eleven = new List<PlayerData>(Squad.LineupPlayers());
        Squad.formation = (Squad.formation + delta + n) % n;
        var arranged = Squad.AutoBuild(Squad.formation, eleven);
        for (int i = 0; i < 11; i++) Squad.lineup[i] = arranged[i].id;
        Squad.Save();
        Refresh();
    }

    void ChangeKit(int delta)
    {
        int n = Squad.KitColors.Length;
        Squad.kitIndex = (Squad.kitIndex + delta + n) % n;
        Squad.Save();
        Refresh();
    }

    void Refresh()
    {
        ovrText.text = Squad.UserOvr().ToString();
        formationText.text = Formations.Get(Squad.formation).name;
        kitText.text = Squad.KitNames[Squad.kitIndex];
        badge.color = Squad.KitColors[Squad.kitIndex];

        for (int i = cardsLayer.childCount - 1; i >= 0; i--) Destroy(cardsLayer.GetChild(i).gameObject);
        var f = Formations.Get(Squad.formation);
        for (int i = 0; i < 11; i++)
        {
            var p = Squad.LineupPlayer(i);
            if (p == null) continue;
            var slot = f.slots[i];
            float y = Mathf.Lerp(0.1f, 0.9f, Mathf.InverseLerp(0.03f, 0.66f, slot.depth));
            float x = Mathf.Lerp(0.09f, 0.91f, (slot.side + 1f) * 0.5f);
            int idx = i;
            var card = CardView.Build(cardsLayer, p, slot.pos, new Vector2(132f, 176f), () => OpenPicker(idx));
            card.anchorMin = card.anchorMax = new Vector2(x, y);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;
        }
    }

    // ================================================================== picker

    void OpenPicker(int slot)
    {
        pickSlot = slot;
        BuildPicker();
    }

    void ClosePicker()
    {
        if (picker != null) Destroy(picker.gameObject);
        picker = null;
        pickSlot = -1;
    }

    void BuildPicker()
    {
        if (picker != null) Destroy(picker.gameObject);
        var f = Formations.Get(Squad.formation);
        Pos slotPos = f.slots[pickSlot].pos;
        var current = Squad.LineupPlayer(pickSlot);

        var blocker = UIKit.Img(root, "PickerBlocker", null, new Color(0f, 0f, 0f, 0.55f), true);
        UIKit.Stretch(blocker.rectTransform);
        picker = blocker.rectTransform;

        var panel = UIKit.Img(picker, "PickerPanel", UIKit.Rounded, new Color(0.05f, 0.07f, 0.14f, 0.98f));
        UIKit.Stretch(panel.rectTransform, 520f, 20f, 20f, 20f);
        UIKit.Outlined(panel, new Color(0.3f, 0.9f, 0.9f, 0.5f), 2f);
        var pr = panel.rectTransform;

        UIKit.Txt(pr, "SELECT " + slotPos + "  •  now: " + (current != null ? current.name : "-"), 38, Color.white,
            new Vector2(0f, 1f), new Vector2(30f, -22f), new Vector2(1000f, 60f), TextAnchor.MiddleLeft);
        var close = UIKit.Btn(pr, "X", UIKit.Red, new Vector2(1f, 1f), new Vector2(-20f, -16f), new Vector2(80f, 80f), () => ClosePicker(), 40);

        UIKit.Btn(pr, "BEST FOR " + slotPos, showAll ? new Color(0.2f, 0.24f, 0.38f) : UIKit.Blue, new Vector2(0f, 1f), new Vector2(30f, -96f), new Vector2(360f, 64f), () =>
        {
            showAll = false;
            BuildPicker();
        }, 28);
        UIKit.Btn(pr, "ALL PLAYERS", showAll ? UIKit.Blue : new Color(0.2f, 0.24f, 0.38f), new Vector2(0f, 1f), new Vector2(410f, -96f), new Vector2(300f, 64f), () =>
        {
            showAll = true;
            BuildPicker();
        }, 28);

        // Scroll list
        var view = UIKit.Rect("Viewport", pr);
        UIKit.Stretch(view, 20f, 20f, 180f, 20f);
        var viewImg = view.gameObject.AddComponent<Image>();
        viewImg.color = new Color(0f, 0f, 0f, 0.01f);
        view.gameObject.AddComponent<RectMask2D>();

        var content = UIKit.Rect("Content", view);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;
        content.anchoredPosition = Vector2.zero;
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10f;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = panel.gameObject.AddComponent<ScrollRect>();
        sr.viewport = view;
        sr.content = content;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.scrollSensitivity = 40f;

        var list = new List<PlayerData>();
        PosGroup g = PlayerData.Group(slotPos);
        foreach (var p in PlayerDatabase.All)
        {
            if (!showAll && PlayerData.Group(p.pos) != g) continue;
            list.Add(p);
        }
        list.Sort((a, b) =>
        {
            int c = b.RatingAt(slotPos).CompareTo(a.RatingAt(slotPos));
            return c != 0 ? c : b.ovr.CompareTo(a.ovr);
        });

        foreach (var p in list) Row(content, p, slotPos, f);
        close.transform.SetAsLastSibling();
    }

    void Row(RectTransform content, PlayerData p, Pos slotPos, Formation f)
    {
        int inSlot = Squad.SlotOf(p.id);
        bool isCurrent = inSlot == pickSlot;
        var row = UIKit.Img(content, "Row", UIKit.RoundedSmall, isCurrent ? new Color(0.15f, 0.35f, 0.3f) : new Color(0.1f, 0.13f, 0.24f), true);
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 118f;
        le.minHeight = 118f;

        int rating = p.RatingAt(slotPos);
        var badgeImg = UIKit.Img(row.transform, "Ovr", UIKit.RoundedSmall, UIKit.TierColor(p), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(100f, 100f));
        Color ink = UIKit.TierText(p);
        UIKit.Txt(badgeImg.transform, rating.ToString(), 46, rating < p.ovr ? new Color(0.8f, 0.15f, 0.1f) : ink, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(96f, 56f));
        UIKit.Txt(badgeImg.transform, p.pos.ToString(), 24, ink, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(96f, 30f));

        UIKit.Txt(row.transform, p.name, 36, Color.white, new Vector2(0f, 0.5f), new Vector2(132f, 18f), new Vector2(520f, 50f), TextAnchor.MiddleLeft);
        string club = p.legend ? "ICON" : string.IsNullOrEmpty(p.club) ? "FREE AGENT" : p.club;
        string sub = p.nation + "  •  " + club + (inSlot >= 0 ? "  •  IN XI (" + f.slots[inSlot].pos + ")" : "");
        UIKit.Txt(row.transform, sub, 24, inSlot >= 0 ? UIKit.Accent : UIKit.TextDim, new Vector2(0f, 0.5f), new Vector2(132f, -26f), new Vector2(560f, 36f), TextAnchor.MiddleLeft, FontStyle.Normal);

        string[] labels = p.StatLabels;
        int[] stats = p.Stats;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 6; i++)
        {
            if (i == 3) sb.Append('\n');
            string col = stats[i] >= 85 ? "#6CFF8A" : stats[i] >= 70 ? "#FFD966" : "#FFFFFF";
            sb.Append("<color=#9AA6C0>").Append(labels[i]).Append("</color> <color=").Append(col).Append('>').Append(stats[i]).Append("</color>   ");
        }
        var statsText = UIKit.Txt(row.transform, sb.ToString(), 28, Color.white, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(520f, 96f), TextAnchor.MiddleRight);
        statsText.supportRichText = true;
        statsText.lineSpacing = 1.1f;

        var btn = row.gameObject.AddComponent<Button>();
        btn.targetGraphic = row;
        int id = p.id;
        btn.onClick.AddListener(() =>
        {
            Squad.Assign(pickSlot, id);
            ClosePicker();
            Refresh();
        });
    }
}
