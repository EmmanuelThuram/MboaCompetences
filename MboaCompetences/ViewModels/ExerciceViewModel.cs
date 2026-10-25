using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MboaCompetences.Core.Models;
using MboaCompetences.Core.Services;
using MboaCompetences.Services;

namespace MboaCompetences.ViewModels;

public enum EtatChoix { Normal, Selectionne, Bon, Mauvais }

public partial class ChoixVm : ObservableObject
{
    public required int Index { get; init; }
    public required string Lettre { get; init; }
    public required string Texte { get; init; }
    public required ICommand Toucher { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Fond), nameof(Contour), nameof(Gras))]
    public partial EtatChoix Etat { get; set; }

    public Color Fond => Etat switch
    {
        EtatChoix.Selectionne => Palette.VertClair,
        EtatChoix.Bon => Palette.FeuVertClair,
        EtatChoix.Mauvais => Palette.RougeClair,
        _ => Palette.Blanc
    };

    public Color Contour => Etat switch
    {
        EtatChoix.Selectionne => Palette.Vert,
        EtatChoix.Bon => Palette.FeuVert,
        EtatChoix.Mauvais => Palette.Rouge,
        _ => Palette.Bord
    };

    public FontAttributes Gras => Etat is EtatChoix.Bon or EtatChoix.Selectionne ? FontAttributes.Bold : FontAttributes.None;
}

[QueryProperty(nameof(Mode), "mode")]
public partial class ExerciceViewModel : ObservableObject
{
    private enum Phase { Question, Correction, Chargement }

    private readonly DatabaseService _db;
    private readonly EtatApplication _etat;
    private readonly Stopwatch _chrono = new();
    private readonly List<Tentative> _realisees = [];
    private IDispatcherTimer? _minuteur;
    private List<Exercice> _serie = [];
    private Eleve? _eleve;
    private int _index;
    private int _essai;
    private Phase _phase = Phase.Chargement;
    private bool _demarre;

    public ExerciceViewModel(DatabaseService db, EtatApplication etat)
    {
        _db = db;
        _etat = etat;
    }

    /// <summary>Mathematiques, Francais, Eveil ou Remediation.</summary>
    public string Mode { get; set; } = "Mathematiques";

    public ObservableCollection<ChoixVm> Choix { get; } = [];

    [ObservableProperty] public partial string Titre { get; set; } = "";
    [ObservableProperty] public partial string Competence { get; set; } = "";
    [ObservableProperty] public partial string Compteur { get; set; } = "";
    [ObservableProperty] public partial double Progression { get; set; }
    [ObservableProperty] public partial string Enonce { get; set; } = "";
    [ObservableProperty] public partial bool EstQcm { get; set; }
    [ObservableProperty] public partial bool EstSaisie { get; set; }
    [ObservableProperty] public partial string Unite { get; set; } = "";
    [ObservableProperty] public partial string Chrono { get; set; } = "⏱ 00:00";
    [ObservableProperty] public partial string Indice { get; set; } = "";
    [ObservableProperty] public partial bool IndiceVisible { get; set; }
    [ObservableProperty] public partial string TitreRetour { get; set; } = "";
    [ObservableProperty] public partial string TexteRetour { get; set; } = "";
    [ObservableProperty] public partial bool RetourVisible { get; set; }
    [ObservableProperty] public partial Color FondRetour { get; set; } = Palette.FeuVertClair;
    [ObservableProperty] public partial Color CouleurRetour { get; set; } = Palette.FeuVert;
    [ObservableProperty] public partial string TexteBouton { get; set; } = "Valider ✓";
    [ObservableProperty] public partial bool BoutonActif { get; set; }
    [ObservableProperty] public partial bool SaisieActive { get; set; } = true;

    [ObservableProperty]
    public partial string Reponse { get; set; } = "";

    public bool ReponseValide => !string.IsNullOrWhiteSpace(Reponse);

    partial void OnReponseChanged(string value) => MettreAJourBouton();

    public bool EstRemediation => Mode == "Remediation";

