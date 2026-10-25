using MboaCompetences.Core.Models;
using SQLite;

namespace MboaCompetences.Core.Services;

/// <summary>Accès à la base SQLite locale : aucune connexion internet n'est nécessaire.</summary>
public class DatabaseService
{
    private readonly string _chemin;
    private readonly SemaphoreSlim _verrou = new(1, 1);
    private SQLiteAsyncConnection? _db;

    public DatabaseService(string chemin) => _chemin = chemin;

    private async Task<SQLiteAsyncConnection> ConnexionAsync()
    {
        if (_db is not null) return _db;

        await _verrou.WaitAsync();
        try
        {
            if (_db is not null) return _db;

            var db = new SQLiteAsyncConnection(_chemin, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
            await db.CreateTableAsync<Eleve>();
            await db.CreateTableAsync<Tentative>();
            await db.CreateTableAsync<VariableLocale>();

            if (await db.Table<VariableLocale>().CountAsync() == 0)
                await db.InsertAllAsync(DonneesInitiales.Variables());

            _db = db;
            return db;
        }
        finally
        {
            _verrou.Release();
        }
    }

    public async Task<DictionnaireLocal> DictionnaireAsync()
    {
        var db = await ConnexionAsync();
        return new DictionnaireLocal(await db.Table<VariableLocale>().ToListAsync());
    }

    public async Task<List<Eleve>> ElevesAsync()
    {
        var db = await ConnexionAsync();
        return await db.Table<Eleve>().OrderBy(e => e.Id).ToListAsync();
    }

    public async Task<Eleve?> EleveAsync(int id)
    {
        var db = await ConnexionAsync();
        return await db.Table<Eleve>().Where(e => e.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Eleve> AjouterEleveAsync(string prenom, int niveau)
    {
        var db = await ConnexionAsync();
        var existants = await db.Table<Eleve>().CountAsync();
        var eleve = new Eleve { Prenom = prenom.Trim(), Niveau = niveau, CouleurAvatar = existants % 4, DateCreation = DateTime.Now };
        await db.InsertAsync(eleve);
        return eleve;
    }

    public async Task SupprimerEleveAsync(int id)
    {
        var db = await ConnexionAsync();
        await db.ExecuteAsync("DELETE FROM Tentative WHERE EleveId = ?", id);
        await db.ExecuteAsync("DELETE FROM Eleve WHERE Id = ?", id);
    }

    public async Task<List<Tentative>> TentativesAsync(int eleveId)
    {
        var db = await ConnexionAsync();
        return await db.Table<Tentative>().Where(t => t.EleveId == eleveId).OrderBy(t => t.Date).ToListAsync();
    }

    public async Task EnregistrerTentativeAsync(Tentative tentative)
    {
        var db = await ConnexionAsync();
        await db.InsertAsync(tentative);
    }

    public async Task EnregistrerTentativesAsync(IEnumerable<Tentative> tentatives)
    {
        var db = await ConnexionAsync();
        await db.InsertAllAsync(tentatives);
    }

    public async Task<int> NombreTentativesAsync(int eleveId)
    {
        var db = await ConnexionAsync();
        return await db.Table<Tentative>().Where(t => t.EleveId == eleveId).CountAsync();
    }

    public async Task FermerAsync()
    {
        if (_db is not null)
        {
            await _db.CloseAsync();
            _db = null;
        }
    }
}
