namespace StockFroid.Models;

public enum CauseDePerte
{
    Pourriture = 0,
    RuptureDeFroid = 1,
    Manutention = 2,
    Autre = 3
}

public enum EtatPeremption
{
    AJour,
    Bientot,
    Perime
}

public static class CauseDePerteExtensions
{
    public static string Libelle(this CauseDePerte cause) => cause switch
    {
        CauseDePerte.Pourriture => "Pourriture",
        CauseDePerte.RuptureDeFroid => "Rupture de froid",
        CauseDePerte.Manutention => "Manutention",
        _ => "Autre"
    };

    public static string Description(this CauseDePerte cause) => cause switch
    {
        CauseDePerte.Pourriture => "Flétrissement, moisissure, contamination fongique",
        CauseDePerte.RuptureDeFroid => "Écart thermique, arrêt ou panne du groupe froid",
        CauseDePerte.Manutention => "Écrasement, choc de caisse, transport interne",
        _ => "Dessèchement, vol ou toute autre cause"
    };
}