    // ---------------------------------------------------------------- démarrage

    public async Task DemarrerAsync()
    {
        if (_demarre) return;
        _demarre = true;

        _eleve = await _etat.ActualiserAsync();
        if (_eleve is null)
        {
            await Shell.Current.GoToAsync("//eleves");
            return;
        }

        var historique = await _db.TentativesAsync(_eleve.Id);
        var rng = new Random();
        List<Competence> competences;
        if (EstRemediation)
        {
            competences = PlanificateurSerie.Remediation(_eleve.Niveau, historique, rng);
            if (competences.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Rien à retravailler", "Bravo : aucune compétence n'est en difficulté pour le moment.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }
            Titre = "🩹 Remédiation";
        }
        else
        {
            var matiere = Enum.Parse<Matiere>(Mode);
            competences = PlanificateurSerie.Normale(_eleve.Niveau, matiere, historique, rng);
            Titre = $"{Catalogue.Icone(matiere)} {Catalogue.Nom(matiere)}";
        }

        var generateur = new GenerateurExercices(await _db.DictionnaireAsync(), rng);
        _serie = competences.Select(c => generateur.Generer(c, _eleve.Niveau)).ToList();
        _index = 0;
        _realisees.Clear();

        _minuteur = Application.Current?.Dispatcher.CreateTimer();
        if (_minuteur is not null)
        {
            _minuteur.Interval = TimeSpan.FromSeconds(1);
            _minuteur.Tick += (_, _) => Chrono = FormaterChrono();
        }

        AfficherExercice();
    }

    [RelayCommand]
    private async Task QuitterAsync()
    {
        Arreter();
        await Shell.Current.GoToAsync("..");
    }

    public void Arreter()
    {
        _chrono.Stop();
        _minuteur?.Stop();
    }

    public void Reprendre()
    {
        if (_phase == Phase.Question)
        {
            _chrono.Start();
            _minuteur?.Start();
        }
    }

    // ---------------------------------------------------------------- affichage d'un exercice

    private string FormaterChrono() => $"⏱ {(int)_chrono.Elapsed.TotalMinutes:00}:{_chrono.Elapsed.Seconds:00}";

    private void AfficherExercice()
    {
        var ex = _serie[_index];
        _essai = 1;
        _phase = Phase.Question;

        Compteur = $"Exercice {_index + 1} sur {_serie.Count}";
        Progression = _index / (double)_serie.Count;
        Competence = $"{Catalogue.Nom(ex.Competence.Domaine)} · {ex.Competence.Libelle}";
        Enonce = ex.Enonce;
        EstQcm = ex.EstQcm;
        EstSaisie = !ex.EstQcm;
        Unite = ex.Unite;
        Indice = "💡 " + ex.Indice;
        IndiceVisible = EstRemediation;
        RetourVisible = false;
        SaisieActive = true;
        Reponse = "";

        Choix.Clear();
        for (var i = 0; i < ex.Choix.Count; i++)
        {
            var courant = i;
            Choix.Add(new ChoixVm
            {
                Index = i,
                Lettre = ((char)('A' + i)).ToString(),
                Texte = ex.Choix[i],
                Toucher = new RelayCommand(() => Selectionner(courant))
            });
        }

        TexteBouton = "Valider ✓";
        MettreAJourBouton();
        _chrono.Restart();
        _minuteur?.Start();
        Chrono = FormaterChrono();
    }

    private ChoixVm? ChoixSelectionne => Choix.FirstOrDefault(c => c.Etat == EtatChoix.Selectionne);

    private void Selectionner(int index)
    {
        if (_phase != Phase.Question) return;
        var cible = Choix[index];
        if (cible.Etat == EtatChoix.Mauvais) return;

        foreach (var c in Choix.Where(c => c.Etat == EtatChoix.Selectionne)) c.Etat = EtatChoix.Normal;
        cible.Etat = EtatChoix.Selectionne;
        MettreAJourBouton();
    }

    private void MettreAJourBouton()
    {
        if (_phase == Phase.Correction) { BoutonActif = true; return; }
        BoutonActif = _phase == Phase.Question && (EstQcm ? ChoixSelectionne is not null : ReponseValide);
    }

    // ---------------------------------------------------------------- validation

    [RelayCommand]
    private async Task ValiderAsync()
    {
        if (_phase == Phase.Correction)
        {
            await PasserALaSuiteAsync();
            return;
        }
        if (_phase != Phase.Question || !BoutonActif) return;

        var ex = _serie[_index];
        var correct = ex.EstQcm
            ? ex.VerifierChoix(ChoixSelectionne!.Index)
            : ex.VerifierNombre(Reponse);

        if (correct)
        {
            if (ex.EstQcm) ChoixSelectionne!.Etat = EtatChoix.Bon;
            await TerminerExerciceAsync(ex, reussi: true);
            AfficherCorrection(
                _essai == 1 ? "Bravo, c'est juste ! 🎉" : "Bien joué au 2e essai !",
                ex.Explication + (_essai == 1 ? "" : " Ce 2e essai ne compte pas pour le TAG, mais tu progresses."),
                true);
            return;
        }

        if (_essai == 1)
        {
            // Deuxième chance : l'indice apparaît, le chrono continue.
            _essai = 2;
            if (ex.EstQcm)
            {
                ChoixSelectionne!.Etat = EtatChoix.Mauvais;
                MettreAJourBouton();
            }
            else
            {
                Reponse = "";
            }
            IndiceVisible = true;
            TitreRetour = "Pas tout à fait… essaie encore !";
            TexteRetour = "Tu as droit à un 2e essai. Lis l'indice ci-dessus.";
            FondRetour = Palette.FeuOrangeClair;
            CouleurRetour = Palette.FeuOrange;
            RetourVisible = true;
            return;
        }

        // Deuxième échec : on montre la bonne réponse et l'explication.
        if (ex.EstQcm)
        {
            ChoixSelectionne!.Etat = EtatChoix.Mauvais;
            Choix[ex.IndexBonneReponse].Etat = EtatChoix.Bon;
        }
        await TerminerExerciceAsync(ex, reussi: false);
        AfficherCorrection($"La bonne réponse était : {ex.ReponseAttendueTexte}", ex.Explication, false);
    }

    private void AfficherCorrection(string titre, string texte, bool bravo)
    {
        _chrono.Stop();
        _minuteur?.Stop();
        _phase = Phase.Correction;
        TitreRetour = titre;
        TexteRetour = texte;
        FondRetour = bravo ? Palette.FeuVertClair : Palette.RougeClair;
        CouleurRetour = bravo ? Palette.FeuVert : Palette.Rouge;
        RetourVisible = true;
        SaisieActive = false;
        TexteBouton = _index == _serie.Count - 1 ? "Voir mon résultat 🏁" : "Question suivante ›";
        BoutonActif = true;
    }

    private async Task TerminerExerciceAsync(Exercice ex, bool reussi)
    {
        _chrono.Stop();
        var tentative = new Tentative
        {
            EleveId = _eleve!.Id,
            CompetenceCode = ex.CompetenceCode,
            Enonce = ex.Enonce,
            ReussiPremierEssai = reussi && _essai == 1,
            ReussiFinalement = reussi,
            NbEssais = _essai,
            DureeSecondes = Math.Round(_chrono.Elapsed.TotalSeconds, 1),
            Date = DateTime.Now,
            EstRemediation = EstRemediation
        };
        _realisees.Add(tentative);
        await _db.EnregistrerTentativeAsync(tentative);
    }

    private async Task PasserALaSuiteAsync()
    {
        if (_index < _serie.Count - 1)
        {
            _index++;
            AfficherExercice();
            return;
        }

        _phase = Phase.Chargement;
        Arreter();
        _etat.DerniereSerie = new ResultatSerie(_eleve!, _realisees.ToList(), Mode);
        await Shell.Current.GoToAsync("//accueil/resultat");
    }
}
