using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MboaCompetences.Core.Models;
using MboaCompetences.Core.Services;
using MboaCompetences.Services;

namespace MboaCompetences.ViewModels;

public partial class AjoutEleveViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly EtatApplication _etat;

    public AjoutEleveViewModel(DatabaseService db, EtatApplication etat)
    {
        _db = db;
        _etat = etat;
    }

    public IReadOnlyList<string> Niveaux { get; } = Core.Models.Niveaux.Noms;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeutEnregistrer))]
    public partial string Prenom { get; set; } = "";

    [ObservableProperty]
    public partial int NiveauIndex { get; set; } = 1;

    public bool PeutEnregistrer => !string.IsNullOrWhiteSpace(Prenom);

    [RelayCommand]
    private async Task EnregistrerAsync()
    {
        if (!PeutEnregistrer) return;
        var eleve = await _db.AjouterEleveAsync(Prenom, NiveauIndex);
        _etat.Choisir(eleve);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private Task AnnulerAsync() => Shell.Current.GoToAsync("..");
}
