using Microsoft.Maui.Graphics;
using StockFroid.Models;
using StockFroid.Services;
using StockFroid.Utilities;

namespace StockFroid.PageModels.Items;

public class RepartitionItem
{
    public string Libelle { get; init; } = string.Empty;
    public string QuantiteTexte { get; init; } = string.Empty;
    public string PartTexte { get; init; } = string.Empty;
    public double Progression { get; init; }
    public Color Couleur { get; init; } = Colors.Gray;

    public static RepartitionItem Creer(RepartitionCause r) => new()
    {
        Libelle = r.Cause.Libelle(),
        QuantiteTexte = Fr.Quantite(r.Quantite),
        PartTexte = Fr.Pourcent(r.Part),
        Progression = Math.Clamp(r.Part, 0, 1),
        Couleur = Palette.CouleurCause(r.Cause)
    };
}

public class LotCritiqueItem
{
    public string Code { get; init; } = string.Empty;
    public string PerduTexte { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string PartTexte { get; init; } = string.Empty;

    public static LotCritiqueItem Creer(LotCritique c) => new()
    {
        Code = $"#{c.Lot.Code}",
        PerduTexte = $"{Fr.Quantite(c.Perdu)} perdus",
        Detail = $"{c.Lot.LieuStockage} • {c.CausePrincipale.Libelle()}",
        PartTexte = Fr.Pourcent(c.PartDuLot)
    };
}
