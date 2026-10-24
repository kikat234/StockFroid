using StockFroid.Models;

namespace StockFroid.Services;

public record RepartitionCause(CauseDePerte Cause, double Quantite, double Part);

public record LotCritique(Lot Lot, double Perdu, double PartDuLot, CauseDePerte CausePrincipale);

/// <summary>Situation d'un lot : quantité perdue et état de péremption à la date du calcul.</summary>
public record LotBilan(Lot Lot, double Perdu, EtatPeremption Etat)
{
    public double Restant => Math.Max(0, Lot.QuantiteInitiale - Perdu);

    public double TauxPerte => Lot.QuantiteInitiale <= 0 ? 0 : Perdu / Lot.QuantiteInitiale;
}

public class BilanCampagne
{
    /// <summary>Au-delà de ce taux de perte, la campagne est signalée comme critique.</summary>
    public const double SeuilTauxCritique = 0.10;

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

    /// <summary>Tous les lots de la campagne (triés par date de péremption), avec leurs pertes.</summary>
    public IReadOnlyList<LotBilan> Lots { get; init; } = Array.Empty<LotBilan>();
}
