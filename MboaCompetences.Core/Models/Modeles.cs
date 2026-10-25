using SQLite;

namespace MboaCompetences.Core.Models;

public static class Niveaux
{
    public static readonly string[] Noms = ["SIL", "CP", "CE1", "CE2", "CM1", "CM2"];

    public static string Nom(int niveau) => Noms[Math.Clamp(niveau, 0, Noms.Length - 1)];
}

[Table("Eleve")]
public class Eleve
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Prenom { get; set; } = "";

    /// <summary>0 = SIL, 1 = CP, 2 = CE1, 3 = CE2, 4 = CM1, 5 = CM2.</summary>
    public int Niveau { get; set; }

    public DateTime DateCreation { get; set; } = DateTime.Now;

    public int CouleurAvatar { get; set; }

    [Ignore]
    public string NomNiveau => Niveaux.Nom(Niveau);
}

/// <summary>Un exercice réalisé par un élève (une ligne par situation-problème).</summary>
[Table("Tentative")]
public class Tentative
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int EleveId { get; set; }

    public string CompetenceCode { get; set; } = "";

    public string Enonce { get; set; } = "";

    public bool ReussiPremierEssai { get; set; }

    public bool ReussiFinalement { get; set; }

    public int NbEssais { get; set; } = 1;

    /// <summary>Temps entre l'affichage de l'énoncé et la validation finale (Stopwatch).</summary>
    public double DureeSecondes { get; set; }

    public DateTime Date { get; set; } = DateTime.Now;

    public bool EstRemediation { get; set; }
}

/// <summary>Entrée du dictionnaire des variables locales camerounaises.</summary>
[Table("VariableLocale")]
public class VariableLocale
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Marche, Article, Prenom, Ville.</summary>
    [Indexed]
    public string Categorie { get; set; } = "";

    public string Valeur { get; set; } = "";

    /// <summary>Marche : ville. Article : unité (kg, tas...). Prenom : F ou M.</summary>
    public string Detail { get; set; } = "";

    /// <summary>Article : formule du prix unitaire, par exemple « le kilo ».</summary>
    public string Unite { get; set; } = "";

    /// <summary>Article : prix en FCFA pour une unité.</summary>
    public int PrixUnitaire { get; set; }
}

public enum StatutCompetence
{
    ADecouvrir,
    Rouge,
    Orange,
    Vert
}
