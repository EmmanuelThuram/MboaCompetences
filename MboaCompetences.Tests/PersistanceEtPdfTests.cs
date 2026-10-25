using MboaCompetences.Core.Models;
using MboaCompetences.Core.Services;

namespace MboaCompetences.Tests;

public class PersistanceEtPdfTests : IDisposable
{
    private readonly string _dossier = Path.Combine(Path.GetTempPath(), "mboa_tests_" + Guid.NewGuid().ToString("N"));

    public PersistanceEtPdfTests() => Directory.CreateDirectory(_dossier);

    public void Dispose()
    {
        SQLite.SQLiteAsyncConnection.ResetPool();
        try { Directory.Delete(_dossier, true); } catch { /* fichier encore verrouillé : sans importance */ }
    }

    private DatabaseService NouvelleBase() => new(Path.Combine(_dossier, "test.db3"));

    [Fact]
    public async Task La_base_est_amorcee_avec_le_dictionnaire_local()
    {
        var dico = await NouvelleBase().DictionnaireAsync();
        Assert.True(dico.Marches.Count >= 8);
        Assert.True(dico.Articles.Count >= 10);
        Assert.Contains(dico.Marches, m => m.Valeur.Contains("Mokolo"));
        Assert.All(dico.Articles, a => Assert.True(a.PrixUnitaire > 0));
    }

    [Fact]
    public async Task Un_eleve_et_son_historique_sont_conserves_puis_supprimes()
    {
        var db = NouvelleBase();
        var eleve = await db.AjouterEleveAsync("  Kevin ", 3);
        await db.EnregistrerTentativeAsync(new Tentative { EleveId = eleve.Id, CompetenceCode = "MA-ADD", Enonce = "x", ReussiPremierEssai = true });

        Assert.Equal("Kevin", (await db.ElevesAsync()).Single().Prenom);
        Assert.Equal(1, await db.NombreTentativesAsync(eleve.Id));

        await db.SupprimerEleveAsync(eleve.Id);
        Assert.Empty(await db.ElevesAsync());
        Assert.Equal(0, await db.NombreTentativesAsync(eleve.Id));
    }

    [Fact]
    public async Task Les_donnees_de_demonstration_racontent_une_progression()
    {
        var db = NouvelleBase();
        var eleve = await DonneesDemonstration.CreerAsync(db);
        var tentatives = await db.TentativesAsync(eleve.Id);

        Assert.InRange(tentatives.Count, 100, 200);

        var cmp = Analyse.Comparer(tentatives, eleve.Niveau);
        Console.WriteLine($"TAG avant {cmp.Avant.Tag:0}% -> après {cmp.Apres.Tag:0}% ; IVR {cmp.Avant.IvrSecondes:0}s -> {cmp.Apres.IvrSecondes:0}s ; TRE {cmp.Apres.Tre:0}% ({cmp.Apres.NbRedressees}/{cmp.Apres.NbDejaRouge}) ; vertes {cmp.Avant.NbVertes} -> {cmp.Apres.NbVertes}");

        Assert.True(cmp.Apres.Tag > cmp.Avant.Tag + 10);
        Assert.True(cmp.Apres.IvrSecondes < cmp.Avant.IvrSecondes);
        Assert.True(cmp.Apres.NbVertes > cmp.Avant.NbVertes);
        Assert.NotNull(cmp.Apres.Tre);
    }

    [Fact]
    public async Task Le_bilan_PDF_est_genere_en_deux_pages()
    {
        var db = NouvelleBase();
        var eleve = await DonneesDemonstration.CreerAsync(db);
        var tentatives = await db.TentativesAsync(eleve.Id);

        var chemin = Path.Combine(_dossier, PdfBilanService.NomFichier(eleve, DateTime.Now));
        PdfBilanService.GenererFichier(eleve, tentatives, chemin);

        Assert.True(File.Exists(chemin));
        Assert.True(new FileInfo(chemin).Length > 5_000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(chemin), 0, 4));

        var images = PdfBilanService.GenererImages(eleve, tentatives);
        Assert.Equal(2, images.Count);

        var sortie = Environment.GetEnvironmentVariable("MBOA_APERCU");
        if (!string.IsNullOrEmpty(sortie))
        {
            Directory.CreateDirectory(sortie);
            for (var i = 0; i < images.Count; i++) File.WriteAllBytes(Path.Combine(sortie, $"bilan_p{i + 1}.png"), images[i]);
            File.Copy(chemin, Path.Combine(sortie, "bilan_exemple.pdf"), true);
        }
    }

    [Fact]
    public void Le_bilan_PDF_fonctionne_meme_sans_historique()
    {
        var eleve = new Eleve { Id = 1, Prenom = "Mireille", Niveau = 0 };
        var images = PdfBilanService.GenererImages(eleve, []);
        Assert.Equal(2, images.Count);
    }

    [Fact]
    public void Le_nom_du_fichier_est_sans_accent_ni_espace()
    {
        var nom = PdfBilanService.NomFichier(new Eleve { Prenom = "Aïcha Désirée", Niveau = 4 }, new DateTime(2026, 10, 23));
        Assert.Equal("Bilan_AichaDesiree_CM1_20261023.pdf", nom);
    }
}
