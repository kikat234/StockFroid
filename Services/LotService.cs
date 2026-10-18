using SQLite;
using StockFroid.Data;
using StockFroid.Models;

namespace StockFroid.Services;

/// <summary>Un lot avec la quantité déjà perdue (le stock restant est dérivé).</summary>
public record LotAvecStock(Lot Lot, double Perdu)
{
    public double Restant => Math.Max(0, Lot.QuantiteInitiale - Perdu);
}

public class LotService
{
    private readonly StockFroidDatabase _database;

    public LotService(StockFroidDatabase database) => _database = database;

    public async Task<List<LotAvecStock>> GetLotsAvecStockAsync(int campagneId)
    {
        var db = await _database.GetConnectionAsync();
        var lots = await db.Table<Lot>().Where(l => l.CampagneId == campagneId).ToListAsync();
        var perdus = await TotauxPerdusAsync(db, lots.Select(l => l.Id));
        return lots.Select(l => new LotAvecStock(l, perdus.GetValueOrDefault(l.Id))).ToList();
    }

    public async Task<LotAvecStock?> GetLotAvecStockAsync(int lotId)
    {
        var db = await _database.GetConnectionAsync();
        var lot = await db.FindAsync<Lot>(lotId);
        if (lot is null) return null;

        var perdus = await TotauxPerdusAsync(db, new[] { lotId });
        return new LotAvecStock(lot, perdus.GetValueOrDefault(lotId));
    }

    /// <summary>
    /// BACK-END (fonction implémentée) : valide puis enregistre un nouveau lot.
    /// La date de péremption n'est jamais saisie : elle est dérivée par PeremptionCalculator.
    /// </summary>
    public async Task<ResultatOperation<Lot>> CreerLotAsync(
        int campagneId, string produit, DateTime dateRecolte, int dureeConservationJours,
        double quantiteKg, string unite, string lieuStockage)
    {
        if (campagneId <= 0)
            return ResultatOperation<Lot>.Echec("Aucune campagne active : créez ou choisissez une campagne.");
        if (string.IsNullOrWhiteSpace(produit))
            return ResultatOperation<Lot>.Echec("Choisissez le produit stocké.");
        if (dateRecolte.Date > DateTime.Today)
            return ResultatOperation<Lot>.Echec("La date de récolte ne peut pas être dans le futur.");
        if (dureeConservationJours <= 0 || dureeConservationJours > 730)
            return ResultatOperation<Lot>.Echec("La durée de conservation doit être comprise entre 1 et 730 jours.");
        if (quantiteKg <= 0)
            return ResultatOperation<Lot>.Echec("La quantité doit être supérieure à 0.");
        if (string.IsNullOrWhiteSpace(lieuStockage))
            return ResultatOperation<Lot>.Echec("Indiquez le lieu de stockage.");

        var db = await _database.GetConnectionAsync();
        var campagne = await db.FindAsync<Campagne>(campagneId);
        if (campagne is null)
            return ResultatOperation<Lot>.Echec("Campagne introuvable.");
        if (!campagne.EstEnCours)
            return ResultatOperation<Lot>.Echec("Cette campagne est clôturée : impossible d'y ajouter un lot.");

        var lot = new Lot
        {
            CampagneId = campagneId,
            Produit = produit.Trim(),
            DateRecolte = dateRecolte.Date,
            DureeConservationJours = dureeConservationJours,
            QuantiteInitiale = quantiteKg,
            Unite = string.IsNullOrWhiteSpace(unite) ? "kg" : unite,
            LieuStockage = lieuStockage.Trim()
        };

        await db.InsertAsync(lot);                       // l'Id est renseigné après l'insertion
        lot.Code = $"LOT-{Prefixe(lot.Produit)}-{lot.Id:000}";
        await db.UpdateAsync(lot);

        return ResultatOperation<Lot>.Ok(lot, "Lot enregistré.");
    }

    private static string Prefixe(string produit)
    {
        var lettres = new string(produit.Where(char.IsLetter).Take(3).ToArray()).ToUpperInvariant();
        return lettres.Length == 0 ? "LOT" : lettres;
    }

    // Volumes faibles (stockage local) : on agrège en mémoire pour rester simple et lisible.
    private static async Task<Dictionary<int, double>> TotauxPerdusAsync(SQLiteAsyncConnection db, IEnumerable<int> lotIds)
    {
        var ids = lotIds.ToHashSet();
        var pertes = await db.Table<Perte>().ToListAsync();
        return pertes
            .Where(p => ids.Contains(p.LotId))
            .GroupBy(p => p.LotId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.QuantitePerdue));
    }
}
