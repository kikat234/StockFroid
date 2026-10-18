using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFroid.Models;
using StockFroid.PageModels.Items;
using StockFroid.Services;
using StockFroid.Utilities;

namespace StockFroid.PageModels;

public partial class CampagneDetailPageModel : ObservableObject, IQueryAttributable
{
    private readonly CampagneService _campagnes;
    private readonly LotService _lots;
    private readonly BilanService _bilans;
    private readonly SessionService _session;

    private List<LotItem> _tous = new();
    private string _filtre = "tous";

    public CampagneDetailPageModel(CampagneService campagnes, LotService lots, BilanService bilans, SessionService session)
    {
        _campagnes = campagnes;
        _lots = lots;
        _bilans = bilans;
        _session = session;

        Filtres = new ObservableCollection<FiltreItem>
        {
            new("tous", "Tous les lots", ChoisirFiltre) { EstActif = true },
            new("perimes", "Périmés", ChoisirFiltre),
            new("bientot", "Proche péremption", ChoisirFiltre),
            new("ajour", "À jour", ChoisirFiltre)
        };
    }

    public int CampagneId { get; private set; }

    public ObservableCollection<LotItem> Lots { get; } = new();

    public ObservableCollection<FiltreItem> Filtres { get; }

    [ObservableProperty]
    private string _titre = string.Empty;

    [ObservableProperty]
    private string _statutTexte = string.Empty;

    [ObservableProperty]
    private string _depuisTexte = string.Empty;

    [ObservableProperty]
    private string _stockActifTexte = "0 kg";

    [ObservableProperty]
    private string _lotsGeresTexte = "0 lot";

    [ObservableProperty]
    private string _alertesTexte = "Aucune";

    [ObservableProperty]
    private bool _aDesAlertes;

    [ObservableProperty]
    private string _affichesTexte = "0 affiché";

    [ObservableProperty]
    private bool _estOccupe;

    [ObservableProperty]
    private bool _estVide;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AErreur))]
    private string _erreur = string.Empty;

    public bool AErreur => !string.IsNullOrEmpty(Erreur);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("campagneId", out var valeur) && valeur is int id)
        {
            CampagneId = id;
            _ = ChargerAsync();
        }
    }

    public async Task ChargerAsync()
    {
        if (CampagneId == 0 || EstOccupe) return;

        EstOccupe = true;
        Erreur = string.Empty;
        try
        {
            var campagne = await _campagnes.GetAsync(CampagneId);
            if (campagne is null)
            {
                Erreur = "Campagne introuvable.";
                return;
            }

            var bilan = await _bilans.CalculerAsync(CampagneId);
            var lots = await _lots.GetLotsAvecStockAsync(CampagneId);
            var maintenant = DateTime.Now;

            // Tri par urgence : périmés, puis bientôt périmés, puis à jour.
            _tous = lots.Select(l => LotItem.Creer(l, maintenant))
                        .OrderBy(l => l.Gravite)
                        .ThenBy(l => l.DatePeremption)
                        .ToList();

            var alertes = bilan.NbPerimes + bilan.NbBientot;

            Titre = campagne.Nom;
            StatutTexte = campagne.EstEnCours ? "ACTIF" : "CLÔTURÉE";
            DepuisTexte = $"Depuis le {Fr.DateCourte(campagne.DateDebut)}";
            StockActifTexte = Fr.Quantite(bilan.StockRestant);
            LotsGeresTexte = $"{bilan.NbLots} lot{(bilan.NbLots > 1 ? "s" : "")}";
            ADesAlertes = alertes > 0;
            AlertesTexte = alertes == 0 ? "Aucune" : $"{alertes} critique{(alertes > 1 ? "s" : "")}";

            Filtres[0].Libelle = $"Tous les lots ({_tous.Count})";
            Filtres[1].Libelle = $"Périmés ({_tous.Count(l => l.Etat == EtatPeremption.Perime)})";
            Filtres[2].Libelle = $"Proche péremption ({_tous.Count(l => l.Etat == EtatPeremption.Bientot)})";
            Filtres[3].Libelle = $"À jour ({_tous.Count(l => l.Etat == EtatPeremption.AJour)})";
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            Erreur = "Impossible de charger les lots de cette campagne.";
        }
        finally
        {
            EstOccupe = false;
            AppliquerFiltre();
        }
    }

    private void ChoisirFiltre(FiltreItem filtre)
    {
        foreach (var f in Filtres) f.EstActif = f == filtre;
        _filtre = filtre.Cle;
        AppliquerFiltre();
    }

    private void AppliquerFiltre()
    {
        IEnumerable<LotItem> resultat = _tous;

        resultat = _filtre switch
        {
            "perimes" => resultat.Where(l => l.Etat == EtatPeremption.Perime),
            "bientot" => resultat.Where(l => l.Etat == EtatPeremption.Bientot),
            "ajour" => resultat.Where(l => l.Etat == EtatPeremption.AJour),
            _ => resultat
        };

        Lots.Clear();
        foreach (var l in resultat) Lots.Add(l);

        AffichesTexte = $"{Lots.Count} affiché{(Lots.Count > 1 ? "s" : "")}";
        EstVide = !EstOccupe && !AErreur && Lots.Count == 0;
    }

    [RelayCommand]
    private Task ReessayerAsync() => ChargerAsync();

    [RelayCommand]
    private async Task OuvrirLotAsync(LotItem? lot)
    {
        if (lot is null) return;
        await Shell.Current.GoToAsync("lotdetail", new Dictionary<string, object> { ["lotId"] = lot.Id });
    }

    [RelayCommand]
    private async Task NouveauLotAsync()
    {
        _session.CampagneActiveId = CampagneId;
        await Shell.Current.GoToAsync("//main/nouveaulot");
    }

    [RelayCommand]
    private async Task ConsulterBilanAsync()
    {
        _session.CampagneActiveId = CampagneId;
        await Shell.Current.GoToAsync("//main/bilan");
    }
}
