namespace StockFroid.Models;

/// <summary>
/// Règles métier de péremption (fonctions pures, sans accès base ni interface).
/// Date de péremption = date de récolte + durée de conservation.
/// </summary>
public static class PeremptionCalculator
{
    /// <summary>Un lot est « bientôt périmé » à partir de ce nombre de jours restants.</summary>
    public const int SeuilBientotJours = 3;

    public static DateTime Calculer(DateTime dateRecolte, int dureeConservationJours) =>
        dateRecolte.Date.AddDays(dureeConservationJours);

    public static int JoursRestants(DateTime datePeremption, DateTime maintenant) =>
        (datePeremption.Date - maintenant.Date).Days;

    public static EtatPeremption Evaluer(DateTime datePeremption, DateTime maintenant)
    {
        var jours = JoursRestants(datePeremption, maintenant);

        if (jours < 0) return EtatPeremption.Perime;
        if (jours <= SeuilBientotJours) return EtatPeremption.Bientot;
        return EtatPeremption.AJour;
    }
}
