using StockFroid.Data;
using StockFroid.Models;

namespace StockFroid.Services;

public record RepartitionCause(CauseDePerte Cause, double Quantite, double Part);

public record LotCritique(Lot Lot, double Perdu, double PartDuLot, CauseDePerte CausePrincipale);

public class BilanCampagne
{
    public int NbLots { get; init; }
    public int LotsActifs { get; init; }
    public int NbConstats { get; init; }
    public int NbPerimes { get; init; }
    public int NbBientot { get; init; }
    public double StockInitial { get; init; }
    public double TotalPerdu { get; init; }

    public double StockRestant => Math.Max(0, StockInitial - TotalPerdu);
    public double TauxPerte => StockInitial <= 0 ? 0 : TotalPerdu / StockInitial;

    public IReadOnlyList<RepartitionCause> Repartition { get; init; } = Array.Empty<RepartitionCause>();
    public IReadOnlyList<LotCritique> LotsCritiques { get; init; } = Array.Empty<LotCritique>();
}

public class BilanService
{
    private readonly StockFroidDatabase _database;

    public BilanService(StockFroidDatabase database) => _database = database;

    /// <summary>Agrège stocks, pertes, taux de perte et répartition par cause pour une campagne.</summary>
    public async Task<BilanCampagne> CalculerAsync(int campagneId)
    {
        var db = await _database.GetConnectionAsync();
        var campagne = await db.FindAsync<Campagne>(campagneId);
        var lots = await db.Table<Lot>().Where(l => l.CampagneId == campagneId).ToListAsync();

        var ids = lots.Select(l => l.Id).ToHashSet();
        var pertes = (await db.Table<Perte>().ToListAsync()).Where(p => ids.Contains(p.LotId)).ToList();
        var perdusParLot = pertes.GroupBy(p => p.LotId).ToDictionary(g => g.Key, g => g.Sum(p => p.QuantitePerdue));

        var maintenant = DateTime.Now;
        var enCours = campagne?.EstEnCours ?? false;
        int actifs = 0, perimes = 0, bientot = 0;

        foreach (var lot in lots)
        {
            var restant = lot.QuantiteInitiale - perdusParLot.GetValueOrDefault(lot.Id);
            if (restant <= 0) continue;

            actifs++;
            if (!enCours) continue; // pas d'alerte sur une campagne clôturée

            switch (PeremptionCalculator.Evaluer(lot.DatePeremption, maintenant))
            {
                case EtatPeremption.Perime: perimes++; break;
                case EtatPeremption.Bientot: bientot++; break;
            }
        }

        var totalPerdu = pertes.Sum(p => p.QuantitePerdue);

        var repartition = Enum.GetValues<CauseDePerte>()
            .Select(c =>
            {
                var q = pertes.Where(p => p.Cause == c).Sum(p => p.QuantitePerdue);
                return new RepartitionCause(c, q, totalPerdu <= 0 ? 0 : q / totalPerdu);
            })
            .OrderByDescending(r => r.Quantite)
            .ToList();

        var critiques = lots
            .Select(l =>
            {
                var perdu = perdusParLot.GetValueOrDefault(l.Id);
                var cause = pertes.Where(p => p.LotId == l.Id)
                    .GroupBy(p => p.Cause)
                    .OrderByDescending(g => g.Sum(p => p.QuantitePerdue))
                    .Select(g => g.Key)
                    .FirstOrDefault();
                return new LotCritique(l, perdu, l.QuantiteInitiale <= 0 ? 0 : perdu / l.QuantiteInitiale, cause);
            })
            .Where(c => c.Perdu > 0)
            .OrderByDescending(c => c.Perdu)
            .Take(3)
            .ToList();

        return new BilanCampagne
        {
            NbLots = lots.Count,
            LotsActifs = actifs,
            NbConstats = pertes.Count,
            NbPerimes = perimes,
            NbBientot = bientot,
            StockInitial = lots.Sum(l => l.QuantiteInitiale),
            TotalPerdu = totalPerdu,
            Repartition = repartition,
            LotsCritiques = critiques
        };
    }
}
