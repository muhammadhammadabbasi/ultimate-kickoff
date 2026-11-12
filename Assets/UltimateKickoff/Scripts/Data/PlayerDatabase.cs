using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All player cards in the game. Names are deliberately altered (no real licences).
/// Fields: name, position, nation, OVR, PAC, SHO, PAS, DRI, DEF, PHY, club.
/// Goalkeepers: name, nation, OVR, DIV, HAN, KIC, REF, SPD, POS, club.
/// </summary>
public static class PlayerDatabase
{
    static List<PlayerData> players;
    static Dictionary<int, PlayerData> byId;
    static List<ClubData> clubs;

    public static List<PlayerData> All { get { Init(); return players; } }
    public static List<ClubData> Clubs { get { Init(); return clubs; } }

    public static PlayerData Get(int id)
    {
        Init();
        PlayerData p;
        return byId.TryGetValue(id, out p) ? p : null;
    }

    public static List<PlayerData> ClubPlayers(string abbr)
    {
        Init();
        var list = new List<PlayerData>();
        foreach (var p in players) if (p.club == abbr) list.Add(p);
        return list;
    }

    static void Init()
    {
        if (players != null) return;
        players = new List<PlayerData>();
        byId = new Dictionary<int, PlayerData>();
        clubs = new List<ClubData>();

        Club("Madrid Kings", "MDK", C(0.95f, 0.95f, 0.95f), C(0.18f, 0.18f, 0.24f), C(0.95f, 0.95f, 0.95f), 0);
        G("T. Courtua", "BEL", 89, 85, 89, 76, 90, 46, 89, "MDK");
        P("K. Mbape", Pos.ST, "FRA", 91, 97, 90, 80, 92, 36, 77, "MDK");
        P("V. Junio", Pos.LW, "BRA", 90, 95, 84, 81, 92, 29, 69, "MDK");
        P("J. Bellinghan", Pos.CAM, "ENG", 90, 80, 85, 84, 89, 78, 83, "MDK");
        P("Rodrygho", Pos.RW, "BRA", 86, 89, 82, 80, 88, 32, 62, "MDK");
        P("F. Valverdi", Pos.CM, "URU", 89, 88, 84, 85, 85, 79, 86, "MDK");
        P("A. Tchuameny", Pos.CDM, "FRA", 85, 72, 72, 80, 79, 84, 86, "MDK");
        P("E. Kamavinga", Pos.CM, "FRA", 84, 82, 70, 80, 85, 81, 81, "MDK");
        P("L. Modrich", Pos.CM, "CRO", 86, 68, 76, 89, 87, 72, 70, "MDK");
        P("A. Rudigar", Pos.CB, "GER", 87, 82, 52, 72, 70, 87, 86, "MDK");
        P("E. Militau", Pos.CB, "BRA", 86, 85, 50, 70, 72, 86, 84, "MDK");
        P("D. Carvajall", Pos.RB, "ESP", 84, 80, 60, 78, 80, 82, 80, "MDK");
        P("F. Mendi", Pos.LB, "FRA", 83, 87, 50, 72, 80, 80, 80, "MDK");
        P("A. Gulery", Pos.RW, "TUR", 80, 75, 80, 82, 85, 40, 58, "MDK");

        Club("Catalonia FC", "CAT", C(0.55f, 0.08f, 0.22f), C(0.95f, 0.8f, 0.2f), C(0.1f, 0.12f, 0.35f), 0);
        G("M. Ter Stegan", "GER", 88, 86, 85, 88, 90, 50, 86, "CAT");
        P("R. Lewandovski", Pos.ST, "POL", 89, 76, 91, 79, 86, 44, 82, "CAT");
        P("L. Yamaal", Pos.RW, "ESP", 89, 88, 82, 86, 92, 35, 62, "CAT");
        P("Raphinio", Pos.LW, "BRA", 87, 88, 85, 83, 87, 48, 72, "CAT");
        P("Pedry", Pos.CM, "ESP", 88, 78, 73, 89, 91, 66, 68, "CAT");
        P("Gavvi", Pos.CM, "ESP", 83, 80, 70, 82, 86, 72, 74, "CAT");
        P("F. De Jongg", Pos.CDM, "NED", 86, 80, 70, 87, 87, 78, 78, "CAT");
        P("D. Olmoo", Pos.CAM, "ESP", 85, 78, 82, 85, 87, 55, 68, "CAT");
        P("R. Araujjo", Pos.CB, "URU", 85, 80, 50, 62, 65, 86, 88, "CAT");
        P("P. Cubarsy", Pos.CB, "ESP", 83, 72, 40, 78, 73, 84, 78, "CAT");
        P("J. Kounday", Pos.RB, "FRA", 85, 82, 55, 75, 77, 84, 82, "CAT");
        P("A. Baldy", Pos.LB, "ESP", 82, 90, 58, 75, 82, 77, 72, "CAT");
        P("F. Torrez", Pos.LW, "ESP", 80, 82, 80, 77, 82, 35, 65, "CAT");

        Club("Manchester Sky", "MSK", C(0.45f, 0.72f, 0.95f), C(0.15f, 0.15f, 0.2f), C(0.95f, 0.95f, 0.95f), 2);
        G("Edersen", "BRA", 87, 84, 82, 93, 86, 62, 85, "MSK");
        P("E. Haland", Pos.ST, "NOR", 91, 89, 94, 66, 80, 45, 88, "MSK");
        P("K. De Bruin", Pos.CM, "BEL", 89, 72, 87, 93, 86, 64, 75, "MSK");
        P("P. Fodden", Pos.CAM, "ENG", 87, 82, 84, 85, 90, 57, 62, "MSK");
        P("Rodry", Pos.CDM, "ESP", 91, 66, 80, 86, 84, 87, 85, "MSK");
        P("B. Silvo", Pos.CM, "POR", 87, 74, 78, 87, 90, 68, 68, "MSK");
        P("R. Diaz", Pos.CB, "POR", 88, 64, 40, 70, 69, 89, 87, "MSK");
        P("J. Gvardiyol", Pos.LB, "CRO", 86, 80, 60, 74, 76, 85, 82, "MSK");
        P("M. Akanjy", Pos.CB, "SUI", 85, 72, 45, 76, 72, 85, 80, "MSK");
        P("K. Walkar", Pos.RB, "ENG", 83, 88, 62, 74, 76, 80, 80, "MSK");
        P("J. Dokku", Pos.LW, "BEL", 83, 94, 72, 74, 89, 30, 63, "MSK");
        P("O. Marmush", Pos.ST, "EGY", 85, 86, 84, 80, 85, 40, 72, "MSK");

        Club("Merseyside Reds", "MRR", C(0.78f, 0.08f, 0.12f), C(0.95f, 0.95f, 0.95f), C(0.78f, 0.08f, 0.12f), 0);
        G("Alison", "BRA", 89, 87, 86, 85, 89, 55, 90, "MRR");
        P("M. Salha", Pos.RW, "EGY", 90, 89, 88, 84, 88, 45, 76, "MRR");
        P("V. Van Dyke", Pos.CB, "NED", 89, 76, 60, 72, 72, 90, 87, "MRR");
        P("F. Virtz", Pos.CAM, "GER", 89, 82, 84, 87, 91, 52, 66, "MRR");
        P("A. Mac Alistar", Pos.CM, "ARG", 87, 70, 80, 86, 85, 78, 77, "MRR");
        P("R. Gravenburg", Pos.CDM, "NED", 85, 78, 70, 80, 85, 80, 82, "MRR");
        P("D. Szobosley", Pos.CM, "HUN", 84, 80, 82, 85, 83, 66, 80, "MRR");
        P("L. Dias", Pos.LW, "COL", 85, 90, 80, 78, 87, 45, 72, "MRR");
        P("C. Gakpoe", Pos.LW, "NED", 84, 83, 82, 80, 84, 45, 78, "MRR");
        P("D. Nunyez", Pos.ST, "URU", 82, 90, 82, 68, 78, 42, 84, "MRR");
        P("I. Konatey", Pos.CB, "FRA", 84, 82, 40, 62, 65, 85, 88, "MRR");
        P("A. Robertsen", Pos.LB, "SCO", 84, 80, 60, 82, 78, 80, 76, "MRR");
        P("J. Frimpon", Pos.RB, "NED", 84, 93, 70, 78, 84, 70, 68, "MRR");

        Club("London Gunners", "LDG", C(0.86f, 0.12f, 0.12f), C(0.95f, 0.85f, 0.3f), C(0.95f, 0.95f, 0.95f), 2);
        G("D. Raia", "ESP", 86, 84, 82, 88, 87, 58, 84, "LDG");
        P("B. Sakka", Pos.RW, "ENG", 89, 87, 85, 84, 89, 58, 70, "LDG");
        P("M. Odegard", Pos.CAM, "NOR", 88, 74, 82, 90, 89, 60, 64, "LDG");
        P("D. Ryce", Pos.CDM, "ENG", 88, 76, 75, 84, 82, 85, 84, "LDG");
        P("W. Salibo", Pos.CB, "FRA", 88, 82, 40, 72, 72, 88, 86, "LDG");
        P("Gabryel", Pos.CB, "BRA", 86, 70, 50, 62, 65, 87, 88, "LDG");
        P("V. Gyokerez", Pos.ST, "SWE", 87, 88, 88, 72, 84, 42, 88, "LDG");
        P("K. Havertsz", Pos.ST, "GER", 84, 80, 82, 80, 84, 55, 80, "LDG");
        P("G. Martinelly", Pos.LW, "BRA", 84, 90, 80, 77, 85, 45, 70, "LDG");
        P("M. Merinho", Pos.CM, "ESP", 84, 66, 78, 82, 80, 78, 82, "LDG");
        P("B. Wyte", Pos.RB, "ENG", 84, 82, 58, 75, 76, 82, 78, "LDG");
        P("R. Calafiory", Pos.LB, "ITA", 82, 76, 55, 76, 76, 80, 82, "LDG");
        P("L. Trosart", Pos.LW, "BEL", 83, 80, 82, 80, 85, 40, 64, "LDG");

        Club("Paris Saints", "PSA", C(0.08f, 0.14f, 0.38f), C(0.95f, 0.95f, 0.95f), C(0.08f, 0.14f, 0.38f), 0);
        G("G. Donaruma", "ITA", 89, 89, 82, 72, 90, 52, 85, "PSA");
        P("O. Dembelle", Pos.ST, "FRA", 90, 93, 86, 84, 91, 40, 64, "PSA");
        P("K. Kvaratskelia", Pos.LW, "GEO", 88, 85, 82, 82, 90, 40, 70, "PSA");
        P("Vitinia", Pos.CM, "POR", 88, 76, 78, 89, 89, 74, 72, "PSA");
        P("J. Nevez", Pos.CDM, "POR", 86, 74, 74, 85, 86, 78, 72, "PSA");
        P("F. Ruis", Pos.CM, "ESP", 84, 66, 78, 84, 83, 76, 78, "PSA");
        P("A. Hakimy", Pos.RB, "MAR", 87, 90, 75, 80, 84, 78, 80, "PSA");
        P("N. Mendez", Pos.LB, "POR", 86, 90, 60, 78, 84, 80, 76, "PSA");
        P("Marquinios", Pos.CB, "BRA", 86, 78, 50, 72, 74, 87, 80, "PSA");
        P("W. Pachoe", Pos.CB, "ECU", 84, 76, 40, 65, 66, 85, 85, "PSA");
        P("D. Douey", Pos.RW, "FRA", 84, 86, 80, 80, 88, 35, 64, "PSA");
        P("B. Barcolla", Pos.LW, "FRA", 82, 91, 78, 74, 85, 32, 62, "PSA");
        P("G. Ramoz", Pos.ST, "POR", 82, 80, 83, 70, 78, 40, 78, "PSA");

        Club("Munich Eagles", "MUE", C(0.86f, 0.05f, 0.18f), C(0.95f, 0.95f, 0.95f), C(0.95f, 0.95f, 0.95f), 2);
        G("M. Neuar", "GER", 86, 84, 84, 88, 86, 55, 88, "MUE");
        P("H. Kain", Pos.ST, "ENG", 91, 70, 93, 85, 83, 48, 82, "MUE");
        P("J. Musiaala", Pos.CAM, "GER", 89, 85, 82, 84, 93, 62, 62, "MUE");
        P("M. Olisse", Pos.RW, "FRA", 87, 85, 82, 85, 88, 40, 62, "MUE");
        P("J. Kimich", Pos.CDM, "GER", 87, 70, 72, 88, 84, 82, 74, "MUE");
        P("L. Goretska", Pos.CM, "GER", 82, 74, 78, 80, 80, 78, 85, "MUE");
        P("L. Sanne", Pos.LW, "GER", 84, 89, 82, 80, 87, 38, 70, "MUE");
        P("A. Davis", Pos.LB, "CAN", 84, 94, 65, 76, 84, 76, 78, "MUE");
        P("D. Upamekano", Pos.CB, "FRA", 84, 82, 45, 68, 70, 84, 86, "MUE");
        P("M. Kimm", Pos.CB, "KOR", 84, 80, 40, 66, 66, 86, 86, "MUE");
        P("J. Taah", Pos.CB, "GER", 84, 74, 40, 65, 66, 85, 86, "MUE");
        P("K. Laimar", Pos.RB, "AUT", 81, 82, 60, 75, 78, 76, 82, "MUE");
        P("S. Gnabri", Pos.RW, "GER", 80, 84, 80, 78, 83, 40, 70, "MUE");

        Club("Milano Stars", "MIL", C(0.05f, 0.22f, 0.62f), C(0.95f, 0.95f, 0.95f), C(0.08f, 0.08f, 0.1f), 3);
        G("Y. Sommar", "SUI", 85, 84, 82, 80, 87, 50, 82, "MIL");
        P("L. Martines", Pos.ST, "ARG", 89, 82, 88, 78, 88, 50, 84, "MIL");
        P("M. Turamm", Pos.ST, "FRA", 85, 88, 82, 74, 84, 40, 82, "MIL");
        P("N. Barela", Pos.CM, "ITA", 87, 78, 76, 87, 87, 79, 80, "MIL");
        P("H. Calhanoglo", Pos.CDM, "TUR", 86, 66, 82, 88, 84, 76, 74, "MIL");
        P("H. Mkitaryan", Pos.CM, "ARM", 82, 70, 78, 84, 84, 62, 64, "MIL");
        P("T. Rijnders", Pos.CM, "NED", 85, 76, 80, 84, 86, 72, 74, "MIL");
        P("A. Bastone", Pos.CB, "ITA", 87, 76, 48, 78, 74, 88, 84, "MIL");
        P("S. De Vrei", Pos.CB, "NED", 83, 60, 45, 70, 66, 85, 80, "MIL");
        P("F. Akerby", Pos.CB, "ITA", 82, 66, 45, 66, 64, 85, 82, "MIL");
        P("F. Dimarko", Pos.LM, "ITA", 85, 80, 72, 85, 82, 74, 72, "MIL");
        P("D. Dumfris", Pos.RM, "NED", 84, 88, 72, 74, 78, 78, 85, "MIL");
        P("R. Leau", Pos.LW, "POR", 86, 92, 80, 76, 88, 30, 74, "MIL");
        P("C. Pulisich", Pos.RW, "USA", 84, 85, 80, 80, 86, 40, 62, "MIL");

        Club("Turin Zebras", "TUZ", C(0.12f, 0.12f, 0.12f), C(0.95f, 0.95f, 0.95f), C(0.95f, 0.95f, 0.95f), 1);
        G("M. Di Gregori", "ITA", 82, 82, 78, 72, 84, 48, 80, "TUZ");
        P("D. Vlahovich", Pos.ST, "SRB", 84, 78, 86, 66, 78, 40, 84, "TUZ");
        P("J. Davidd", Pos.ST, "CAN", 83, 84, 84, 74, 82, 38, 72, "TUZ");
        P("K. Yildis", Pos.LM, "TUR", 84, 85, 80, 80, 88, 35, 60, "TUZ");
        P("F. Conseicao", Pos.RM, "POR", 82, 88, 76, 78, 87, 35, 58, "TUZ");
        P("T. Koopmeinars", Pos.CM, "NED", 82, 66, 80, 83, 80, 74, 78, "TUZ");
        P("M. Locatelly", Pos.CM, "ITA", 82, 62, 70, 82, 78, 80, 78, "TUZ");
        P("W. McKenny", Pos.CM, "USA", 79, 76, 72, 74, 76, 74, 82, "TUZ");
        P("Bremar", Pos.CB, "BRA", 86, 80, 40, 62, 66, 87, 88, "TUZ");
        P("F. Gatty", Pos.CB, "ITA", 81, 74, 45, 60, 62, 82, 85, "TUZ");
        P("A. Cambiazo", Pos.LB, "ITA", 82, 82, 62, 78, 82, 78, 72, "TUZ");
        P("P. Kalulo", Pos.RB, "FRA", 80, 84, 45, 68, 72, 80, 78, "TUZ");
        P("N. Gonsalez", Pos.RW, "ARG", 81, 80, 80, 76, 82, 50, 74, "TUZ");

        Club("World Legends", "WLD", C(0.86f, 0.68f, 0.25f), C(0.1f, 0.1f, 0.12f), C(0.95f, 0.95f, 0.95f), 0);
        G("G. Bufon", "ITA", 93, 91, 90, 82, 94, 58, 93, "WLD", true);
        G("P. Chech", "CZE", 91, 89, 90, 78, 91, 52, 92, "WLD", true);
        P("R. Nazarro", Pos.ST, "BRA", 96, 96, 95, 80, 95, 40, 82, "WLD", true);
        P("Z. Zidano", Pos.CAM, "FRA", 96, 80, 90, 94, 96, 60, 84, "WLD", true);
        P("J. Kruiff", Pos.ST, "NED", 95, 90, 92, 90, 94, 42, 74, "WLD", true);
        P("Ronaldinio", Pos.LW, "BRA", 95, 90, 90, 90, 97, 40, 74, "WLD", true);
        P("T. Henri", Pos.ST, "FRA", 94, 94, 92, 84, 92, 40, 80, "WLD", true);
        P("P. Maldoni", Pos.LB, "ITA", 94, 85, 55, 78, 76, 95, 85, "WLD", true);
        P("F. Baresio", Pos.CB, "ITA", 94, 76, 55, 80, 76, 96, 86, "WLD", true);
        P("L. Mesi", Pos.RW, "ARG", 94, 85, 92, 92, 96, 35, 65, "WLD", true);
        P("C. Ronaldu", Pos.ST, "POR", 93, 88, 94, 82, 88, 35, 85, "WLD", true);
        P("A. Pirelo", Pos.CM, "ITA", 93, 66, 82, 96, 92, 68, 68, "WLD", true);
        P("Xavio", Pos.CM, "ESP", 93, 70, 76, 96, 92, 70, 66, "WLD", true);
        P("A. Iniestra", Pos.CM, "ESP", 93, 78, 80, 93, 95, 62, 64, "WLD", true);
        P("Kafu", Pos.RB, "BRA", 92, 92, 70, 84, 86, 88, 82, "WLD", true);
        P("R. Carlas", Pos.LB, "BRA", 92, 92, 82, 84, 86, 84, 84, "WLD", true);
        P("F. Canavaro", Pos.CB, "ITA", 92, 82, 50, 72, 74, 94, 86, "WLD", true);
        P("A. Nesto", Pos.CB, "ITA", 92, 80, 50, 74, 76, 94, 84, "WLD", true);
        P("Kakka", Pos.CAM, "BRA", 92, 90, 88, 88, 92, 45, 78, "WLD", true);
        P("P. Vierra", Pos.CDM, "FRA", 91, 82, 75, 85, 84, 90, 90, "WLD", true);
        P("F. Rijkart", Pos.CDM, "NED", 91, 78, 76, 86, 84, 90, 88, "WLD", true);
        P("S. Gerard", Pos.CM, "ENG", 91, 80, 88, 89, 84, 78, 84, "WLD", true);
        P("F. Lampart", Pos.CM, "ENG", 91, 76, 89, 88, 84, 72, 80, "WLD", true);
        P("C. Puyal", Pos.CB, "ESP", 91, 80, 50, 70, 70, 93, 88, "WLD", true);
        P("P. Lahmer", Pos.RB, "GER", 91, 84, 66, 86, 86, 90, 72, "WLD", true);
        P("D. Drogbo", Pos.ST, "CIV", 91, 86, 91, 74, 84, 40, 92, "WLD", true);
        P("P. Nedvedd", Pos.CM, "CZE", 91, 86, 88, 86, 88, 66, 84, "WLD", true);
        P("D. Beckam", Pos.RM, "ENG", 90, 80, 86, 95, 84, 60, 76, "WLD", true);
        P("R. Ferdinandd", Pos.CB, "ENG", 90, 84, 50, 72, 72, 91, 86, "WLD", true);
        P("C. Makelelle", Pos.CDM, "FRA", 89, 76, 60, 82, 80, 90, 84, "WLD", true);
        P("Neimar Jr", Pos.LW, "BRA", 89, 88, 85, 86, 94, 35, 62, "WLD", true);

        // Free agents: not in a club, but you can sign them into your XI
        G("J. Oblack", "SVN", 87, 86, 88, 76, 89, 50, 88, "");
        G("M. Maignon", "FRA", 87, 86, 84, 84, 89, 52, 84, "");
        P("C. Palmar", Pos.CAM, "ENG", 88, 78, 86, 87, 88, 55, 70, "");
        P("A. Isaak", Pos.ST, "SWE", 88, 86, 89, 76, 86, 40, 76, "");
        P("V. Osimen", Pos.ST, "NGA", 87, 89, 88, 66, 80, 42, 86, "");
        P("J. Alvares", Pos.ST, "ARG", 87, 84, 86, 80, 86, 55, 78, "");
        P("A. Grizmann", Pos.CAM, "FRA", 86, 76, 86, 86, 86, 60, 72, "");
        P("H. Sonn", Pos.LW, "KOR", 86, 88, 87, 80, 85, 40, 68, "");
        P("B. Fernandez", Pos.CAM, "POR", 87, 72, 85, 90, 84, 66, 76, "");
        P("N. Wiliams", Pos.LW, "ESP", 87, 92, 80, 80, 89, 35, 62, "");
        P("M. Caisedo", Pos.CDM, "ECU", 86, 74, 66, 80, 80, 86, 84, "");
        P("E. Fernandes", Pos.CM, "ARG", 84, 72, 78, 85, 82, 76, 78, "");
        P("B. Guimares", Pos.CM, "BRA", 86, 70, 76, 86, 85, 80, 82, "");
        P("N. Kantte", Pos.CDM, "FRA", 84, 76, 64, 76, 80, 86, 80, "");
        P("Casemyro", Pos.CDM, "BRA", 82, 56, 72, 78, 70, 84, 86, "");
        P("T. Hernandes", Pos.LB, "FRA", 85, 92, 70, 76, 80, 78, 84, "");
        P("A. Grimalda", Pos.LB, "ESP", 86, 80, 74, 85, 84, 78, 70, "");
        P("X. Simmons", Pos.CAM, "NED", 84, 84, 80, 83, 87, 50, 62, "");
        P("S. Guirasy", Pos.ST, "GUI", 84, 82, 87, 66, 78, 38, 82, "");
        P("A. Lukman", Pos.LW, "NGA", 84, 88, 82, 76, 86, 35, 66, "");
        P("V. Bonifase", Pos.ST, "NGA", 82, 88, 82, 68, 80, 35, 80, "");
        P("T. Alexander-Arnald", Pos.RB, "ENG", 86, 76, 72, 91, 80, 78, 72, "");
    }

    static Color C(float r, float g, float b) { return new Color(r, g, b); }

    static void Club(string name, string abbr, Color kit, Color alt, Color shorts, int formation)
    {
        clubs.Add(new ClubData { name = name, abbr = abbr, kit = kit, kitAlt = alt, shorts = shorts, formation = formation });
    }

    static void P(string name, Pos pos, string nat, int ovr, int pac, int sho, int pas, int dri, int def, int phy, string club, bool legend = false)
    {
        var p = new PlayerData
        {
            id = players.Count + 1,
            name = name, pos = pos, nation = nat, ovr = ovr,
            pac = pac, sho = sho, pas = pas, dri = dri, def = def, phy = phy,
            club = club, legend = legend
        };
        players.Add(p);
        byId[p.id] = p;
    }

    static void G(string name, string nat, int ovr, int div, int han, int kic, int refl, int spd, int posi, string club, bool legend = false)
    {
        P(name, Pos.GK, nat, ovr, div, han, kic, refl, spd, posi, club, legend);
    }
}
