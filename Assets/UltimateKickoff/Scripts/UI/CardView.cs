using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>FC-style player card built from UI primitives.</summary>
public static class CardView
{
    /// <param name="slotPos">Position the card is used in (shows effective rating). Null = player's own position.</param>
    public static RectTransform Build(Transform parent, PlayerData p, Pos? slotPos, Vector2 size, Action onClick = null)
    {
        Pos shownPos = slotPos ?? p.pos;
        int rating = p.RatingAt(shownPos);
        bool outOfPos = rating < p.ovr;

        Color tier = UIKit.TierColor(p);
        Color ink = UIKit.TierText(p);

        var bg = UIKit.Img(parent, "Card_" + p.name, UIKit.Rounded, tier, onClick != null);
        bg.pixelsPerUnitMultiplier = 2.2f;
        bg.rectTransform.sizeDelta = size;
        UIKit.Outlined(bg, new Color(0f, 0f, 0f, 0.55f), 2f);

        float w = size.x, h = size.y;

        // Shine
        var shine = UIKit.Img(bg.transform, "Shine", UIKit.Gradient, new Color(1f, 1f, 1f, 0.45f));
        UIKit.Stretch(shine.rectTransform, 3, 3, 3, h * 0.45f);

        // Inner frame
        var frame = UIKit.Img(bg.transform, "Frame", UIKit.Rounded, new Color(0f, 0f, 0f, 0.12f));
        frame.pixelsPerUnitMultiplier = 3f;
        UIKit.Stretch(frame.rectTransform, w * 0.06f, w * 0.06f, h * 0.05f, h * 0.2f);

        // Silhouette (head + shoulders)
        Color sil = new Color(ink.r, ink.g, ink.b, 0.28f);
        if (p.legend || p.ovr < 90) sil = new Color(0.2f, 0.15f, 0.05f, 0.25f);
        UIKit.Img(bg.transform, "Shoulders", UIKit.Rounded, sil, new Vector2(0.5f, 0f), new Vector2(w * 0.08f, h * 0.2f), new Vector2(w * 0.62f, h * 0.24f));
        UIKit.Img(bg.transform, "Head", UIKit.Circle, sil, new Vector2(0.5f, 0f), new Vector2(w * 0.08f, h * 0.42f), new Vector2(w * 0.34f, w * 0.34f));

        // Rating + position
        var ovrText = UIKit.Txt(bg.transform, rating.ToString(), Mathf.RoundToInt(h * 0.2f), outOfPos ? new Color(0.85f, 0.2f, 0.1f) : ink,
            new Vector2(0f, 1f), new Vector2(w * 0.07f, -h * 0.04f), new Vector2(w * 0.45f, h * 0.22f), TextAnchor.MiddleLeft);
        ovrText.fontStyle = FontStyle.Bold;
        UIKit.Txt(bg.transform, UIKit.PosName(shownPos), Mathf.RoundToInt(h * 0.09f), ink,
            new Vector2(0f, 1f), new Vector2(w * 0.08f, -h * 0.25f), new Vector2(w * 0.4f, h * 0.1f), TextAnchor.MiddleLeft);
        UIKit.Txt(bg.transform, p.nation, Mathf.RoundToInt(h * 0.075f), ink,
            new Vector2(0f, 1f), new Vector2(w * 0.08f, -h * 0.36f), new Vector2(w * 0.4f, h * 0.08f), TextAnchor.MiddleLeft, FontStyle.Normal);
        if (p.legend)
            UIKit.Txt(bg.transform, "ICON", Mathf.RoundToInt(h * 0.07f), new Color(0.55f, 0.35f, 0.05f),
                new Vector2(1f, 1f), new Vector2(-w * 0.08f, -h * 0.06f), new Vector2(w * 0.4f, h * 0.08f), TextAnchor.MiddleRight);

        // Name band
        var band = UIKit.Img(bg.transform, "NameBand", UIKit.RoundedSmall, new Color(0f, 0f, 0f, 0.55f));
        band.rectTransform.anchorMin = new Vector2(0f, 0f);
        band.rectTransform.anchorMax = new Vector2(1f, 0f);
        band.rectTransform.pivot = new Vector2(0.5f, 0f);
        band.rectTransform.offsetMin = new Vector2(w * 0.05f, h * 0.04f);
        band.rectTransform.offsetMax = new Vector2(-w * 0.05f, h * 0.2f);
        var nameT = UIKit.FillText(band.transform, p.name.ToUpperInvariant(), Mathf.RoundToInt(h * 0.075f), Color.white);
        nameT.horizontalOverflow = HorizontalWrapMode.Wrap;
        nameT.resizeTextForBestFit = true;
        nameT.resizeTextMinSize = 8;
        nameT.resizeTextMaxSize = Mathf.RoundToInt(h * 0.075f);

        if (onClick != null)
        {
            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => onClick());
        }
        return bg.rectTransform;
    }
}
