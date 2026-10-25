using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MboaCompetences.Core.Models;
using MboaCompetences.Core.Services;
using MboaCompetences.Services;

namespace MboaCompetences.ViewModels;

public class EleveItem
{
    public required Eleve Eleve { get; init; }
    public string Prenom => Eleve.Prenom;
    public string Initiale => Eleve.Prenom.Length > 0 ? Eleve.Prenom[..1].ToUpper() : "?";
    public required string Detail { get; init; }
    public Color Couleur => Palette.Avatars[Eleve.CouleurAvatar % Palette.Avatars.Length];
    public bool EstActif { get; init; }
    public required ICommand Choisir { get; init; }
    public required ICommand Supprimer { get; init; }
}

public partial class ElevesViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly EtatApplication _etat;

    public ElevesViewModel(DatabaseService db, EtatApplication etat)
    {
        _db = db;
        _etat = etat;
    }

    public ObservableCollection<EleveItem> Eleves { get; } = [];

    [ObservableProperty]
    public partial bool AucunEleve { get; set; }

    [ObservableProperty]
    public partial bool Occupe { get; set; }

    public async Task ChargerAsync()
    {
        var actif = await _etat.ActualiserAsync();
        var liste = await _db.ElevesAsync();

        Eleves.Clear();
        foreach (var eleve in liste)
        {
            var n = await _db.NombreTentativesAsync(eleve.Id);
            var courant = eleve;
            Eleves.Add(new EleveItem
            {
                Eleve = courant,
                Detail = $"{courant.NomNiveau} · {n} exercice{(n > 1 ? "s" : "")}",
                EstActif = actif?.Id == courant.Id,
                Choisir = new AsyncRelayCommand(() => ChoisirAsync(courant)),
                Supprimer = new AsyncRelayCommand(() => SupprimerAsync(courant))
            });
        }
        AucunEleve = Eleves.Count == 0;
    }

    private async Task ChoisirAsync(Eleve eleve)
    {
        _etat.Choisir(eleve);
        await Shell.Current.GoToAsync("//accueil");
    }

    private async Task SupprimerAsync(Eleve eleve)
    {
        var ok = await Shell.Current.DisplayAlertAsync("Supprimer ce profil ?",
            $"Le profil de {eleve.Prenom} et tout son historique d'exercices seront effacés du téléphone.", "Supprimer", "Annuler");
        if (!ok) return;

        await _db.SupprimerEleveAsync(eleve.Id);
        if (_etat.EleveActif?.Id == eleve.Id) _etat.Choisir(null);
        await ChargerAsync();
    }

    [RelayCommand]
    private Task AjouterAsync() => Shell.Current.GoToAsync("ajoutEleve");

    [RelayCommand]
    private async Task ChargerDemoAsync()
    {
        if (Occupe) return;
        Occupe = true;
        try
        {
            var eleve = await Task.Run(() => DonneesDemonstration.CreerAsync(_db));
            _etat.Choisir(eleve);
            await ChargerAsync();
            await Shell.Current.DisplayAlertAsync("Données de démonstration",
                $"{eleve.Prenom} (CM1) a maintenant cinq semaines d'historique. Ouvrez l'Espace parent pour voir son évolution.", "OK");
        }
        finally
        {
            Occupe = false;
        }
    }
}
