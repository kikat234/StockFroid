using SQLite;
using StockFroid.Models;
using StockFroid.Services;

namespace StockFroid.Data;

/// <summary>Données de démonstration, calées sur la date du jour pour illustrer les trois états.</summary>
internal static class DemoData
{
    /// <summary>Identifiants du compte de démonstration (à supprimer pour une mise en production).</summary>
    public const string IdentifiantDemo = "AGT-2026-88";
    public const string MotDePasseDemo = "stockfroid2026";

    public static async Task InsererAgentAsync(SQLiteAsyncConnection db)
    {
        var (sel, empreinte) = MotDePasseHasher.Hacher(MotDePasseDemo);
        await db.InsertAsync(new Agent
        {
            Identifiant = AuthService.NormaliserIdentifiant(IdentifiantDemo),
            NomComplet = "Agent de démonstration",
            Affectation = "Entrepôt Nord – Zone maraîchère",
            Sel = sel,
            EmpreinteMotDePasse = empreinte,
            Iterations = MotDePasseHasher.IterationsParDefaut,
            DateCreation = DateTime.Now
        });
    }

    public static async Task InsererAsync(SQLiteAsyncConnection db)
    {
        var aujourdhui = DateTime.Today;

        var tomates = new Campagne { Nom = "Campagne 2026 — Tomates", DateDebut = aujourdhui.AddDays(-60) };
        var plantains = new Campagne { Nom = "Campagne 2026 — Plantains", DateDebut = aujourdhui.AddDays(-30) };
        var oignons = new Campagne
        {
            Nom = "Campagne 2025 — Oignons",
            DateDebut = aujourdhui.AddDays(-365),
            DateFin = aujourdhui.AddDays(-290)
        };
        await db.InsertAsync(tomates);
        await db.InsertAsync(plantains);
        await db.InsertAsync(oignons);

        async Task<Lot> AjouterLot(Campagne c, string code, string produit, int joursDepuisRecolte,
                                   int duree, double quantite, string lieu)
        {
            var lot = new Lot
            {
                CampagneId = c.Id,
                Code = code,
                Produit = produit,
                DateRecolte = aujourdhui.AddDays(-joursDepuisRecolte),
                DureeConservationJours = duree,
                QuantiteInitiale = quantite,
                Unite = "kg",
                LieuStockage = lieu
            };
            await db.InsertAsync(lot);
            return lot;
        }

        async Task AjouterPerte(Lot lot, DateTime quand, double quantite, CauseDePerte cause, string? commentaire = null)
        {
            await db.InsertAsync(new Perte
            {
                LotId = lot.Id,
                DateConstat = quand,
                QuantitePerdue = quantite,
                Cause = cause,
                Commentaire = commentaire
            });
        }

        // Campagne tomates : un lot périmé (J+1), un lot bientôt périmé, deux lots à jour.
        var roma = await AjouterLot(tomates, "LOT-TOM-003", "Tomate Roma", 11, 10, 150, "Chambre froide A1");
        var coeur = await AjouterLot(tomates, "LOT-TOM-008", "Tomate Cœur de Bœuf", 11, 14, 180, "Chambre froide B2");
        await AjouterLot(tomates, "LOT-TOM-009", "Tomate Cerise", 4, 14, 200, "Chambre froide A1");
        await AjouterLot(tomates, "LOT-TOM-010", "Tomate Roma", 2, 14, 400, "Chambre froide A2");

        await AjouterPerte(roma, aujourdhui.AddHours(9).AddMinutes(14), 20, CauseDePerte.Pourriture);
        await AjouterPerte(roma, aujourdhui.AddDays(-3).AddHours(14).AddMinutes(30), 15, CauseDePerte.RuptureDeFroid);
        await AjouterPerte(coeur, aujourdhui.AddDays(-2).AddHours(11).AddMinutes(5), 10, CauseDePerte.Manutention);

        // Campagne plantains.
        var plantain1 = await AjouterLot(plantains, "LOT-BAN-001", "Banane plantain", 7, 8, 300, "Hangar ventilé");
        await AjouterLot(plantains, "LOT-BAN-002", "Banane plantain", 2, 10, 250, "Hangar ventilé");
        await AjouterPerte(plantain1, aujourdhui.AddDays(-1).AddHours(8), 30, CauseDePerte.Autre, "Dessèchement");

        // Campagne clôturée (oignons).
        var oignon1 = await AjouterLot(oignons, "LOT-OIG-001", "Oignon", 340, 120, 800, "Entrepôt sec");
        await AjouterPerte(oignon1, aujourdhui.AddDays(-300).AddHours(10), 40, CauseDePerte.Pourriture);
        await AjouterPerte(oignon1, aujourdhui.AddDays(-295).AddHours(15), 25, CauseDePerte.Manutention);
    }
}
