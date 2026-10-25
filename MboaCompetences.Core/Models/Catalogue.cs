namespace MboaCompetences.Core.Models;

public enum Matiere
{
    Mathematiques,
    Francais,
    Eveil
}

/// <summary>Les domaines de la vie affichés sur le radar.</summary>
public enum Domaine
{
    VieEconomique,
    Langue,
    Sante,
    Environnement,
    Citoyennete
}

public record Competence(string Code, string Libelle, Matiere Matiere, Domaine Domaine, int NiveauMin);

public static class Catalogue
{
    public static readonly IReadOnlyList<Competence> Toutes =
    [
        new("MA-ADD", "Additionner des prix", Matiere.Mathematiques, Domaine.VieEconomique, 0),
        new("MA-SOU", "Calculer ce qui reste", Matiere.Mathematiques, Domaine.VieEconomique, 0),
        new("MA-MON", "Rendre la monnaie", Matiere.Mathematiques, Domaine.VieEconomique, 2),
        new("MA-MUL", "Multiplier (quantités)", Matiere.Mathematiques, Domaine.VieEconomique, 3),
        new("MA-DIV", "Partager (division)", Matiere.Mathematiques, Domaine.VieEconomique, 4),
        new("MA-REM", "Calculer une remise", Matiere.Mathematiques, Domaine.VieEconomique, 5),

        new("FR-CON", "Conjuguer au présent", Matiere.Francais, Domaine.Langue, 1),
        new("FR-PLU", "Pluriel des noms", Matiere.Francais, Domaine.Langue, 1),
        new("FR-ACC", "Accorder l'adjectif", Matiere.Francais, Domaine.Langue, 3),
        new("FR-FUT", "Conjuguer au futur", Matiere.Francais, Domaine.Langue, 4),

        new("EV-PAL", "Prévenir le paludisme", Matiere.Eveil, Domaine.Sante, 0),
        new("EV-HYG", "Hygiène et eau propre", Matiere.Eveil, Domaine.Sante, 0),
        new("EV-DEC", "Gérer les déchets", Matiere.Eveil, Domaine.Environnement, 1),
        new("EV-ARB", "Protéger la forêt", Matiere.Eveil, Domaine.Environnement, 2),
        new("EV-ROU", "Code de la route", Matiere.Eveil, Domaine.Citoyennete, 1),
        new("EV-CIV", "Vivre ensemble", Matiere.Eveil, Domaine.Citoyennete, 2),
    ];

    public static Competence Par(string code) =>
        Toutes.FirstOrDefault(c => c.Code == code) ?? throw new ArgumentException($"Compétence inconnue : {code}");

    public static IEnumerable<Competence> DisponiblesPour(int niveau, Matiere? matiere = null) =>
        Toutes.Where(c => c.NiveauMin <= niveau && (matiere is null || c.Matiere == matiere));

    public static string Nom(Matiere m) => m switch
    {
        Matiere.Mathematiques => "Mathématiques",
        Matiere.Francais => "Français",
        _ => "Éveil"
    };

    public static string Icone(Matiere m) => m switch
    {
        Matiere.Mathematiques => "🧮",
        Matiere.Francais => "📖",
        _ => "🌿"
    };

    public static string Nom(Domaine d) => d switch
    {
        Domaine.VieEconomique => "Vie économique",
        Domaine.Langue => "Langue",
        Domaine.Sante => "Santé",
        Domaine.Environnement => "Environnement",
        _ => "Citoyenneté"
    };
}
