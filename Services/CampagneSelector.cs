using StockFroid.Models;

namespace StockFroid.Services;

/// <summary>Gère la « campagne active » partagée par les onglets Nouveau lot et Pertes.</summary>
public class CampagneSelector
{
    private readonly CampagneService _campagnes;
    private readonly SessionService _session;
    private readonly IAlertService _alertes;

    public CampagneSelector(CampagneService campagnes, SessionService session, IAlertService alertes)
    {
        _campagnes = campagnes;
        _session = session;
        _alertes = alertes;
    }

    public async Task<Campagne?> ObtenirActiveAsync(bool seulementEnCours)
    {
        var toutes = await _campagnes.GetToutesAsync();
        var candidates = seulementEnCours ? toutes.Where(c => c.EstEnCours).ToList() : toutes;
        if (candidates.Count == 0) return null;

        var active = candidates.FirstOrDefault(c => c.Id == _session.CampagneActiveId) ?? candidates[0];
        _session.CampagneActiveId = active.Id;
        return active;
    }

    public async Task<Campagne?> ProposerChangementAsync(bool seulementEnCours)
    {
        var toutes = await _campagnes.GetToutesAsync();
        var candidates = seulementEnCours ? toutes.Where(c => c.EstEnCours).ToList() : toutes;
        if (candidates.Count == 0)
        {
            await _alertes.AfficherAsync("Aucune campagne", "Créez d'abord une campagne depuis l'onglet Campagnes.");
            return null;
        }

        var choix = await _alertes.ChoisirAsync("Choisir une campagne", candidates.Select(c => c.Nom).ToList());
        var elue = candidates.FirstOrDefault(c => c.Nom == choix);
        if (elue is not null) _session.CampagneActiveId = elue.Id;
        return elue;
    }
}
