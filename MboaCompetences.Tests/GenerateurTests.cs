using MboaCompetences.Core.Models;
using MboaCompetences.Core.Services;

namespace MboaCompetences.Tests;

public class GenerateurTests
{
    private static GenerateurExercices Nouveau(int graine) => new(DictionnaireLocal.PuisDeLaGraine(), new Random(graine));

    [Fact]
    public void Chaque_competence_produit_des_exercices_valides()
    {
        for (var graine = 0; graine < 200; graine++)
        {
            var generateur = Nouveau(graine);
            foreach (var competence in Catalogue.Toutes)
            foreach (var niveau in Enumerable.Range(competence.NiveauMin, 6 - competence.NiveauMin))
            {
                var ex = generateur.Generer(competence, niveau);

                Assert.False(string.IsNullOrWhiteSpace(ex.Enonce));
                Assert.DoesNotContain("{", ex.Enonce);
                Assert.False(string.IsNullOrWhiteSpace(ex.Indice));
                Assert.False(string.IsNullOrWhiteSpace(ex.Explication));
                Assert.DoesNotContain("{", ex.Explication);

                if (ex.EstQcm)
                {
                    Assert.Equal(3, ex.Choix.Count);
                    Assert.Equal(3, ex.Choix.Distinct().Count());
                    Assert.InRange(ex.IndexBonneReponse, 0, 2);
                }
                else
                {
                    Assert.True(ex.ReponseNombre > 0, $"{competence.Code} : réponse {ex.ReponseNombre} dans « {ex.Enonce} »");
                }
            }
        }
    }

    [Fact]
    public void Les_villes_commencant_par_une_voyelle_sont_elidees()
    {
        var generateur = Nouveau(5);
        foreach (var code in new[] { "MA-DIV", "FR-ACC", "EV-ARB", "EV-CIV" })
        for (var i = 0; i < 500; i++)
        {
            var enonce = generateur.Generer(Catalogue.Par(code), 5).Enonce;
            Assert.DoesNotMatch(@"\bde [AEIOUÉ]", enonce);
        }
    }

    [Fact]
    public void La_monnaie_rendue_est_toujours_positive_et_coherente()
    {
        var generateur = Nouveau(7);
        for (var i = 0; i < 300; i++)
        {
            var ex = generateur.Generer(Catalogue.Par("MA-MON"), 2);
            Assert.InRange(ex.ReponseNombre, 1, 9500);
        }
    }

    [Fact]
    public void La_division_tombe_juste()
    {
        var generateur = Nouveau(11);
        for (var i = 0; i < 200; i++)
        {
            var ex = generateur.Generer(Catalogue.Par("MA-DIV"), 4);
            Assert.Equal(0, ex.ReponseNombre % 50);
        }
    }

    [Fact]
    public void La_verification_accepte_les_espaces_et_la_monnaie()
    {
        var ex = new Exercice { CompetenceCode = "MA-MON", Enonce = "x", ReponseNombre = 1950 };
        Assert.True(ex.VerifierNombre("1950"));
        Assert.True(ex.VerifierNombre("1 950 FCFA"));
        Assert.False(ex.VerifierNombre("950"));
        Assert.False(ex.VerifierNombre(""));
        Assert.False(ex.VerifierNombre(null));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(950, "950")]
    [InlineData(1050, "1 050")]
    [InlineData(10000, "10 000")]
    [InlineData(1234567, "1 234 567")]
    public void Les_nombres_sont_groupes_par_trois(int n, string attendu) => Assert.Equal(attendu, Formats.Nombre(n));

    [Fact]
    public void Un_exemple_de_chaque_matiere_est_lisible()
    {
        var generateur = Nouveau(3);
        foreach (var code in new[] { "MA-MON", "FR-CON", "FR-ACC", "EV-PAL" })
        {
            var ex = generateur.Generer(Catalogue.Par(code), 4);
            Console.WriteLine($"[{code}] {ex.Enonce}  => {ex.ReponseAttendueTexte}");
        }
    }
}
