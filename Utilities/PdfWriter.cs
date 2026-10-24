using System.Globalization;
using System.Text;

namespace StockFroid.Utilities;

public readonly record struct Couleur(double R, double G, double B)
{
    public static readonly Couleur Noir = new(0, 0, 0);
    public static readonly Couleur Blanc = new(1, 1, 1);

    /// <summary>Crée une couleur depuis un code « #RRGGBB ».</summary>
    public static Couleur Hex(string hex)
    {
        var h = hex.TrimStart('#');
        return new Couleur(
            Convert.ToInt32(h.Substring(0, 2), 16) / 255.0,
            Convert.ToInt32(h.Substring(2, 2), 16) / 255.0,
            Convert.ToInt32(h.Substring(4, 2), 16) / 255.0);
    }
}

/// <summary>
/// Générateur PDF minimal, sans dépendance : pages A4, texte (Helvetica) et rectangles pleins.
/// Les ordonnées passées aux méthodes sont mesurées depuis le HAUT de la page, en points.
/// Le texte est encodé en WinAnsi : les accents français sont conservés.
/// </summary>
public sealed class PdfWriter
{
    public const double Largeur = 595;
    public const double Hauteur = 842;
    public const double Marge = 48;

    /// <summary>Limite basse du contenu (au-dessus du pied de page).</summary>
    public const double BasUtile = Hauteur - 56;

    private readonly List<StringBuilder> _pages = new();
    private StringBuilder _courante;

    public PdfWriter()
    {
        _courante = new StringBuilder();
        _pages.Add(_courante);
        Y = Marge;
    }

    /// <summary>Position verticale courante (depuis le haut de la page).</summary>
    public double Y { get; set; }

    public string PiedDePage { get; set; } = string.Empty;

    public int NombrePages => _pages.Count;

    public void NouvellePage()
    {
        _courante = new StringBuilder();
        _pages.Add(_courante);
        Y = Marge;
    }

    /// <summary>Passe à une nouvelle page si <paramref name="hauteur"/> ne tient plus sur la page courante.</summary>
    public void ReserverEspace(double hauteur, Action? surNouvellePage = null)
    {
        if (Y + hauteur <= BasUtile) return;
        NouvellePage();
        surNouvellePage?.Invoke();
    }

    public void Rectangle(double x, double haut, double largeur, double hauteur, Couleur fond) =>
        Rectangle(_courante, x, haut, largeur, hauteur, fond);

    public void Texte(string texte, double x, double lignedeBase, double taille,
                      bool gras = false, Couleur? couleur = null) =>
        Texte(_courante, texte, x, lignedeBase, taille, gras, couleur ?? Couleur.Noir);

    public void TexteDroite(string texte, double xDroite, double lignedeBase, double taille,
                            bool gras = false, Couleur? couleur = null) =>
        Texte(texte, xDroite - Mesurer(texte, taille, gras), lignedeBase, taille, gras, couleur);

    /// <summary>Largeur approximative d'un texte en Helvetica (métriques arrondies par famille de caractères).</summary>
    public static double Mesurer(string texte, double taille, bool gras = false)
    {
        double total = 0;
        foreach (var c in texte) total += LargeurCaractere(c);
        return total / 1000.0 * taille * (gras ? 1.06 : 1.0);
    }

    /// <summary>Raccourcit un texte (avec « … ») pour qu'il tienne dans la largeur donnée.</summary>
    public static string Tronquer(string texte, double largeurMax, double taille, bool gras = false)
    {
        if (Mesurer(texte, taille, gras) <= largeurMax) return texte;
        var t = texte;
        while (t.Length > 1 && Mesurer(t + "…", taille, gras) > largeurMax) t = t[..^1];
        return t.TrimEnd() + "…";
    }

    public byte[] Construire()
    {
        var flux = new MemoryStream();
        var offsets = new List<long>();

        void Ecrire(string s)
        {
            var octets = Encoding.Latin1.GetBytes(s);
            flux.Write(octets, 0, octets.Length);
        }

        void DebutObjet(int numero)
        {
            offsets.Add(flux.Position);
            Ecrire($"{numero} 0 obj\n");
        }

        Ecrire("%PDF-1.4\n%âãÏÓ\n");

        // 1 : catalogue, 2 : arbre des pages, 3-4 : polices ; page i → objets 5+2i (page) et 6+2i (contenu).
        DebutObjet(1);
        Ecrire("<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        DebutObjet(2);
        var kids = string.Join(" ", Enumerable.Range(0, _pages.Count).Select(i => $"{5 + 2 * i} 0 R"));
        Ecrire($"<< /Type /Pages /Kids [{kids}] /Count {_pages.Count} >>\nendobj\n");

        DebutObjet(3);
        Ecrire("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");
        DebutObjet(4);
        Ecrire("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");

        var gris = Couleur.Hex("#64748B");
        for (var i = 0; i < _pages.Count; i++)
        {
            var contenu = new StringBuilder(_pages[i].ToString());
            Rectangle(contenu, Marge, Hauteur - 44, Largeur - 2 * Marge, 0.6, Couleur.Hex("#CBD5E1"));
            Texte(contenu, PiedDePage, Marge, Hauteur - 30, 8, false, gris);
            var pageTexte = $"Page {i + 1} / {_pages.Count}";
            Texte(contenu, pageTexte, Largeur - Marge - Mesurer(pageTexte, 8), Hauteur - 30, 8, false, gris);

            var octetsContenu = Encoding.Latin1.GetBytes(contenu.ToString());

            DebutObjet(5 + 2 * i);
            Ecrire($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Nombre(Largeur)} {Nombre(Hauteur)}] " +
                   $"/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {6 + 2 * i} 0 R >>\nendobj\n");

            DebutObjet(6 + 2 * i);
            Ecrire($"<< /Length {octetsContenu.Length} >>\nstream\n");
            flux.Write(octetsContenu, 0, octetsContenu.Length);
            Ecrire("\nendstream\nendobj\n");
        }

