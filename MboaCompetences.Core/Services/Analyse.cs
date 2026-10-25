using MboaCompetences.Core.Models;

namespace MboaCompetences.Core.Services;

public record StatutInfo(Competence Competence, StatutCompetence Statut, int? Pourcent, int NbExercices);

/// <param name="Tag">Taux d'Acquisition Global (0 à 100) : réussis dès le premier essai.</param>
/// <param name="IvrSecondes">Indice de Vitesse de Résolution : temps moyen par exercice, en secondes.</param>
/// <param name="Tre">Taux de Réduction des Erreurs (0 à 100) : compétences passées de Rouge à Vert.</param>
public record Indicateurs(
    int NbExercices,
    double? Tag,
    double? IvrSecondes,
    double? Tre,
    int NbDejaRouge,
    int NbRedressees,
    double? ExosMoyensPourRedresser,
    int NbVertes,
    int NbOranges,
    int NbRouges);

public record PointProgression(string Libelle, double Valeur);

public record Comparatif(
    DateTime? Coupure,
    DateTime? Debut,
    DateTime? Fin,
    Indicateurs Avant,
    Indicateurs Apres,
    IReadOnlyList<StatutInfo> StatutsAvant,
    IReadOnlyList<StatutInfo> StatutsApres,
    IReadOnlyDictionary<Domaine, double> RadarAvant,
    IReadOnlyDictionary<Domaine, double> RadarApres)
{
    public bool ADeuxPeriodes => Coupure is not null;

    /// <summary>Évolution du TAG en points de pourcentage.</summary>
    public double? EvolutionTag => Avant.Tag is { } a && Apres.Tag is { } b ? b - a : null;

    /// <summary>Évolution du temps moyen en pourcentage (négatif = l'enfant va plus vite).</summary>
    public double? EvolutionIvrPourcent =>
        Avant.IvrSecondes is { } a && Apres.IvrSecondes is { } b && a > 0 ? (b - a) / a * 100 : null;
}

/// <summary>Module analytique : calcule les statuts (feux) et les trois KPI à partir de l'historique.</summary>
public static class Analyse
{
    /// <summary>Nombre d'exercices récents pris en compte pour juger une compétence.</summary>
    public const int Fenetre = 5;

    public static (StatutCompetence Statut, int? Pourcent) Statut(IReadOnlyList<Tentative> ordonnees)
    {
        if (ordonnees.Count == 0) return (StatutCompetence.ADecouvrir, null);

        var recentes = ordonnees.Skip(Math.Max(0, ordonnees.Count - Fenetre)).ToList();
        var taux = recentes.Count(t => t.ReussiPremierEssai) / (double)recentes.Count;
        var pourcent = (int)Math.Round(taux * 100);

        if (recentes.Count >= 3 && taux >= 0.75) return (StatutCompetence.Vert, pourcent);
        if (recentes.Count >= 2 && taux < 0.40) return (StatutCompetence.Rouge, pourcent);
        return (StatutCompetence.Orange, pourcent);
    }

