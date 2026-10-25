using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MboaCompetences.Core.Models;
using MboaCompetences.Core.Services;
using MboaCompetences.Drawables;
using MboaCompetences.Services;

namespace MboaCompetences.ViewModels;

public class LigneCompetence
{
    public required string Libelle { get; init; }
    public required Color Feu { get; init; }
    public required string PourcentTexte { get; init; }
    public required Color PourcentCouleur { get; init; }
}

public class GroupeCompetences
{
    public required string Titre { get; init; }
    public required IReadOnlyList<LigneCompetence> Lignes { get; init; }
}

public class LigneBilan
{
    public required string Nom { get; init; }
    public required string Avant { get; init; }
    public required string Apres { get; init; }
    public required string Evolution { get; init; }
    public required Color CouleurEvolution { get; init; }
}

public partial class ParentViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly EtatApplication _etat;
    private readonly ExportPdfService _export;

    public ParentViewModel(DatabaseService db, EtatApplication etat, ExportPdfService export)
    {
        _db = db;
        _etat = etat;
        _export = export;
    }

    /// <summary>Demande au graphique de se redessiner (les GraphicsView ne détectent pas seuls les changements de données).</summary>
    public event Action? DessinsChanges;

    public CourbeDrawable Courbe { get; } = new();
    public RadarDrawable Radar { get; } = new();
    public ObservableCollection<GroupeCompetences> Groupes { get; } = [];
    public ObservableCollection<LigneBilan> LignesBilan { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AucunEleve))]
    public partial bool ADesEleve { get; set; }

    public bool AucunEleve => !ADesEleve;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Onglet0), nameof(Onglet1), nameof(Onglet2), nameof(Onglet3))]
    public partial int Onglet { get; set; }

    public bool Onglet0 => Onglet == 0;
    public bool Onglet1 => Onglet == 1;
    public bool Onglet2 => Onglet == 2;
    public bool Onglet3 => Onglet == 3;

    [ObservableProperty] public partial string Titre { get; set; } = "";
    [ObservableProperty] public partial string Periode { get; set; } = "";

    [ObservableProperty] public partial string TagTexte { get; set; } = "—";
    [ObservableProperty] public partial double TagBarre { get; set; }
    [ObservableProperty] public partial string TagTendance { get; set; } = "";
    [ObservableProperty] public partial Color TagCouleur { get; set; } = Palette.TexteGris;

    [ObservableProperty] public partial string IvrTexte { get; set; } = "—";
    [ObservableProperty] public partial string IvrTendance { get; set; } = "";
    [ObservableProperty] public partial Color IvrCouleur { get; set; } = Palette.TexteGris;

    [ObservableProperty] public partial string TreTexte { get; set; } = "—";
    [ObservableProperty] public partial string TreDetail { get; set; } = "";
    [ObservableProperty] public partial string TreRemediation { get; set; } = "";

    [ObservableProperty] public partial string NbExercices { get; set; } = "";
    [ObservableProperty] public partial string LegendeRadar { get; set; } = "";
    [ObservableProperty] public partial bool RadarAvantVisible { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeutExporter))]
    public partial bool ExportEnCours { get; set; }

    public bool PeutExporter => !ExportEnCours;
    [ObservableProperty] public partial string MessageExport { get; set; } = "";
    [ObservableProperty] public partial bool PdfPret { get; set; }

    private Eleve? _eleve;
    private List<Tentative> _historique = [];
    private string? _dernierPdf;

    public async Task ChargerAsync()
    {
        _eleve = await _etat.ActualiserAsync();
        ADesEleve = _eleve is not null;
        if (_eleve is null) return;

        _historique = await _db.TentativesAsync(_eleve.Id);
        var cmp = Analyse.Comparer(_historique, _eleve.Niveau);

        Titre = $"{_eleve.Prenom} · {_eleve.NomNiveau}";
        Periode = cmp.Debut is null
            ? "Aucun exercice pour l'instant"
            : $"Du {cmp.Debut:dd/MM/yyyy} au {cmp.Fin:dd/MM/yyyy}";
        NbExercices = $"{_historique.Count} exercice{(_historique.Count > 1 ? "s" : "")} réalisé{(_historique.Count > 1 ? "s" : "")}";

        RemplirCompetences(cmp);
        RemplirIndicateurs(cmp);
        RemplirGraphiques(cmp);
        RemplirBilan(cmp);
        DessinsChanges?.Invoke();
    }

    // ---------------------------------------------------------------- onglet Compétences

    private void RemplirCompetences(Comparatif cmp)
    {
        Groupes.Clear();
        foreach (var groupe in cmp.StatutsApres.GroupBy(s => s.Competence.Matiere).OrderBy(g => g.Key))
        {
            Groupes.Add(new GroupeCompetences
            {
                Titre = $"{Catalogue.Icone(groupe.Key)} {Catalogue.Nom(groupe.Key)}",
                Lignes = groupe.Select(s => new LigneCompetence
                {
                    Libelle = s.Competence.Libelle,
                    Feu = Palette.Feu(s.Statut),
                    PourcentTexte = s.Pourcent is { } p ? $"{p} %" : "à découvrir",
                    PourcentCouleur = Palette.FeuFonce(s.Statut)
                }).ToList()
            });
        }
    }

    // ---------------------------------------------------------------- onglet Indicateurs

    private void RemplirIndicateurs(Comparatif cmp)
    {
        var kpi = cmp.Apres;

        TagTexte = kpi.Tag is { } tag ? $"{Math.Round(tag)} %" : "—";
        TagBarre = (kpi.Tag ?? 0) / 100;
        if (cmp.EvolutionTag is { } e)
        {
            TagTendance = $"{(e >= 0 ? "▲ +" : "▼ −")}{Math.Abs(Math.Round(e))} points depuis le début";
            TagCouleur = e >= 0 ? Palette.FeuFonce(StatutCompetence.Vert) : Palette.Rouge;
        }
        else
        {
            TagTendance = "Pas encore assez d'exercices pour comparer";
            TagCouleur = Palette.TexteGris;
        }

        IvrTexte = kpi.IvrSecondes is { } ivr ? $"{Math.Round(ivr)} s" : "—";
        if (cmp.EvolutionIvrPourcent is { } v)
        {
            IvrTendance = v <= 0
                ? $"▼ −{Math.Abs(Math.Round(v))} % : l'enfant va plus vite"
                : $"▲ +{Math.Round(v)} % : l'enfant prend plus de temps";
            IvrCouleur = v <= 0 ? Palette.FeuFonce(StatutCompetence.Vert) : Palette.FeuFonce(StatutCompetence.Orange);
        }
        else
        {
            IvrTendance = "Mesuré avec le chronomètre de chaque exercice";
            IvrCouleur = Palette.TexteGris;
        }

        if (kpi.Tre is { } tre)
        {
            TreTexte = $"{Math.Round(tre)} %";
            TreDetail = $"{kpi.NbRedressees} compétence{(kpi.NbRedressees > 1 ? "s" : "")} sur {kpi.NbDejaRouge} passée{(kpi.NbDejaRouge > 1 ? "s" : "")} de Rouge à Vert";
            TreRemediation = kpi.ExosMoyensPourRedresser is { } m ? $"en {Math.Round(m)} exercices de remédiation en moyenne" : "";
        }
        else
        {
            TreTexte = "—";
            TreDetail = "Aucune compétence n'est encore passée au Rouge";
            TreRemediation = "";
        }
    }

    // ---------------------------------------------------------------- onglet Progrès

    private void RemplirGraphiques(Comparatif cmp)
    {
        Courbe.Points = Analyse.ProgressionHebdomadaire(_historique);
        Radar.Avant = cmp.ADeuxPeriodes ? cmp.RadarAvant : null;
        Radar.Apres = cmp.RadarApres;
        RadarAvantVisible = cmp.ADeuxPeriodes;
        LegendeRadar = cmp.ADeuxPeriodes
            ? "Vert : période récente · Tirets rouges : début de l'utilisation"
            : "Vert : résultats de l'enfant par domaine";
    }

    // ---------------------------------------------------------------- onglet Bilan

    private void RemplirBilan(Comparatif cmp)
    {
        var deux = cmp.ADeuxPeriodes;
        var bon = Palette.FeuFonce(StatutCompetence.Vert);

        string Pct(double? v) => v is { } x ? $"{Math.Round(x)} %" : "—";
        string Sec(double? v) => v is { } x ? $"{Math.Round(x)} s" : "—";

        LignesBilan.Clear();
        LignesBilan.Add(new LigneBilan
        {
            Nom = "TAG", Avant = deux ? Pct(cmp.Avant.Tag) : "—", Apres = Pct(cmp.Apres.Tag),
            Evolution = cmp.EvolutionTag is { } t ? $"{(t >= 0 ? "▲ +" : "▼ −")}{Math.Abs(Math.Round(t))}" : "—",
            CouleurEvolution = cmp.EvolutionTag is >= 0 ? bon : Palette.Rouge
        });
        LignesBilan.Add(new LigneBilan
        {
            Nom = "IVR", Avant = deux ? Sec(cmp.Avant.IvrSecondes) : "—", Apres = Sec(cmp.Apres.IvrSecondes),
            Evolution = cmp.EvolutionIvrPourcent is { } i ? $"{(i <= 0 ? "▼ −" : "▲ +")}{Math.Abs(Math.Round(i))} %" : "—",
            CouleurEvolution = cmp.EvolutionIvrPourcent is <= 0 ? bon : Palette.FeuFonce(StatutCompetence.Orange)
        });
        LignesBilan.Add(new LigneBilan
        {
            Nom = "TRE", Avant = deux ? Pct(cmp.Avant.Tre) : "—", Apres = Pct(cmp.Apres.Tre),
            Evolution = cmp.Apres.Tre is null ? "—" : "▲", CouleurEvolution = bon
        });
        var ecart = cmp.Apres.NbVertes - cmp.Avant.NbVertes;
        LignesBilan.Add(new LigneBilan
        {
            Nom = "Vertes", Avant = deux ? cmp.Avant.NbVertes.ToString() : "—", Apres = cmp.Apres.NbVertes.ToString(),
            Evolution = deux ? $"{(ecart >= 0 ? "+" : "−")}{Math.Abs(ecart)}" : "—", CouleurEvolution = ecart >= 0 ? bon : Palette.Rouge
        });
    }

    // ---------------------------------------------------------------- commandes

    [RelayCommand]
    private void ChoisirOnglet(string index) => Onglet = int.Parse(index);

    [RelayCommand]
    private Task ChoisirEleveAsync() => Shell.Current.GoToAsync("//eleves");

    [RelayCommand]
    private async Task ExporterAsync()
    {
        if (_eleve is null || ExportEnCours) return;

        ExportEnCours = true;
        MessageExport = "Création du PDF…";
        try
        {
            _dernierPdf = await _export.GenererAsync(_eleve, _historique);
            PdfPret = true;
            MessageExport = $"PDF enregistré : {Path.GetFileName(_dernierPdf)}";
            await _export.PartagerAsync(_dernierPdf);
        }
        catch (Exception ex)
        {
            MessageExport = _dernierPdf is null ? "Le PDF n'a pas pu être créé." : $"PDF enregistré dans : {_dernierPdf}";
            await Shell.Current.DisplayAlertAsync("Export du bilan", ex.Message, "OK");
        }
        finally
        {
            ExportEnCours = false;
        }
    }

    [RelayCommand]
    private async Task PartagerAsync()
    {
        if (_dernierPdf is null) return;
        try { await _export.PartagerAsync(_dernierPdf); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Partage", ex.Message, "OK"); }
    }
}
