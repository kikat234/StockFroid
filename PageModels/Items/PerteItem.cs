using StockFroid.Models;
using StockFroid.Utilities;

namespace StockFroid.PageModels.Items;

public class PerteItem
{
    public string DateHeure { get; init; } = string.Empty;
    public string Cause { get; init; } = string.Empty;
    public string Quantite { get; init; } = string.Empty;
    public string Commentaire { get; init; } = string.Empty;

    public bool ACommentaire => !string.IsNullOrWhiteSpace(Commentaire);

    public static PerteItem Creer(Perte p) => new()
    {
        DateHeure = p.DateConstat.ToString("dd/MM/yyyy • HH:mm", Fr.Culture),
        Cause = p.Cause.Libelle(),
        Quantite = $"-{Fr.Quantite(p.QuantitePerdue)}",
        Commentaire = p.Commentaire ?? string.Empty
    };
}
