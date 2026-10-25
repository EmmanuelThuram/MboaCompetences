using MboaCompetences.Core.Models;

namespace MboaCompetences.Core.Services;

/// <summary>Choisit les compétences d'une série d'exercices selon l'état (feux) de l'élève.</summary>
public static class PlanificateurSerie
{
    public const int TailleSerie = 5;

    /// <summary>Série normale : on insiste sur les compétences rouges et orange, sans oublier de découvrir de nouvelles notions.</summary>
    public static List<Competence> Normale(int niveau, Matiere? matiere, IReadOnlyList<Tentative> historique, Random rng, int nb = TailleSerie)
    {
        var candidates = Analyse.Statuts(historique, niveau)
            .Where(s => s.Competence.NiveauMin <= niveau && (matiere is null || s.Competence.Matiere == matiere))
            .ToList();

        if (candidates.Count == 0) return [];

        var serie = new List<Competence>();
        for (var i = 0; i < nb; i++)
        {
            var possibles = candidates.Where(c => candidates.Count == 1 || serie.Count == 0 || c.Competence != serie[^1]).ToList();
            var total = possibles.Sum(c => Poids(c.Statut));
            var tirage = rng.NextDouble() * total;
            foreach (var c in possibles)
            {
                tirage -= Poids(c.Statut);
                if (tirage <= 0 || c == possibles[^1])
                {
                    serie.Add(c.Competence);
                    break;
                }
            }
        }
        return serie;
    }

    /// <summary>Module de remédiation : uniquement les compétences à retravailler (rouges d'abord, puis orange).</summary>
    public static List<Competence> Remediation(int niveau, IReadOnlyList<Tentative> historique, Random rng, int nb = TailleSerie)
    {
        var statuts = Analyse.Statuts(historique, niveau).Where(s => s.Competence.NiveauMin <= niveau).ToList();
        var cibles = statuts.Where(s => s.Statut == StatutCompetence.Rouge).Select(s => s.Competence).ToList();
        if (cibles.Count == 0)
            cibles = statuts.Where(s => s.Statut == StatutCompetence.Orange).Select(s => s.Competence).ToList();
        if (cibles.Count == 0) return [];

        var melange = cibles.OrderBy(_ => rng.Next()).ToList();
        return Enumerable.Range(0, nb).Select(i => melange[i % melange.Count]).ToList();
    }

    public static List<Competence> AretravaillerListe(int niveau, IReadOnlyList<Tentative> historique) =>
        Analyse.Statuts(historique, niveau)
            .Where(s => s.Competence.NiveauMin <= niveau && s.Statut == StatutCompetence.Rouge)
            .Select(s => s.Competence)
            .ToList();

    private static double Poids(StatutCompetence statut) => statut switch
    {
        StatutCompetence.Rouge => 4,
        StatutCompetence.Orange => 2,
        StatutCompetence.ADecouvrir => 3,
        _ => 1
    };
}
