using StockFroid.Data;
using StockFroid.Models;
using StockFroid.Utilities;

namespace StockFroid.Services;

public class PerteService
{
    private readonly StockFroidDatabase _database;
    private readonly LotService _lots;

    public PerteService(StockFroidDatabase database, LotService lots)
    {
        _database = database;
        _lots = lots;
    }

    public async Task<List<Perte>> GetPertesDuLotAsync(int lotId)
    {
        var db = await _database.GetConnectionAsync();
        var pertes = await db.Table<Perte>().Where(p => p.LotId == lotId).ToListAsync();
        return pertes.OrderByDescending(p => p.DateConstat).ToList();
    }

    /// <summary>
    /// BACK-END (fonction implémentée) : déclare une perte sur un lot.
    /// Règle métier principale : la quantité perdue ne peut pas dépasser le stock restant.
    /// </summary>
    public async Task<ResultatOperation> DeclarerPerteAsync(
        int lotId, DateTime dateConstat, double quantite, CauseDePerte cause, string? commentaire)
    {
        if (quantite <= 0)
            return ResultatOperation.Echec("La quantité perdue doit être supérieure à 0.");

        var lotStock = await _lots.GetLotAvecStockAsync(lotId);
        if (lotStock is null)
            return ResultatOperation.Echec("Lot introuvable.");

        if (dateConstat.Date > DateTime.Today)
            return ResultatOperation.Echec("La date du constat ne peut pas être dans le futur.");
        if (dateConstat.Date < lotStock.Lot.DateRecolte.Date)
            return ResultatOperation.Echec("La date du constat ne peut pas précéder la date de récolte.");

        if (quantite - lotStock.Restant > 1e-9)
            return ResultatOperation.Echec(
                $"La quantité dépasse le stock restant ({Fr.Quantite(lotStock.Restant, lotStock.Lot.Unite)}).");

        var db = await _database.GetConnectionAsync();
        await db.InsertAsync(new Perte
        {
            LotId = lotId,
            DateConstat = dateConstat.Date + DateTime.Now.TimeOfDay,
            QuantitePerdue = quantite,
            Cause = cause,
            Commentaire = string.IsNullOrWhiteSpace(commentaire) ? null : commentaire.Trim()
        });

        return ResultatOperation.Ok("Perte enregistrée.");
    }
}
