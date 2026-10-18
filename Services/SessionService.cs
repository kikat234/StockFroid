namespace StockFroid.Services;

/// <summary>État de session en mémoire : agent connecté et campagne active.</summary>
public class SessionService
{
    public string NomAgent { get; private set; } = "Agent";

    public bool EstConnecte { get; private set; }

    public int? CampagneActiveId { get; set; }

    public void Connecter(string nom)
    {
        NomAgent = string.IsNullOrWhiteSpace(nom) ? "Agent" : nom.Trim();
        EstConnecte = true;
    }

    public void Deconnecter()
    {
        EstConnecte = false;
        CampagneActiveId = null;
    }
}
