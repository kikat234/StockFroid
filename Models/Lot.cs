using SQLite;

namespace StockFroid.Models;

public class Lot
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int CampagneId { get; set; }

    /// <summary>Code de traçabilité, ex. LOT-TOM-003.</summary>
    public string Code { get; set; } = string.Empty;

    public string Produit { get; set; } = string.Empty;

    public DateTime DateRecolte { get; set; }

    public int DureeConservationJours { get; set; }

    /// <summary>Quantité initiale, toujours exprimée en kg.</summary>
    public double QuantiteInitiale { get; set; }

    public string Unite { get; set; } = "kg";

    public string LieuStockage { get; set; } = string.Empty;

    /// <summary>Valeur calculée (non stockée) : récolte + durée de conservation.</summary>
    [Ignore]
    public DateTime DatePeremption =>
        PeremptionCalculator.Calculer(DateRecolte, DureeConservationJours);
}