    /// <summary>Statut de toutes les compétences accessibles au niveau de l'élève (ou déjà travaillées).</summary>
    public static List<StatutInfo> Statuts(IEnumerable<Tentative> tentatives, int niveau, DateTime? avant = null)
    {
        var parCompetence = tentatives
            .Where(t => avant is null || t.Date < avant)
            .GroupBy(t => t.CompetenceCode)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Date).ToList());

        return Catalogue.Toutes
            .Where(c => c.NiveauMin <= niveau || parCompetence.ContainsKey(c.Code))
            .Select(c =>
            {
                var liste = parCompetence.GetValueOrDefault(c.Code) ?? [];
                var (statut, pct) = Statut(liste);
                return new StatutInfo(c, statut, pct, liste.Count);
            })
            .ToList();
    }

    /// <summary>Calcule les KPI sur la période [debut, fin[ (bornes facultatives).</summary>
    public static Indicateurs Calculer(IReadOnlyList<Tentative> toutes, int niveau, DateTime? debut = null, DateTime? fin = null)
    {
        var periode = toutes
            .Where(t => (debut is null || t.Date >= debut) && (fin is null || t.Date < fin))
            .ToList();

        double? tag = periode.Count == 0 ? null : periode.Count(t => t.ReussiPremierEssai) * 100.0 / periode.Count;
        double? ivr = periode.Count == 0 ? null : periode.Average(t => t.DureeSecondes);

        var statuts = Statuts(toutes, niveau, fin);
        var (dejaRouge, redressees, exos) = RejouerTransitions(toutes.Where(t => fin is null || t.Date < fin));
        double? tre = dejaRouge == 0 ? null : redressees * 100.0 / dejaRouge;
        double? exosMoyens = exos.Count == 0 ? null : exos.Average();

        return new Indicateurs(
            periode.Count, tag, ivr, tre, dejaRouge, redressees, exosMoyens,
            statuts.Count(s => s.Statut == StatutCompetence.Vert),
            statuts.Count(s => s.Statut == StatutCompetence.Orange),
            statuts.Count(s => s.Statut == StatutCompetence.Rouge));
    }

    /// <summary>
    /// Rejoue l'historique pour mesurer la transition Rouge → Vert : combien de compétences sont déjà
    /// passées au Rouge, combien sont revenues au Vert, et en combien d'exercices de remédiation.
    /// </summary>
    private static (int DejaRouge, int Redressees, List<int> ExosPourRedresser) RejouerTransitions(IEnumerable<Tentative> tentatives)
    {
        var dejaRouge = 0;
        var redressees = 0;
        var exos = new List<int>();

        foreach (var groupe in tentatives.GroupBy(t => t.CompetenceCode))
        {
            var liste = groupe.OrderBy(t => t.Date).ToList();
            var ilEstRougeDepuis = -1;
            var comptee = false;
            var redressee = false;

            for (var i = 0; i < liste.Count; i++)
            {
                var (statut, _) = Statut(liste.Take(i + 1).ToList());
                if (statut == StatutCompetence.Rouge && ilEstRougeDepuis < 0)
                {
                    ilEstRougeDepuis = i;
                    if (!comptee) { dejaRouge++; comptee = true; }
                }
                else if (statut == StatutCompetence.Vert && ilEstRougeDepuis >= 0)
                {
                    if (!redressee) { redressees++; redressee = true; }
                    exos.Add(i - ilEstRougeDepuis);
                    ilEstRougeDepuis = -1;
                }
            }
        }

        return (dejaRouge, redressees, exos);
    }

    /// <summary>TAG par semaine (la dernière période d'au plus <paramref name="maxSemaines"/> semaines utilisées).</summary>
    public static List<PointProgression> ProgressionHebdomadaire(IReadOnlyList<Tentative> tentatives, int maxSemaines = 12)
    {
        if (tentatives.Count == 0) return [];

        var premier = tentatives.Min(t => t.Date).Date;
        return tentatives
            .GroupBy(t => (t.Date.Date - premier).Days / 7)
            .OrderBy(g => g.Key)
            .TakeLast(maxSemaines)
            .Select(g => new PointProgression($"S{g.Key + 1}", g.Count(t => t.ReussiPremierEssai) * 100.0 / g.Count()))
            .ToList();
    }

    /// <summary>Score (0 à 100) par domaine de la vie : taux de réussite au premier essai.</summary>
    public static Dictionary<Domaine, double> ScoresParDomaine(IEnumerable<Tentative> tentatives)
    {
        var liste = tentatives.ToList();
        var resultat = new Dictionary<Domaine, double>();
        foreach (var domaine in Enum.GetValues<Domaine>())
        {
            var concernes = liste.Where(t => Catalogue.Par(t.CompetenceCode).Domaine == domaine).ToList();
            resultat[domaine] = concernes.Count == 0 ? 0 : concernes.Count(t => t.ReussiPremierEssai) * 100.0 / concernes.Count;
        }
        return resultat;
    }

    /// <summary>Compare la première moitié et la seconde moitié de la période d'utilisation (Avant / Après).</summary>
    public static Comparatif Comparer(IReadOnlyList<Tentative> tentatives, int niveau)
    {
        if (tentatives.Count < 6)
        {
            var seul = Calculer(tentatives, niveau);
            return new Comparatif(null, tentatives.Count == 0 ? null : tentatives.Min(t => t.Date), tentatives.Count == 0 ? null : tentatives.Max(t => t.Date),
                Calculer([], niveau), seul, Statuts([], niveau), Statuts(tentatives, niveau),
                ScoresParDomaine([]), ScoresParDomaine(tentatives));
        }

        var debut = tentatives.Min(t => t.Date);
        var fin = tentatives.Max(t => t.Date);
        var coupure = debut + (fin - debut) / 2;

        var avant = tentatives.Where(t => t.Date < coupure).ToList();
        var apres = tentatives.Where(t => t.Date >= coupure).ToList();

        return new Comparatif(coupure, debut, fin,
            Calculer(tentatives, niveau, null, coupure),
            Calculer(tentatives, niveau),
            Statuts(tentatives, niveau, coupure),
            Statuts(tentatives, niveau),
            ScoresParDomaine(avant),
            ScoresParDomaine(apres));
    }

    /// <summary>Phrases simples à destination du parent, tirées des feux et de l'évolution.</summary>
    public static List<string> Conseils(Comparatif c, string prenom)
    {
        var conseils = new List<string>();
        var vertes = c.StatutsApres.Where(s => s.Statut == StatutCompetence.Vert).Select(s => s.Competence.Libelle.ToLower()).ToList();
        var rouges = c.StatutsApres.Where(s => s.Statut == StatutCompetence.Rouge).Select(s => s.Competence.Libelle.ToLower()).ToList();
        var oranges = c.StatutsApres.Where(s => s.Statut == StatutCompetence.Orange).Select(s => s.Competence.Libelle.ToLower()).ToList();

        if (c.EvolutionTag is { } tag)
            conseils.Add(tag >= 0
                ? $"{prenom} réussit davantage dès le premier essai : le TAG a progressé de {Math.Round(tag)} points."
                : $"Le TAG a baissé de {Math.Round(-tag)} points : proposez des séries plus courtes et plus régulières.");

        if (c.EvolutionIvrPourcent is { } ivr)
            conseils.Add(ivr <= 0
                ? $"{prenom} résout les exercices {Math.Round(-ivr)} % plus vite : la méthode s'installe."
                : $"{prenom} met {Math.Round(ivr)} % de temps en plus par exercice : laissez-le/la prendre le temps de bien lire l'énoncé.");

        if (vertes.Count > 0)
            conseils.Add($"Compétences acquises : {string.Join(", ", vertes.Take(4))}.");

        if (rouges.Count > 0)
            conseils.Add($"À retravailler en priorité avec le module de remédiation : {string.Join(", ", rouges.Take(3))}.");
        else if (oranges.Count > 0)
            conseils.Add($"Encore en cours d'acquisition : {string.Join(", ", oranges.Take(3))}. Quelques exercices par jour suffisent.");

        if (conseils.Count == 0)
            conseils.Add("Pas encore assez d'exercices pour tirer des conclusions : encouragez une petite série chaque jour.");

        return conseils;
    }
}
