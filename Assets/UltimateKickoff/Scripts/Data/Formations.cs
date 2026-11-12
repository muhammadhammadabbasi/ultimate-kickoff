using UnityEngine;

public struct FormationSlot
{
    public Pos pos;
    /// <summary>0 = own goal line, 0.5 = halfway, 1 = opponent goal line.</summary>
    public float depth;
    /// <summary>-1 = left touchline, +1 = right touchline (as seen when attacking).</summary>
    public float side;

    public FormationSlot(Pos pos, float depth, float side) { this.pos = pos; this.depth = depth; this.side = side; }
}

public class Formation
{
    public string name;
    public FormationSlot[] slots;
}

public static class Formations
{
    public static readonly Formation[] All =
    {
        new Formation { name = "4-3-3", slots = new[] {
            S(Pos.GK, 0.03f, 0f),
            S(Pos.LB, 0.2f, -0.8f), S(Pos.CB, 0.17f, -0.3f), S(Pos.CB, 0.17f, 0.3f), S(Pos.RB, 0.2f, 0.8f),
            S(Pos.CM, 0.37f, -0.42f), S(Pos.CDM, 0.3f, 0f), S(Pos.CM, 0.37f, 0.42f),
            S(Pos.LW, 0.58f, -0.72f), S(Pos.ST, 0.64f, 0f), S(Pos.RW, 0.58f, 0.72f) } },

        new Formation { name = "4-4-2", slots = new[] {
            S(Pos.GK, 0.03f, 0f),
            S(Pos.LB, 0.2f, -0.8f), S(Pos.CB, 0.17f, -0.3f), S(Pos.CB, 0.17f, 0.3f), S(Pos.RB, 0.2f, 0.8f),
            S(Pos.LM, 0.4f, -0.8f), S(Pos.CM, 0.35f, -0.25f), S(Pos.CM, 0.35f, 0.25f), S(Pos.RM, 0.4f, 0.8f),
            S(Pos.ST, 0.62f, -0.22f), S(Pos.ST, 0.62f, 0.22f) } },

        new Formation { name = "4-2-3-1", slots = new[] {
            S(Pos.GK, 0.03f, 0f),
            S(Pos.LB, 0.2f, -0.8f), S(Pos.CB, 0.17f, -0.3f), S(Pos.CB, 0.17f, 0.3f), S(Pos.RB, 0.2f, 0.8f),
            S(Pos.CDM, 0.3f, -0.25f), S(Pos.CDM, 0.3f, 0.25f),
            S(Pos.LW, 0.52f, -0.72f), S(Pos.CAM, 0.47f, 0f), S(Pos.RW, 0.52f, 0.72f),
            S(Pos.ST, 0.65f, 0f) } },

        new Formation { name = "3-5-2", slots = new[] {
            S(Pos.GK, 0.03f, 0f),
            S(Pos.CB, 0.17f, -0.5f), S(Pos.CB, 0.15f, 0f), S(Pos.CB, 0.17f, 0.5f),
            S(Pos.LM, 0.4f, -0.85f), S(Pos.CM, 0.35f, -0.33f), S(Pos.CDM, 0.29f, 0f), S(Pos.CM, 0.35f, 0.33f), S(Pos.RM, 0.4f, 0.85f),
            S(Pos.ST, 0.62f, -0.2f), S(Pos.ST, 0.62f, 0.2f) } },

        new Formation { name = "4-1-2-1-2", slots = new[] {
            S(Pos.GK, 0.03f, 0f),
            S(Pos.LB, 0.2f, -0.8f), S(Pos.CB, 0.17f, -0.3f), S(Pos.CB, 0.17f, 0.3f), S(Pos.RB, 0.2f, 0.8f),
            S(Pos.CDM, 0.29f, 0f), S(Pos.CM, 0.38f, -0.42f), S(Pos.CM, 0.38f, 0.42f),
            S(Pos.CAM, 0.49f, 0f),
            S(Pos.ST, 0.63f, -0.2f), S(Pos.ST, 0.63f, 0.2f) } },
    };

    static FormationSlot S(Pos p, float d, float s) { return new FormationSlot(p, d, s); }

    public static Formation Get(int index)
    {
        return All[Mathf.Clamp(index, 0, All.Length - 1)];
    }
}
