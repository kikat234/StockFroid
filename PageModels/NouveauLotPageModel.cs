using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFroid.Models;
using StockFroid.Services;
using StockFroid.Utilities;

namespace StockFroid.PageModels;

public partial class NouveauLotPageModel : ObservableObject
{
    private readonly LotService _lots;
    private readonly CampagneSelector _selector;
    private readonly IAlertService _alertes;

    public NouveauLotPageModel(LotService lots, CampagneSelector selector, IAlertService alertes)
    {
        _lots = lots;
        _selector = selector;
        _alertes = alertes;
        MettreAJourPeremption();
    }

    public List<string> Produits { get; } = new()
    {
        "Tomate Roma", "Tomate Cœur de Bœuf", "Tomate Cerise", "Banane plantain",
        "Oignon", "Pomme de terre", "Igname", "Manioc", "Maïs", "Autre produit"
    };

    public List<string> Unites { get; } = new() { "kg", "t" };

    public List<string> Lieux { get; } = new()
    {
        "Chambre froide A1", "Chambre froide A2", "Chambre froide B2",
        "Hangar ventilé", "Entrepôt sec", "Dépôt de marché", "Grenier / magasin"
    };

    public int CampagneId { get; private set; }

    public DateTime DateMax => DateTime.Today;

    [ObservableProperty]
    private string _campagneNom = "Aucune campagne en cours";

    [ObservableProperty]
    private string? _produit;

    [ObservableProperty]
    private DateTime _dateRecolte = DateTime.Today;

    [ObservableProperty]
    private string _dureeTexte = "14";

    [ObservableProperty]
    private string _quantiteTexte = string.Empty;

    [ObservableProperty]
    private string _unite = "kg";

    [ObservableProperty]
    private string? _lieu;

    [ObservableProperty]
    private string _datePeremptionTexte = "—";

    [ObservableProperty]
    private string _dansTexte = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeutEnregistrer))]
    private bool _estOccupe;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AErreur))]
    private string _erreur = string.Empty;

    public bool AErreur => !string.IsNullOrEmpty(Erreur);

    public bool PeutEnregistrer => !EstOccupe;

    partial void OnDateRecolteChanged(DateTime value) => MettreAJourPeremption();

    partial void OnDureeTexteChanged(string value) => MettreAJourPeremption();

    /// <summary>Recalcule en direct la date de péremption prévisionnelle (fonction clé de l'écran).</summary>
    private void MettreAJourPeremption()
    {
        if (int.TryParse(DureeTexte, out var duree) && duree > 0 && duree <= 730)
        {
            var date = PeremptionCalculator.Calculer(DateRecolte, duree);
            var jours = PeremptionCalculator.JoursRestants(date, DateTime.Now);

            DatePeremptionTexte = Fr.DateLongue(date);
            DansTexte = jours >= 0
                ? $"(dans {Fr.Jours(jours)})"
                : $"(périmé depuis {Fr.Jours(-jours)})";
        }
        else
        {
            DatePeremptionTexte = "—";
            DansTexte = "Durée invalide (1 à 730 jours)";
        }
    }

    public async Task ChargerAsync()
    {
        try
        {
            var campagne = await _selector.ObtenirActiveAsync(seulementEnCours: true);
            CampagneId = campagne?.Id ?? 0;
            CampagneNom = campagne?.Nom ?? "Aucune campagne en cours";
            Erreur = campagne is null
                ? "Créez d'abord une campagne depuis l'onglet Campagnes."
                : string.Empty;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            Erreur = "Impossible de charger la campagne active.";
        }
    }

    [RelayCommand]
    private async Task ChangerCampagneAsync()
    {
        var campagne = await _selector.ProposerChangementAsync(seulementEnCours: true);
        if (campagne is null) return;

        CampagneId = campagne.Id;
        CampagneNom = campagne.Nom;
        Erreur = string.Empty;
    }

    [RelayCommand]
    private void AugmenterDuree()
    {
        var duree = int.TryParse(DureeTexte, out var d) ? d : 0;
        DureeTexte = Math.Min(duree + 1, 730).ToString();
    }

    [RelayCommand]
    private void DiminuerDuree()
    {
        var duree = int.TryParse(DureeTexte, out var d) ? d : 2;
        DureeTexte = Math.Max(duree - 1, 1).ToString();
    }

    [RelayCommand]
    private void DefinirDuree(string? jours)
    {
        if (!string.IsNullOrWhiteSpace(jours)) DureeTexte = jours;
    }

    [RelayCommand]
    private async Task EnregistrerAsync()
    {
        if (EstOccupe) return;
        Erreur = string.Empty;

        if (!int.TryParse(DureeTexte, out var duree))
        {
            Erreur = "Saisissez une durée de conservation valide (en jours).";
            return;
        }

        if (!Fr.TryParseDouble(QuantiteTexte, out var quantite))
        {
            Erreur = "Saisissez une quantité valide.";
            return;
        }

        // Le stock est toujours conservé en kg : 1 tonne = 1 000 kg.
        var quantiteKg = Unite == "t" ? quantite * 1000 : quantite;

        EstOccupe = true;
        try
        {
            var resultat = await _lots.CreerLotAsync(
                CampagneId, Produit ?? string.Empty, DateRecolte, duree, quantiteKg, "kg", Lieu ?? string.Empty);

            if (!resultat.Succes)
            {
                Erreur = resultat.Message;
                return;
            }

            await _alertes.AfficherAsync("Lot enregistré", $"Le lot {resultat.Valeur!.Code} a bien été enregistré.");
            var campagneId = CampagneId;
            Reinitialiser();

            await Shell.Current.GoToAsync("//main/campagnes");
            await Shell.Current.GoToAsync("campagnedetail",
                new Dictionary<string, object> { ["campagneId"] = campagneId });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            Erreur = "Une erreur est survenue pendant l'enregistrement.";
        }
        finally
        {
            EstOccupe = false;
        }
    }

    private void Reinitialiser()
    {
        Produit = null;
        DateRecolte = DateTime.Today;
        DureeTexte = "14";
        QuantiteTexte = string.Empty;
        Unite = "kg";
        Lieu = null;
    }
}
