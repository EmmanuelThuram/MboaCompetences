using MboaCompetences.Core.Models;
using MboaCompetences.Core.Services;

namespace MboaCompetences.Services;

public record ResultatSerie(Eleve Eleve, IReadOnlyList<Tentative> Tentatives, string Mode);

/// <summary>État partagé entre les écrans : élève actif et dernière série terminée.</summary>
public class EtatApplication
{
    private const string CleEleve = "eleve_actif";

    private readonly DatabaseService _db;

    public EtatApplication(DatabaseService db) => _db = db;

    public Eleve? EleveActif { get; private set; }

    public ResultatSerie? DerniereSerie { get; set; }

    public void Choisir(Eleve? eleve)
    {
        EleveActif = eleve;
        try { Preferences.Default.Set(CleEleve, eleve?.Id ?? 0); } catch { /* préférences indisponibles : sans conséquence */ }
    }

    /// <summary>Retrouve l'élève actif (après un redémarrage ou une suppression).</summary>
    public async Task<Eleve?> ActualiserAsync()
    {
        var id = EleveActif?.Id ?? SafeGet();
        EleveActif = id > 0 ? await _db.EleveAsync(id) : null;
        if (EleveActif is null)
        {
            var premier = (await _db.ElevesAsync()).FirstOrDefault();
            if (premier is not null) Choisir(premier);
        }
        return EleveActif;
    }

    private static int SafeGet()
    {
        try { return Preferences.Default.Get(CleEleve, 0); } catch { return 0; }
    }
}
