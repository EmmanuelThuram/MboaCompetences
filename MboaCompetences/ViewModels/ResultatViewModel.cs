using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MboaCompetences.Core.Models;
using MboaCompetences.Core.Services;
using MboaCompetences.Services;

namespace MboaCompetences.ViewModels;

public class CompetenceTravaillee
{
    public required string Libelle { get; init; }
    public required Color Feu { get; init; }
    public required string Statut { get; init; }
    public required Color StatutCouleur { get; init; }
    public required Color StatutFond { get; init; }
}

public partial class ResultatViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly EtatApplication _etat;
    private string _mode = "Mathematiques";

    public ResultatViewModel(DatabaseService db, EtatApplication etat)
    {
        _db = db;
        _etat = etat;
    }

    public ObservableCollection<CompetenceTravaillee> Competences { get; } = [];

    [ObservableProperty] public partial string Felicitations { get; set; } = "";
    [ObservableProperty] public partial string Etoiles { get; set; } = "";
    [ObservableProperty] public partial string Reussis { get; set; } = "";
    [ObservableProperty] public partial string TempsMoyen { get; set; } = "";

    public async Task ChargerAsync()
    {
        var serie = _etat.DerniereSerie;
        if (serie is null) return;

        _mode = serie.Mode;
        var n = serie.Tentatives.Count;
        var premiers = serie.Tentatives.Count(t => t.ReussiPremierEssai);

        Felicitations = premiers == n ? $"Parfait {serie.Eleve.Prenom} !" : premiers >= n / 2.0 ? $"Bravo {serie.Eleve.Prenom} !" : $"Courage {serie.Eleve.Prenom} !";
        Etoiles = string.Concat(Enumerable.Range(0, n).Select(i => i < premiers ? "⭐" : "☆"));
        Reussis = $"{premiers}/{n}";
        TempsMoyen = n == 0 ? "—" : $"{Math.Round(serie.Tentatives.Average(t => t.DureeSecondes))} s";

        var historique = await _db.TentativesAsync(serie.Eleve.Id);
        var statuts = Analyse.Statuts(historique, serie.Eleve.Niveau).ToDictionary(s => s.Competence.Code);

        Competences.Clear();
        foreach (var code in serie.Tentatives.Select(t => t.CompetenceCode).Distinct())
        {
            var s = statuts[code];
            Competences.Add(new CompetenceTravaillee
            {
                Libelle = s.Competence.Libelle,
                Feu = Palette.Feu(s.Statut),
                Statut = Palette.Texte_(s.Statut),
                StatutCouleur = Palette.FeuFonce(s.Statut),
                StatutFond = Palette.FeuClair(s.Statut)
            });
        }
    }

    [RelayCommand]
    private Task RejouerAsync() => Shell.Current.GoToAsync($"//accueil/exercice?mode={_mode}");

    [RelayCommand]
    private Task RetourAsync() => Shell.Current.GoToAsync("//accueil");
}
