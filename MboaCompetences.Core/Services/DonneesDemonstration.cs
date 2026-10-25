using MboaCompetences.Core.Models;

namespace MboaCompetences.Core.Services;

/// <summary>Crée un élève fictif avec cinq semaines d'historique, pour présenter le tableau de bord et le bilan PDF.</summary>
public static class DonneesDemonstration
{
    public const int NbJours = 35;

    // Probabilité de réussir au premier essai : (début de période, fin de période).
    private static readonly Dictionary<string, (double Debut, double Fin)> Profil = new()
    {
        ["MA-ADD"] = (.55, .97), ["MA-SOU"] = (.50, .95), ["MA-MON"] = (.20, .72), ["MA-MUL"] = (.35, .92),
        ["MA-DIV"] = (.10, .45), ["FR-CON"] = (.25, .90), ["FR-PLU"] = (.45, .92), ["FR-ACC"] = (.30, .80),
        ["FR-FUT"] = (.15, .70), ["EV-PAL"] = (.10, .92), ["EV-HYG"] = (.40, .95), ["EV-DEC"] = (.45, .95),
        ["EV-ARB"] = (.40, .88), ["EV-ROU"] = (.45, .90), ["EV-CIV"] = (.50, .95)
    };

    public static async Task<Eleve> CreerAsync(DatabaseService db, DateTime? aujourdhui = null, int graine = 2025)
    {
        var rng = new Random(graine);
        var jourJ = (aujourdhui ?? DateTime.Now).Date;
        var eleve = await db.AjouterEleveAsync("Aïcha", 4);
        var generateur = new GenerateurExercices(await db.DictionnaireAsync(), rng);

        var historique = new List<Tentative>();
        for (var jour = 0; jour < NbJours; jour++)
        {
            if (jour > 0 && rng.NextDouble() < 0.15) continue; // jours sans exercice

            var progres = jour / (double)(NbJours - 1);
            var date = jourJ.AddDays(jour - (NbJours - 1)).AddHours(7);
            var serie = PlanificateurSerie.Normale(eleve.Niveau, null, historique, rng);

            for (var i = 0; i < serie.Count; i++)
            {
                var competence = serie[i];
                var exercice = generateur.Generer(competence, eleve.Niveau);
                var (debut, fin) = Profil[competence.Code];
                var reussitPremier = rng.NextDouble() < debut + (fin - debut) * progres;
                var reussitFinal = reussitPremier || rng.NextDouble() < 0.65;
                var base_ = (62 - 32 * progres) * (exercice.EstQcm ? 0.7 : 1.0);
                var duree = base_ * (0.75 + rng.NextDouble() * 0.5) + (reussitPremier ? 0 : 12);

                var statutAvant = Analyse.Statut(historique.Where(t => t.CompetenceCode == competence.Code).ToList()).Statut;
                historique.Add(new Tentative
                {
                    EleveId = eleve.Id,
                    CompetenceCode = competence.Code,
                    Enonce = exercice.Enonce,
                    ReussiPremierEssai = reussitPremier,
                    ReussiFinalement = reussitFinal,
                    NbEssais = reussitPremier ? 1 : 2,
                    DureeSecondes = Math.Round(duree, 1),
                    Date = date.AddMinutes(i * 3),
                    EstRemediation = statutAvant == StatutCompetence.Rouge && rng.NextDouble() < 0.5
                });
            }
        }

        await db.EnregistrerTentativesAsync(historique);
        return eleve;
    }
}