        var debutXref = flux.Position;
        Ecrire($"xref\n0 {offsets.Count + 1}\n");
        Ecrire("0000000000 65535 f \n");
        foreach (var offset in offsets) Ecrire($"{offset:D10} 00000 n \n");
        Ecrire($"trailer\n<< /Size {offsets.Count + 1} /Root 1 0 R >>\nstartxref\n{debutXref}\n%%EOF\n");

        return flux.ToArray();
    }

    private static void Rectangle(StringBuilder sb, double x, double haut, double largeur, double hauteur, Couleur fond)
    {
        sb.Append($"{Nombre(fond.R)} {Nombre(fond.G)} {Nombre(fond.B)} rg\n");
        sb.Append($"{Nombre(x)} {Nombre(Hauteur - haut - hauteur)} {Nombre(largeur)} {Nombre(hauteur)} re\nf\n");
    }

    private static void Texte(StringBuilder sb, string texte, double x, double ligneDeBase, double taille,
                              bool gras, Couleur couleur)
    {
        if (texte.Length == 0) return;
        sb.Append("BT\n");
        sb.Append($"/{(gras ? "F2" : "F1")} {Nombre(taille)} Tf\n");
        sb.Append($"{Nombre(couleur.R)} {Nombre(couleur.G)} {Nombre(couleur.B)} rg\n");
        sb.Append($"1 0 0 1 {Nombre(x)} {Nombre(Hauteur - ligneDeBase)} Tm\n");
        sb.Append('(').Append(EncoderTexte(texte)).Append(") Tj\nET\n");
    }

    private static string Nombre(double valeur) => valeur.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>Convertit un texte Unicode en octets WinAnsi (représentés par des caractères 0-255) et échappe ( ) \.</summary>
    private static string EncoderTexte(string texte)
    {
        var sb = new StringBuilder(texte.Length + 8);
        foreach (var c in texte)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '(': sb.Append("\\("); break;
                case ')': sb.Append("\\)"); break;
                case '\u00A0': case '\u202F': case '\u2009': case '\u2007': sb.Append(' '); break;
                case '\u2013': sb.Append('\u0096'); break;   // –
                case '\u2014': sb.Append('\u0097'); break;   // —
                case '\u2018': sb.Append('\u0091'); break;   // ‘
                case '\u2019': sb.Append('\u0092'); break;   // ’
                case '\u201C': sb.Append('\u0093'); break;   // “
                case '\u201D': sb.Append('\u0094'); break;   // ”
                case '\u2022': sb.Append('\u0095'); break;   // •
                case '\u2026': sb.Append('\u0085'); break;   // …
                case '\u20AC': sb.Append('\u0080'); break;   // €
                case '\u0152': sb.Append('\u008C'); break;   // Œ
                case '\u0153': sb.Append('\u009C'); break;   // œ
                case '\u2265': sb.Append(">="); break;       // ≥
                case '\u2264': sb.Append("<="); break;       // ≤
                default:
                    if (c < 32) sb.Append(' ');
                    else if (c < 256) sb.Append(c);
                    else sb.Append('?');
                    break;
            }
        }
        return sb.ToString();
    }

    private static double LargeurCaractere(char c) => c switch
    {
        ' ' or '.' or ',' or ':' or ';' or '/' or '!' => 278,
        'i' or 'j' or 'l' or '\'' or '|' => 222,
        't' or 'f' or 'I' => 278,
        '(' or ')' or '-' or 'r' => 333,
        'm' or 'M' => 833,
        'w' => 722,
        'W' => 944,
        's' or 'c' or 'k' or 'v' or 'x' or 'y' or 'z' => 500,
        >= '0' and <= '9' => 556,
        >= 'A' and <= 'Z' => 667,
        >= 'a' and <= 'z' => 556,
        _ => 556
    };
}
