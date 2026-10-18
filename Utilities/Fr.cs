using System.Globalization;

namespace StockFroid.Utilities;

/// <summary>Formatage français centralisé (dates, nombres, pourcentages).</summary>
public static class Fr
{
    public static readonly CultureInfo Culture = new("fr-FR");

    public static string Nombre(double valeur) =>
        valeur.ToString("#,##0.##", Culture).Replace('\u202f', ' ').Replace('\u00a0', ' ');

    public static string Quantite(double valeur, string unite = "kg") => $"{Nombre(valeur)} {unite}";

    public static string Pourcent(double ratio) => $"{(ratio * 100).ToString("0.0", Culture)} %";

    public static string Date(DateTime d) => d.ToString("dd/MM/yyyy", Culture);

    public static string DateCourte(DateTime d) => d.ToString("dd MMM yyyy", Culture);

    public static string DateLongue(DateTime d) => d.ToString("dd MMMM yyyy", Culture);

    public static string Jours(int n) => Math.Abs(n) <= 1 ? $"{n} jour" : $"{n} jours";

    public static bool TryParseDouble(string? texte, out double valeur) =>
        double.TryParse((texte ?? string.Empty).Trim().Replace(',', '.'),
            NumberStyles.Float, CultureInfo.InvariantCulture, out valeur);
}
