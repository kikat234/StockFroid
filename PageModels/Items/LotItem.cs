using Microsoft.Maui.Graphics;
using StockFroid.Models;
using StockFroid.Services;
using StockFroid.Utilities;

namespace StockFroid.PageModels.Items;

/// <summary>Carte de lot : état de péremption exprimé par une couleur ET un texte (accessibilité).</summary>
public class LotItem
{
    public int Id { get; init; }
    public string Produit { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string BadgeTexte { get; init; } = string.Empty;
    public Color BadgeFond { get; init; } = Colors.Transparent;
    public Color BadgeCouleur { get; init; } = Colors.Black;
    public string RecolteTexte { get; init; } = string.Empty;
    public string EcheanceLibelle { get; init; } = string.Empty;
    public string EcheanceValeur { get; init; } = string.Empty;
    public EtatPeremption Etat { get; init; }
    public DateTime DatePeremption { get; init; }

    /// <summary>0 = périmé, 1 = bientôt, 2 = à jour (sert au tri par urgence).</summary>
    public int Gravite => Etat switch
    {
        EtatPeremption.Perime => 0,
        EtatPeremption.Bientot => 1,
        _ => 2
    };

    public static LotItem Creer(LotAvecStock ls, DateTime maintenant)
    {
        var lot = ls.Lot;
        var date = lot.DatePeremption;
        var etat = PeremptionCalculator.Evaluer(date, maintenant);
        var jours = PeremptionCalculator.JoursRestants(date, maintenant);

        var (texte, fond, couleur) = etat switch
        {
            EtatPeremption.Perime => ("✖ Périmé", Palette.DangerFond, Palette.Danger),
            EtatPeremption.Bientot => (
                jours == 0 ? "⚠ Expire aujourd'hui" : $"⚠ Expire dans {jours} j",
                Palette.AttentionFond, Palette.Attention),
            _ => ("✓ À jour", Palette.OkFond, Palette.Ok)
        };

        return new LotItem
        {
            Id = lot.Id,
            Produit = lot.Produit,
            Detail = $"{Fr.Quantite(ls.Restant)} • {lot.LieuStockage} • #{lot.Code}",
            BadgeTexte = texte,
            BadgeFond = fond,
            BadgeCouleur = couleur,
            RecolteTexte = Fr.Date(lot.DateRecolte),
            EcheanceLibelle = etat == EtatPeremption.Perime ? "Statut" : "Échéance",
            EcheanceValeur = etat == EtatPeremption.Perime
                ? $"Dépassé de {Fr.Jours(-jours)}"
                : $"{Fr.Date(date)} ({jours} j)",
            Etat = etat,
            DatePeremption = date
        };
    }
}
