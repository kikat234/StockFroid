using System.Globalization;
using System.Text;
using StockFroid.Models;
using StockFroid.Utilities;

namespace StockFroid.Services;

/// <summary>Une perte rattachée au code de son lot (pour l'historique du rapport).</summary>
public record LignePerte(string CodeLot, Perte Perte);

/// <summary>Toutes les données nécessaires à un rapport de campagne.</summary>
public record RapportCampagne(
    Campagne Campagne,
    BilanCampagne Bilan,
    IReadOnlyList<LignePerte> Pertes,
    DateTime GenereLe);

/// <summary>Construit le contenu des rapports (CSV et PDF). Fonctions pures : aucun accès base ni fichier.</summary>
public static class RapportBuilder
{
    // ------------------------------------------------------------------ CSV

    /// <summary>
    /// CSV pour Excel en français : séparateur « ; », virgule décimale, UTF-8 (l'écriture ajoute le BOM).
    /// Le fichier contient quatre blocs : synthèse, répartition par cause, détail des lots, historique des pertes.
    /// </summary>
    public static string ConstruireCsv(RapportCampagne r)
    {
        var b = r.Bilan;
        var sb = new StringBuilder();

        void Ligne(params string[] champs) => sb.Append(string.Join(';', champs)).Append("\r\n");
        void Vide() => sb.Append("\r\n");

        Ligne(Texte("StockFroid - Bilan de campagne"));
        Ligne(Texte("Campagne"), Texte(r.Campagne.Nom));
        Ligne(Texte("Période"), Texte(Periode(r.Campagne)));
        Ligne(Texte("Généré le"), Texte(r.GenereLe.ToString("dd/MM/yyyy HH:mm", Fr.Culture)));
        Vide();

        Ligne(Texte("Synthèse"));
        Ligne(Texte("Indicateur"), Texte("Valeur"));
        Ligne(Texte("Stock initial (kg)"), Num(b.StockInitial));
        Ligne(Texte("Quantité perdue (kg)"), Num(b.TotalPerdu));
        Ligne(Texte("Stock restant (kg)"), Num(b.StockRestant));
        Ligne(Texte("Taux de perte (%)"), Pct(b.TauxPerte));
        Ligne(Texte("Nombre de lots"), b.NbLots.ToString(CultureInfo.InvariantCulture));
        Ligne(Texte("Nombre de constats de pertes"), b.NbConstats.ToString(CultureInfo.InvariantCulture));
        Vide();

        Ligne(Texte("Répartition des pertes par cause"));
        Ligne(Texte("Cause"), Texte("Quantité perdue (kg)"), Texte("Part des pertes (%)"));
        foreach (var c in b.Repartition)
            Ligne(Texte(c.Cause.Libelle()), Num(c.Quantite), Pct(c.Part));
        Vide();

        Ligne(Texte("Détail des lots"));
        Ligne(Texte("Code"), Texte("Produit"), Texte("Lieu de stockage"), Texte("Date de récolte"),
              Texte("Durée de conservation (jours)"), Texte("Date de péremption"), Texte("État"),
              Texte("Quantité initiale (kg)"), Texte("Quantité perdue (kg)"), Texte("Quantité restante (kg)"),
              Texte("Taux de perte (%)"));
        foreach (var l in b.Lots)
            Ligne(Texte(l.Lot.Code), Texte(l.Lot.Produit), Texte(l.Lot.LieuStockage),
                  Fr.Date(l.Lot.DateRecolte), l.Lot.DureeConservationJours.ToString(CultureInfo.InvariantCulture),
                  Fr.Date(l.Lot.DatePeremption), Texte(LibelleEtat(l, r.Campagne)),
                  Num(l.Lot.QuantiteInitiale), Num(l.Perdu), Num(l.Restant), Pct(l.TauxPerte));
        Vide();

        Ligne(Texte("Historique des pertes"));
        Ligne(Texte("Code du lot"), Texte("Date du constat"), Texte("Quantité perdue (kg)"),
              Texte("Cause"), Texte("Commentaire"));
        foreach (var p in r.Pertes)
            Ligne(Texte(p.CodeLot), p.Perte.DateConstat.ToString("dd/MM/yyyy HH:mm", Fr.Culture),
                  Num(p.Perte.QuantitePerdue), Texte(p.Perte.Cause.Libelle()), Texte(p.Perte.Commentaire));

        return sb.ToString();
    }

