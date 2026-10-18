using StockFroid.Data;
using StockFroid.Models;

namespace StockFroid.Services;

public class CampagneService
{
    private readonly StockFroidDatabase _database;

    public CampagneService(StockFroidDatabase database) => _database = database;

    public async Task<List<Campagne>> GetToutesAsync()
    {
        var db = await _database.GetConnectionAsync();
        var liste = await db.Table<Campagne>().ToListAsync();
        return liste.OrderByDescending(c => c.DateDebut).ToList();
    }

    public async Task<Campagne?> GetAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        return await db.FindAsync<Campagne>(id);
    }

    public async Task<ResultatOperation<Campagne>> CreerAsync(string nom)
    {
        nom = (nom ?? string.Empty).Trim();

        if (nom.Length == 0)
            return ResultatOperation<Campagne>.Echec("Le nom de la campagne est obligatoire.");
        if (nom.Length > 60)
            return ResultatOperation<Campagne>.Echec("Le nom est trop long (60 caractères maximum).");

        var existantes = await GetToutesAsync();
        if (existantes.Any(c => string.Equals(c.Nom, nom, StringComparison.CurrentCultureIgnoreCase)))
            return ResultatOperation<Campagne>.Echec("Une campagne porte déjà ce nom.");

        var db = await _database.GetConnectionAsync();
        var campagne = new Campagne { Nom = nom, DateDebut = DateTime.Today };
        await db.InsertAsync(campagne);
        return ResultatOperation<Campagne>.Ok(campagne, "Campagne créée.");
    }
}
