using Microsoft.Maui.Graphics;
using StockFroid.Models;

namespace StockFroid.Utilities;

/// <summary>Couleurs utilisées côté code (mêmes valeurs que Resources/Styles/Theme.xaml).</summary>
public static class Palette
{
    public static readonly Color Ok = Color.FromArgb("#15803D");
    public static readonly Color OkFond = Color.FromArgb("#DCFCE7");
    public static readonly Color Attention = Color.FromArgb("#B45309");
    public static readonly Color AttentionFond = Color.FromArgb("#FEF3C7");
    public static readonly Color Danger = Color.FromArgb("#B91C1C");
    public static readonly Color DangerFond = Color.FromArgb("#FEE2E2");
    public static readonly Color Muted = Color.FromArgb("#64748B");
    public static readonly Color MutedFond = Color.FromArgb("#F1F5F9");

    public static Color CouleurCause(CauseDePerte cause) => cause switch
    {
        CauseDePerte.Pourriture => Danger,
        CauseDePerte.RuptureDeFroid => Color.FromArgb("#475569"),
        CauseDePerte.Manutention => Color.FromArgb("#9F1239"),
        _ => Muted
    };
}
