using StockFroid.Models;

namespace StockFroid.Services;

/// <summary>État de session en mémoire : agent connecté et campagne active.</summary>
public class SessionService
{
    public string NomAgent { get; private set; } = "Agent";

    public string Affectation { get; private set; } = string.Empty;

    public int? AgentId { get; private set; }

    public bool EstConnecte => AgentId is not null;

    public int? CampagneActiveId { get; set; }

    public void Connecter(Agent agent)
    {
        AgentId = agent.Id;
        NomAgent = string.IsNullOrWhiteSpace(agent.NomComplet) ? "Agent" : agent.NomComplet.Trim();
        Affectation = agent.Affectation;
    }

    public void Deconnecter()
    {
        AgentId = null;
        NomAgent = "Agent";
        Affectation = string.Empty;
        CampagneActiveId = null;
    }
}
