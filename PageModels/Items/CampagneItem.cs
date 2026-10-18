using Microsoft.Maui.Graphics;
using StockFroid.Models;
using StockFroid.Services;
using StockFroid.Utilities;

namespace StockFroid.PageModels.Items;

/// <summary>Carte de campagne prête à afficher (textes et couleurs déjà calculés).</summary>
public class CampagneItem
{
    public int Id { get; init; }
    public string Nom { get; init; } = string.Empty;
    public string Periode { get; init; } = string.Empty;
    public bool EstCloturee { get; init; }
    public string StatutLibelle { get; init; } = string.Empty;
    public Color StatutFond { get; init; } = Colors.Transparent;
    public Color StatutCouleur { get; init; } = Colors.Black;
    public bool AAlerte { get; init; }
    public string AlerteTexte { get; init; } = string.Empty;
    public string LibelleLots { get; init; } = string.Empty;
    public string ValeurLots { get; init; } = string.Empty;
    public string LibelleTaux { get; init; } = string.Empty;
    public string ValeurTaux { get; init; } = string.Empty;
    public Color CouleurTaux { get; init; } = Colors.Black;
    public double Opacite { get; init; } = 1;

    public static CampagneItem Creer(Campagne c, BilanCampagne b)
    {
        var cloturee = !c.EstEnCours;
        var alertes = cloturee ? 0 : b.NbPerimes + b.NbBientot;
        var fin = c.DateFin is null ? "En cours" : Fr.DateCourte(c.DateFin.Value);

        return new CampagneItem
        {
            Id = c.Id,
            Nom = c.Nom,
            Periode = $"{Fr.DateCourte(c.DateDebut)} → {fin}",
            EstCloturee = cloturee,
            StatutLibelle = cloturee ? "Clôturée" : alertes > 0 ? "Attention" : "En cours",
            StatutFond = cloturee ? Palette.MutedFond : alertes > 0 ? Palette.AttentionFond : Palette.OkFond,
            StatutCouleur = cloturee ? Palette.Muted : alertes > 0 ? Palette.Attention : Palette.Ok,
            AAlerte = alertes > 0,
            AlerteTexte = alertes == 1
                ? "1 lot à surveiller (péremption proche ou dépassée)"
                : $"{alertes} lots à surveiller (péremption proche ou dépassée)",
            LibelleLots = cloturee ? "Total traité" : "Lots actifs",
            ValeurLots = $"{(cloturee ? b.NbLots : b.LotsActifs):00} lots",
            LibelleTaux = cloturee ? "Perte finale" : "Taux de perte",
            ValeurTaux = Fr.Pourcent(b.TauxPerte),
            CouleurTaux = cloturee ? Palette.Muted
                : b.TauxPerte < 0.05 ? Palette.Ok
                : b.TauxPerte < 0.10 ? Palette.Attention
                : Palette.Danger,
            Opacite = cloturee ? 0.7 : 1
        };
    }
}