    private static string Num(double valeur) => valeur.ToString("0.##", Fr.Culture);

    private static string Pct(double ratio) => (ratio * 100).ToString("0.0", Fr.Culture);

    /// <summary>
    /// Protège un champ texte : guillemets si besoin, et apostrophe devant =, +, -, @ pour qu'un tableur
    /// ne l'interprète pas comme une formule (une saisie comme « =1+1 » ne doit pas s'exécuter).
    /// </summary>
    private static string Texte(string? valeur)
    {
        var v = valeur ?? string.Empty;
        if (v.Length > 0 && "=+-@\t\r".Contains(v[0])) v = "'" + v;
        if (v.IndexOfAny(new[] { ';', '"', '\r', '\n' }) >= 0)
            v = "\"" + v.Replace("\"", "\"\"") + "\"";
        return v;
    }

    // ------------------------------------------------------------------ Libellés communs

    public static string Periode(Campagne c) =>
        c.DateFin is { } fin
            ? $"Du {Fr.Date(c.DateDebut)} au {Fr.Date(fin)} (clôturée)"
            : $"Depuis le {Fr.Date(c.DateDebut)} (en cours)";

    public static string LibelleEtat(LotBilan lot, Campagne campagne)
    {
        if (lot.Restant <= 0) return "Épuisé";
        if (!campagne.EstEnCours) return "Clôturée";
        return lot.Etat switch
        {
            EtatPeremption.Perime => "Périmé",
            EtatPeremption.Bientot => "Bientôt périmé",
            _ => "À jour"
        };
    }

    /// <summary>Nom de fichier sûr : sans accents ni caractères spéciaux, horodaté.</summary>
    public static string NomDeFichier(RapportCampagne r, string extension)
    {
        var decompose = r.Campagne.Nom.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in decompose)
        {
            var categorie = CharUnicodeInfo.GetUnicodeCategory(c);
            if (categorie == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(c < 128 && char.IsLetterOrDigit(c) ? c : '_');
        }

        var slug = string.Join('_', sb.ToString().Split('_', StringSplitOptions.RemoveEmptyEntries));
        if (slug.Length > 40) slug = slug[..40].TrimEnd('_');
        if (slug.Length == 0) slug = "campagne";

        return $"StockFroid_Bilan_{slug}_{r.GenereLe:yyyyMMdd_HHmm}.{extension}";
    }

    // ------------------------------------------------------------------ PDF

    private static readonly Couleur Primaire = Couleur.Hex("#166534");
    private static readonly Couleur TexteSombre = Couleur.Hex("#0F172A");
    private static readonly Couleur Gris = Couleur.Hex("#64748B");
    private static readonly Couleur FondGris = Couleur.Hex("#F1F5F9");
    private static readonly Couleur FondZebre = Couleur.Hex("#F8FAFC");
    private static readonly Couleur Ok = Couleur.Hex("#15803D");
    private static readonly Couleur Attention = Couleur.Hex("#B45309");
    private static readonly Couleur Danger = Couleur.Hex("#B91C1C");
    private static readonly Couleur DangerFond = Couleur.Hex("#FEE2E2");

    // Mêmes valeurs que Palette.CouleurCause (Palette dépend de MAUI, pas ce générateur).
    private static Couleur CouleurCause(CauseDePerte cause) => cause switch
    {
        CauseDePerte.Pourriture => Danger,
        CauseDePerte.RuptureDeFroid => Couleur.Hex("#475569"),
        CauseDePerte.Manutention => Couleur.Hex("#9F1239"),
        _ => Gris
    };

