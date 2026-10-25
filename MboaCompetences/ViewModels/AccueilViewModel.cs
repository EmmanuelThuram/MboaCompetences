using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MboaCompetences.Core.Models;
using MboaCompetences.Core.Services;
using MboaCompetences.Services;

namespace MboaCompetences.ViewModels;

public partial class AccueilViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly EtatApplication _etat;

    public AccueilViewModel(DatabaseService db, EtatApplication etat)
    {
        _db = db;
        _etat = etat;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AucunEleve))]
    public partial bool ADesEleve { get; set; }

    public bool AucunEleve => !ADesEleve;

    [ObservableProperty]
    public partial string Salutation { get; set; } = "";

    [ObservableProperty]
    public partial string SousTitre { get; set; } = "";

    [ObservableProperty]
    public partial string Initiale { get; set; } = "";

    [ObservableProperty]
    public partial Color CouleurAvatar { get; set; } = Palette.Jaune;

    [ObservableProperty]
    public partial string Etoiles { get; set; } = "⭐ 0";

    [ObservableProperty]
    public partial bool ARetravailler { get; set; }

    [ObservableProperty]
    public partial string TexteARetravailler { get; set; } = "";

    public async Task ChargerAsync()
    {
        var eleve = await _etat.ActualiserAsync();
        ADesEleve = eleve is not null;
        if (eleve is null) return;

        var historique = await _db.TentativesAsync(eleve.Id);
        var aujourdhui = historique.Count(t => t.Date.Date == DateTime.Today);
        var rouges = PlanificateurSerie.AretravaillerListe(eleve.Niveau, historique);

        Initiale = eleve.Prenom[..1].ToUpper();
        CouleurAvatar = Palette.Avatars[eleve.CouleurAvatar % Palette.Avatars.Length];
        Salutation = $"Bonjour {eleve.Prenom} !";
        SousTitre = $"{eleve.NomNiveau} · aujourd'hui : {aujourdhui} exercice{(aujourdhui > 1 ? "s" : "")}";
        Etoiles = $"⭐ {historique.Count(t => t.ReussiPremierEssai)}";
        ARetravailler = rouges.Count > 0;
        TexteARetravailler = string.Join(" · ", rouges.Take(3).Select(c => c.Libelle));
    }

    [RelayCommand]
    private Task ChoisirMatiereAsync(string mode) => Shell.Current.GoToAsync($"exercice?mode={mode}");

    [RelayCommand]
    private Task ChoisirEleveAsync() => Shell.Current.GoToAsync("//eleves");
}
