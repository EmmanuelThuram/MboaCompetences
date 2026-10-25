namespace MboaCompetences.Core.Models;

/// <summary>Une situation-problème générée, prête à être affichée (non stockée en base).</summary>
public class Exercice
{
    public string CompetenceCode { get; init; } = "";

    public string Enonce { get; init; } = "";

    public bool EstQcm { get; init; }

    /// <summary>Réponse attendue quand l'élève saisit un nombre.</summary>
    public int ReponseNombre { get; init; }

    public string Unite { get; init; } = "";

    public IReadOnlyList<string> Choix { get; init; } = [];

    public int IndexBonneReponse { get; init; }

    public string Indice { get; init; } = "";

    public string Explication { get; init; } = "";

    public Competence Competence => Catalogue.Par(CompetenceCode);

    public bool VerifierNombre(string? saisie)
    {
        if (string.IsNullOrWhiteSpace(saisie)) return false;
        var propre = new string(saisie.Where(char.IsDigit).ToArray());
        return int.TryParse(propre, out var n) && n == ReponseNombre;
    }

    public bool VerifierChoix(int index) => index == IndexBonneReponse;

    public string ReponseAttendueTexte => EstQcm ? Choix[IndexBonneReponse] : $"{Formats.Nombre(ReponseNombre)} {Unite}".Trim();
}

public static class Formats
{
    /// <summary>Écrit un entier avec une espace tous les trois chiffres (sans dépendre de la culture du téléphone).</summary>
    public static string Nombre(int n)
    {
        var chiffres = Math.Abs(n).ToString();
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < chiffres.Length; i++)
        {
            if (i > 0 && (chiffres.Length - i) % 3 == 0) sb.Append(' ');
            sb.Append(chiffres[i]);
        }
        return (n < 0 ? "-" : "") + sb;
    }

    public static string Fcfa(int n) => $"{Nombre(n)} FCFA";
}