    public static byte[] ConstruirePdf(RapportCampagne r)
    {
        const double zone = PdfWriter.Largeur - 2 * PdfWriter.Marge;   // largeur utile
        const double droite = PdfWriter.Largeur - PdfWriter.Marge;
        const double hauteurLigne = 17;

        var b = r.Bilan;
        var pdf = new PdfWriter
        {
            PiedDePage = $"StockFroid — rapport généré le {r.GenereLe.ToString("dd/MM/yyyy 'à' HH:mm", Fr.Culture)}"
        };

        // En-tête
        pdf.Rectangle(0, 0, PdfWriter.Largeur, 78, Primaire);
        pdf.Texte("StockFroid", PdfWriter.Marge, 40, 24, true, Couleur.Blanc);
        pdf.Texte("Bilan de campagne — suivi des lots et des pertes", PdfWriter.Marge, 60, 11, false, Couleur.Blanc);

        pdf.Y = 108;
        pdf.Texte(PdfWriter.Tronquer(r.Campagne.Nom, zone, 16, true), PdfWriter.Marge, pdf.Y, 16, true, TexteSombre);
        pdf.Y += 18;
        pdf.Texte(Periode(r.Campagne), PdfWriter.Marge, pdf.Y, 10, false, Gris);
        pdf.Y += 20;

        // Indicateurs clés
        var critique = b.TauxPerte >= BilanCampagne.SeuilTauxCritique;
        var indicateurs = new (string Libelle, string Valeur, bool Alerte)[]
        {
            ("Stock initial", Fr.Quantite(b.StockInitial), false),
            ("Quantité perdue", Fr.Quantite(b.TotalPerdu), false),
            ("Stock restant", Fr.Quantite(b.StockRestant), false),
            ("Taux de perte", Fr.Pourcent(b.TauxPerte), critique)
        };
        const double ecart = 10;
        var largeurCarte = (zone - 3 * ecart) / 4;
        for (var i = 0; i < indicateurs.Length; i++)
        {
            var x = PdfWriter.Marge + i * (largeurCarte + ecart);
            var (libelle, valeur, alerte) = indicateurs[i];
            pdf.Rectangle(x, pdf.Y, largeurCarte, 52, alerte ? DangerFond : FondGris);
            pdf.Texte(libelle, x + 8, pdf.Y + 17, 8, false, Gris);
            pdf.Texte(PdfWriter.Tronquer(valeur, largeurCarte - 16, 14, true), x + 8, pdf.Y + 38, 14, true,
                      alerte ? Danger : TexteSombre);
        }
        pdf.Y += 52 + 8;
        pdf.Texte(
            critique
                ? $"Seuil critique dépassé (≥ {Fr.Pourcent(BilanCampagne.SeuilTauxCritique)})"
                : $"Taux de perte conforme (< {Fr.Pourcent(BilanCampagne.SeuilTauxCritique)})",
            PdfWriter.Marge, pdf.Y + 8, 9, true, critique ? Danger : Ok);
        pdf.Texte($"{b.NbLots} lot{(b.NbLots > 1 ? "s" : "")} · {b.NbConstats} constat{(b.NbConstats > 1 ? "s" : "")} de pertes",
                  droite - PdfWriter.Mesurer($"{b.NbLots} lot{(b.NbLots > 1 ? "s" : "")} · {b.NbConstats} constat{(b.NbConstats > 1 ? "s" : "")} de pertes", 9),
                  pdf.Y + 8, 9, false, Gris);
        pdf.Y += 30;

        // Répartition des pertes par cause
        Titre("Répartition des pertes par cause");
        if (b.TotalPerdu <= 0)
        {
            pdf.Texte("Aucune perte enregistrée sur cette campagne.", PdfWriter.Marge, pdf.Y + 10, 10, false, Gris);
            pdf.Y += 26;
        }
        else
        {
            const double xBarre = PdfWriter.Marge + 110;
            const double largeurBarre = 220;
            foreach (var c in b.Repartition)
            {
                pdf.ReserverEspace(hauteurLigne);
                pdf.Texte(c.Cause.Libelle(), PdfWriter.Marge, pdf.Y + 11, 10, false, TexteSombre);
                pdf.Rectangle(xBarre, pdf.Y + 3, largeurBarre, 9, FondGris);
                if (c.Part > 0)
                    pdf.Rectangle(xBarre, pdf.Y + 3, Math.Max(2, largeurBarre * Math.Clamp(c.Part, 0, 1)), 9,
                                  CouleurCause(c.Cause));
                pdf.TexteDroite($"{Fr.Quantite(c.Quantite)}  ·  {Fr.Pourcent(c.Part)}", droite, pdf.Y + 11, 10, false, TexteSombre);
                pdf.Y += hauteurLigne;
            }
            pdf.Y += 12;
        }

        // Détail des lots
        Titre("Détail des lots");
        var colonnesLots = new (string Titre, double Largeur, bool Droite)[]
        {
            ("Code", 78, false), ("Produit", 118, false), ("Péremption", 62, false), ("État", 76, false),
            ("Initial", 55, true), ("Perdu", 55, true), ("Taux", 55, true)
        };
        void EnteteLots() => Entete(colonnesLots);
        EnteteLots();
        if (b.Lots.Count == 0)
        {
            pdf.Texte("Aucun lot dans cette campagne.", PdfWriter.Marge, pdf.Y + 10, 10, false, Gris);
            pdf.Y += 26;
        }
        var rang = 0;
        foreach (var l in b.Lots)
        {
            pdf.ReserverEspace(hauteurLigne, EnteteLots);
            var etat = LibelleEtat(l, r.Campagne);
            var couleurEtat = etat switch
            {
                "Périmé" => Danger,
                "Bientôt périmé" => Attention,
                "À jour" => Ok,
                _ => Gris
            };
            Ligne(colonnesLots, rang++, new[]
            {
                l.Lot.Code, l.Lot.Produit, Fr.Date(l.Lot.DatePeremption), etat,
                Fr.Nombre(l.Lot.QuantiteInitiale), Fr.Nombre(l.Perdu), Fr.Pourcent(l.TauxPerte)
            }, couleurEtat: couleurEtat, colonneEtat: 3);
        }
        pdf.Y += 14;

        // Historique des pertes
        pdf.ReserverEspace(70);
        Titre("Historique des pertes");
        var colonnesPertes = new (string Titre, double Largeur, bool Droite)[]
        {
            ("Lot", 78, false), ("Date du constat", 78, false), ("Quantité", 60, true),
            ("Cause", 90, false), ("Commentaire", 193, false)
        };
        void EntetePertes() => Entete(colonnesPertes);
        EntetePertes();
        if (r.Pertes.Count == 0)
        {
            pdf.Texte("Aucune perte enregistrée.", PdfWriter.Marge, pdf.Y + 10, 10, false, Gris);
            pdf.Y += 26;
        }
        rang = 0;
        foreach (var p in r.Pertes)
        {
            pdf.ReserverEspace(hauteurLigne, EntetePertes);
            Ligne(colonnesPertes, rang++, new[]
            {
                p.CodeLot, Fr.Date(p.Perte.DateConstat), Fr.Quantite(p.Perte.QuantitePerdue),
                p.Perte.Cause.Libelle(), p.Perte.Commentaire ?? string.Empty
            });
        }

        return pdf.Construire();

        // ---- fonctions locales de mise en page ----

        void Titre(string texte)
        {
            pdf.ReserverEspace(60);
            pdf.Texte(texte, PdfWriter.Marge, pdf.Y + 12, 12, true, Primaire);
            pdf.Y += 22;
        }

        void Entete((string Titre, double Largeur, bool Droite)[] colonnes)
        {
            pdf.Rectangle(PdfWriter.Marge, pdf.Y, zone, hauteurLigne, FondGris);
            var x = PdfWriter.Marge;
            foreach (var c in colonnes)
            {
                if (c.Droite) pdf.TexteDroite(c.Titre, x + c.Largeur - 4, pdf.Y + 12, 8.5, true, Gris);
                else pdf.Texte(c.Titre, x + 4, pdf.Y + 12, 8.5, true, Gris);
                x += c.Largeur;
            }
            pdf.Y += hauteurLigne;
        }

        void Ligne((string Titre, double Largeur, bool Droite)[] colonnes, int numero, string[] valeurs,
                   Couleur? couleurEtat = null, int colonneEtat = -1)
        {
            if (numero % 2 == 1) pdf.Rectangle(PdfWriter.Marge, pdf.Y, zone, hauteurLigne, FondZebre);
            var x = PdfWriter.Marge;
            for (var i = 0; i < colonnes.Length; i++)
            {
                var c = colonnes[i];
                var gras = i == colonneEtat;
                var texte = PdfWriter.Tronquer(valeurs[i], c.Largeur - 12, 9, gras);
                var couleur = i == colonneEtat && couleurEtat is { } ce ? ce : TexteSombre;
                if (c.Droite) pdf.TexteDroite(texte, x + c.Largeur - 4, pdf.Y + 12, 9, gras, couleur);
                else pdf.Texte(texte, x + 4, pdf.Y + 12, 9, gras, couleur);
                x += c.Largeur;
            }
            pdf.Y += hauteurLigne;
        }
    }
}
