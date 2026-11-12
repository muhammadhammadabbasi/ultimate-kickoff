using System.Collections.Generic;
using UnityEngine;

/// <summary>A team that takes the pitch: 11 players in formation order + kit.</summary>
public class TeamSheet
{
    public string name;
    public string abbr;
    public int formation;
    public PlayerData[] players = new PlayerData[11];
    public Color kit;
    public Color shorts;
    public Color keeperKit;

    public int Ovr { get { return Squad.TeamOvr(players, formation); } }
}

/// <summary>User's squad, profile and settings (saved with PlayerPrefs).</summary>
public static class Squad
{
    public static readonly string[] DifficultyNames = { "BEGINNER", "PRO", "LEGENDARY" };
    public static readonly int[] HalfMinutesOptions = { 2, 3, 5, 8 };

    public static readonly Color[] KitColors =
    {
        new Color(0.2f, 0.15f, 0.4f),   // royal purple
        new Color(0.08f, 0.1f, 0.22f),  // midnight
        new Color(0.1f, 0.45f, 0.25f),  // green
        new Color(0.95f, 0.45f, 0.08f), // orange
        new Color(0.1f, 0.6f, 0.75f),   // teal
    };
    public static readonly string[] KitNames = { "PURPLE", "MIDNIGHT", "GREEN", "ORANGE", "TEAL" };

    public static int[] lineup = new int[11];
    public static int formation;
    public static string teamName = "M.H FC";
    public static string abbr = "M.H";
    public static int kitIndex;
    public static int difficulty = 1;
    public static int halfIndex = 1;
    public static int wins, draws, losses, goalsFor, goalsAgainst;

    static bool loaded;

    public static void Load()
    {
        if (loaded) return;
        loaded = true;
        formation = Mathf.Clamp(PlayerPrefs.GetInt("uk_formation", 0), 0, Formations.All.Length - 1);
        kitIndex = Mathf.Clamp(PlayerPrefs.GetInt("uk_kit", 0), 0, KitColors.Length - 1);
        difficulty = Mathf.Clamp(PlayerPrefs.GetInt("uk_diff", 1), 0, 2);
        halfIndex = Mathf.Clamp(PlayerPrefs.GetInt("uk_half", 1), 0, HalfMinutesOptions.Length - 1);
        teamName = PlayerPrefs.GetString("uk_team", "M.H FC");
        abbr = PlayerPrefs.GetString("uk_abbr", "M.H");
        wins = PlayerPrefs.GetInt("uk_w", 0);
        draws = PlayerPrefs.GetInt("uk_d", 0);
        losses = PlayerPrefs.GetInt("uk_l", 0);
        goalsFor = PlayerPrefs.GetInt("uk_gf", 0);
        goalsAgainst = PlayerPrefs.GetInt("uk_ga", 0);

        string saved = PlayerPrefs.GetString("uk_lineup", "");
        bool ok = false;
        if (!string.IsNullOrEmpty(saved))
        {
            string[] parts = saved.Split(',');
            if (parts.Length == 11)
            {
                ok = true;
                for (int i = 0; i < 11; i++)
                {
                    int id;
                    if (!int.TryParse(parts[i], out id) || PlayerDatabase.Get(id) == null) { ok = false; break; }
                    lineup[i] = id;
                }
            }
        }
        if (!ok) AutoBuildUser();
    }

    public static void Save()
    {
        PlayerPrefs.SetInt("uk_formation", formation);
        PlayerPrefs.SetInt("uk_kit", kitIndex);
        PlayerPrefs.SetInt("uk_diff", difficulty);
        PlayerPrefs.SetInt("uk_half", halfIndex);
        PlayerPrefs.SetString("uk_team", teamName);
        PlayerPrefs.SetString("uk_abbr", abbr);
        PlayerPrefs.SetInt("uk_w", wins);
        PlayerPrefs.SetInt("uk_d", draws);
        PlayerPrefs.SetInt("uk_l", losses);
        PlayerPrefs.SetInt("uk_gf", goalsFor);
        PlayerPrefs.SetInt("uk_ga", goalsAgainst);
        PlayerPrefs.SetString("uk_lineup", string.Join(",", lineup));
        PlayerPrefs.Save();
    }

    public static PlayerData LineupPlayer(int slot) { return PlayerDatabase.Get(lineup[slot]); }

    public static PlayerData[] LineupPlayers()
    {
        var arr = new PlayerData[11];
        for (int i = 0; i < 11; i++) arr[i] = LineupPlayer(i);
        return arr;
    }

    public static int UserOvr() { return TeamOvr(LineupPlayers(), formation); }

    public static int TeamOvr(PlayerData[] players, int formationIndex)
    {
        var f = Formations.Get(formationIndex);
        float sum = 0f;
        int n = 0;
        for (int i = 0; i < 11 && i < players.Length; i++)
        {
            if (players[i] == null) continue;
            sum += players[i].RatingAt(f.slots[i].pos);
            n++;
        }
        return n == 0 ? 0 : Mathf.RoundToInt(sum / n);
    }

    public static int SlotOf(int playerId)
    {
        for (int i = 0; i < 11; i++) if (lineup[i] == playerId) return i;
        return -1;
    }

    /// <summary>Put a player into a slot. If he is already in the XI, the two swap.</summary>
    public static void Assign(int slot, int playerId)
    {
        int existing = SlotOf(playerId);
        if (existing >= 0) lineup[existing] = lineup[slot];
        lineup[slot] = playerId;
        Save();
    }

    public static void AutoBuildUser()
    {
        var best = AutoBuild(formation, PlayerDatabase.All);
        for (int i = 0; i < 11; i++) lineup[i] = best[i].id;
        Save();
    }

    public static PlayerData[] AutoBuild(int formationIndex, List<PlayerData> pool)
    {
        var f = Formations.Get(formationIndex);
        var result = new PlayerData[11];
        var used = new HashSet<int>();
        for (int i = 0; i < 11; i++)
        {
            Pos slot = f.slots[i].pos;
            PlayerData pick = null;
            int bestScore = int.MinValue;
            foreach (var p in pool)
            {
                if (used.Contains(p.id)) continue;
                int score = p.RatingAt(slot) * 10;
                if (p.pos == slot) score += 50;
                if (score > bestScore) { bestScore = score; pick = p; }
            }
            if (pick == null) pick = pool[0];
            result[i] = pick;
            used.Add(pick.id);
        }
        return result;
    }

    public static TeamSheet UserSheet()
    {
        Load();
        var t = new TeamSheet
        {
            name = teamName,
            abbr = abbr,
            formation = formation,
            players = LineupPlayers(),
            kit = KitColors[kitIndex],
            shorts = new Color(0.08f, 0.08f, 0.12f),
            keeperKit = new Color(0.15f, 0.8f, 0.55f),
        };
        return t;
    }

    public static TeamSheet ClubSheet(ClubData club)
    {
        var pool = PlayerDatabase.ClubPlayers(club.abbr);
        return new TeamSheet
        {
            name = club.name,
            abbr = club.abbr,
            formation = club.formation,
            players = AutoBuild(club.formation, pool),
            kit = club.kit,
            shorts = club.shorts,
            keeperKit = new Color(0.95f, 0.8f, 0.1f),
        };
    }

    public static void RecordResult(int ourGoals, int theirGoals)
    {
        goalsFor += ourGoals;
        goalsAgainst += theirGoals;
        if (ourGoals > theirGoals) wins++;
        else if (ourGoals == theirGoals) draws++;
        else losses++;
        Save();
    }

    public static float ColorDistance(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
    }
}
