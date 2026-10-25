using MboaCompetences.Core.Models;
using MboaCompetences.Core.Services;

namespace MboaCompetences.Tests;

public class AnalyseTests
{
    private static readonly DateTime Debut = new(2026, 9, 1, 8, 0, 0);

    private static Tentative T(string code, bool premier, int minutes, double duree = 40) => new()
    {
        CompetenceCode = code,
        ReussiPremierEssai = premier,
        ReussiFinalement = true,
        DureeSecondes = duree,
        Date = Debut.AddMinutes(minutes)
    };

    private static List<Tentative> Suite(string code, string resultats, int departMinutes = 0) =>
        resultats.Select((r, i) => T(code, r == 'o', departMinutes + i)).ToList();

    [Fact]
    public void Sans_exercice_la_competence_est_a_decouvrir()
    {
        var (statut, pct) = Analyse.Statut([]);
        Assert.Equal(StatutCompetence.ADecouvrir, statut);
        Assert.Null(pct);
    }

    [Theory]
    [InlineData("o", StatutCompetence.Orange)]      // un seul exercice : trop tôt pour conclure
    [InlineData("nn", StatutCompetence.Rouge)]
    [InlineData("on", StatutCompetence.Orange)]
    [InlineData("ooo", StatutCompetence.Vert)]
    [InlineData("oonoo", StatutCompetence.Vert)]    // 4 sur 5 = 80 %
    [InlineData("ononn", StatutCompetence.Orange)]  // 2 sur 5 = 40 % : le rouge commence sous 40 %
    [InlineData("nnnno", StatutCompetence.Rouge)]   // 1 sur 5 = 20 %
    public void Les_feux_suivent_les_cinq_derniers_exercices(string resultats, StatutCompetence attendu)
    {
        var (statut, _) = Analyse.Statut(Suite("MA-ADD", resultats));
        Assert.Equal(attendu, statut);
    }

    [Fact]
    public void Seuls_les_cinq_derniers_exercices_comptent()
    {
        var (statut, pct) = Analyse.Statut(Suite("MA-ADD", "nnnnnooooo"));
        Assert.Equal(StatutCompetence.Vert, statut);
        Assert.Equal(100, pct);
    }

    [Fact]
    public void Le_TAG_compte_les_reussites_au_premier_essai()
    {
        var liste = Suite("MA-ADD", "oonn");
        var kpi = Analyse.Calculer(liste, 4);
        Assert.Equal(50, kpi.Tag);
        Assert.Equal(4, kpi.NbExercices);
    }

    [Fact]
    public void Lindice_de_vitesse_est_le_temps_moyen_du_Stopwatch()
    {
        var liste = new List<Tentative> { T("MA-ADD", true, 0, 30), T("MA-ADD", true, 1, 50) };
        Assert.Equal(40, Analyse.Calculer(liste, 4).IvrSecondes);
    }

    [Fact]
    public void Le_TRE_mesure_les_competences_passees_de_rouge_a_vert()
    {
        // MA-ADD : rouge puis vert (redressée). MA-DIV : reste rouge.
        var liste = Suite("MA-ADD", "nnnooooo");
        liste.AddRange(Suite("MA-DIV", "nnnn", 100));

        var kpi = Analyse.Calculer(liste, 4);

        Assert.Equal(2, kpi.NbDejaRouge);
        Assert.Equal(1, kpi.NbRedressees);
        Assert.Equal(50, kpi.Tre);
        Assert.NotNull(kpi.ExosMoyensPourRedresser);
    }

    [Fact]
    public void Sans_passage_au_rouge_le_TRE_est_indetermine()
    {
        Assert.Null(Analyse.Calculer(Suite("MA-ADD", "oooo"), 4).Tre);
    }

    [Fact]
    public void Le_comparatif_coupe_la_periode_en_deux_et_montre_la_progression()
    {
        var liste = new List<Tentative>();
        for (var i = 0; i < 20; i++) liste.Add(T("MA-ADD", i % 4 == 0, i * 60, 60));   // 25 %, lent
        for (var i = 20; i < 40; i++) liste.Add(T("MA-ADD", i % 4 != 0, i * 60, 30));  // 75 %, rapide

        var cmp = Analyse.Comparer(liste, 4);

        Assert.True(cmp.ADeuxPeriodes);
        Assert.True(cmp.Apres.Tag > cmp.Avant.Tag);
        Assert.True(cmp.EvolutionTag > 0);
        Assert.True(cmp.EvolutionIvrPourcent < 0);
    }

    [Fact]
    public void Avec_peu_de_donnees_il_ny_a_quune_periode()
    {
        var cmp = Analyse.Comparer(Suite("MA-ADD", "oo"), 4);
        Assert.False(cmp.ADeuxPeriodes);
        Assert.Equal(0, cmp.Avant.NbExercices);
    }

    [Fact]
    public void Le_radar_donne_un_score_par_domaine()
    {
        var liste = Suite("MA-ADD", "oonn");
        liste.AddRange(Suite("EV-PAL", "oooo", 50));

        var scores = Analyse.ScoresParDomaine(liste);

        Assert.Equal(50, scores[Domaine.VieEconomique]);
        Assert.Equal(100, scores[Domaine.Sante]);
        Assert.Equal(0, scores[Domaine.Citoyennete]);
    }

    [Fact]
    public void La_progression_est_calculee_par_semaine()
    {
        var liste = new List<Tentative>
        {
            T("MA-ADD", false, 0), T("MA-ADD", true, 10),                 // semaine 1 : 50 %
            T("MA-ADD", true, 8 * 24 * 60), T("MA-ADD", true, 8 * 24 * 60 + 5) // semaine 2 : 100 %
        };
        var points = Analyse.ProgressionHebdomadaire(liste);
        Assert.Equal(["S1", "S2"], points.Select(p => p.Libelle));
        Assert.Equal([50.0, 100.0], points.Select(p => p.Valeur));
    }

    [Fact]
    public void La_serie_de_remediation_ne_contient_que_des_competences_rouges()
    {
        var liste = Suite("MA-DIV", "nnn");
        liste.AddRange(Suite("MA-ADD", "oooo", 10));

        var serie = PlanificateurSerie.Remediation(4, liste, new Random(1));

        Assert.Equal(PlanificateurSerie.TailleSerie, serie.Count);
        Assert.All(serie, c => Assert.Equal("MA-DIV", c.Code));
    }

    [Fact]
    public void La_serie_normale_respecte_la_matiere_et_le_niveau()
    {
        var serie = PlanificateurSerie.Normale(1, Matiere.Mathematiques, [], new Random(2));
        Assert.All(serie, c =>
        {
            Assert.Equal(Matiere.Mathematiques, c.Matiere);
            Assert.True(c.NiveauMin <= 1);
        });
    }
}
