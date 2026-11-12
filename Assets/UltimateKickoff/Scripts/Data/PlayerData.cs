using UnityEngine;

public enum Pos { GK, CB, LB, RB, CDM, CM, CAM, LM, RM, LW, RW, ST }

public enum PosGroup { GK, DEF, MID, ATT }

/// <summary>One footballer card. For goalkeepers the six stats mean DIV, HAN, KIC, REF, SPD, POS.</summary>
public class PlayerData
{
    public int id;
    public string name;
    public Pos pos;
    public string nation;
    public int ovr;
    public int pac, sho, pas, dri, def, phy;
    public string club;
    public bool legend;

    public bool IsGK => pos == Pos.GK;

    public static PosGroup Group(Pos p)
    {
        switch (p)
        {
            case Pos.GK: return PosGroup.GK;
            case Pos.CB: case Pos.LB: case Pos.RB: return PosGroup.DEF;
            case Pos.CDM: case Pos.CM: case Pos.CAM: case Pos.LM: case Pos.RM: return PosGroup.MID;
            default: return PosGroup.ATT;
        }
    }

    /// <summary>Rating when this player plays in a given slot (out of position = lower).</summary>
    public int RatingAt(Pos slot)
    {
        if (slot == pos) return ovr;
        PosGroup a = Group(pos), b = Group(slot);
        if (a == PosGroup.GK || b == PosGroup.GK) return Mathf.Max(30, ovr - 45);
        if (a == b) return ovr - 2;
        bool near = (a == PosGroup.MID) || (b == PosGroup.MID);
        return ovr - (near ? 6 : 12);
    }

    public string[] StatLabels => IsGK
        ? new[] { "DIV", "HAN", "KIC", "REF", "SPD", "POS" }
        : new[] { "PAC", "SHO", "PAS", "DRI", "DEF", "PHY" };

    public int[] Stats => new[] { pac, sho, pas, dri, def, phy };

    /// <summary>Stable pseudo-random value per player (skin/hair variety).</summary>
    public int Hash => (id * 7919 + 104729) & 0x7fffffff;
}

public class ClubData
{
    public string name;
    public string abbr;
    public Color kit;
    public Color kitAlt;
    public Color shorts;
    public int formation;
}
