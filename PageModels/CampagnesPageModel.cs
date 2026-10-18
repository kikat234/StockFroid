using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFroid.PageModels.Items;
using StockFroid.Services;

namespace StockFroid.PageModels;

public partial class CampagnesPageModel : ObservableObject
{
    private readonly CampagneService _campagnes;
    private readonly BilanService _bilans;
    private readonly IAlertService _alertes;

    private List<CampagneItem> _toutes = new();
    private string _filtre = "toutes";

    public CampagnesPageModel(CampagneService campagnes, BilanService bilans, IAlertService alertes)
    {
        _campagnes = campagnes;
        _bilans = bilans;
        _alertes = alertes;

        Filtres = new ObservableCollection<FiltreItem>
        {
            new("toutes", "Toutes", ChoisirFiltre) { EstActif = true },
            new("encours", "En cours", ChoisirFiltre),
            new("cloturees", "Clôturées", ChoisirFiltre)
        };
    }

    public ObservableCollection<CampagneItem> Campagnes { get; } = new();

    public ObservableCollection<FiltreItem> Filtres { get; }

    [ObservableProperty]
    private string _recherche = string.Empty;

    [ObservableProperty]
    private bool _estOccupe;

    [ObservableProperty]
    private bool _estVide;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AErreur))]
    private string _erreur = string.Empty;

    [ObservableProperty]
    private string _campagnesActivesTexte = "00";

    [ObservableProperty]
    private string _campagnesTotalTexte = "/ 00 tot.";

    [ObservableProperty]
    private string _lotsActifsTexte = "0";

    public bool AErreur => !string.IsNullOrEmpty(Erreur);

    partial void OnRechercheChanged(string value) => AppliquerFiltre();

    public async Task ChargerAsync()
    {
        if (EstOccupe) return;

        EstOccupe = true;
        Erreur = string.Empty;
        try
        {
            var campagnes = await _campagnes.GetToutesAsync();
            var items = new List<CampagneItem>();
            var lotsActifs = 0;

            foreach (var c in campagnes)
            {
                var bilan = await _bilans.CalculerAsync(c.Id);
                if (c.EstEnCours) lotsActifs += bilan.LotsActifs;
                items.Add(CampagneItem.Creer(c, bilan));
            }

            _toutes = items;
            CampagnesActivesTexte = campagnes.Count(c => c.EstEnCours).ToString("00");
            CampagnesTotalTexte = $"/ {campagnes.Count:00} tot.";
            LotsActifsTexte = lotsActifs.ToString();

            Filtres[0].Libelle = $"Toutes ({items.Count})";
            Filtres[1].Libelle = $"En cours ({items.Count(i => !i.EstCloturee)})";
            Filtres[2].Libelle = $"Clôturées ({items.Count(i => i.EstCloturee)})";
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            Erreur = "Impossible de charger les campagnes.";
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
        IEnumerable<CampagneItem> resultat = _toutes;

        resultat = _filtre switch
        {
            "encours" => resultat.Where(c => !c.EstCloturee),
            "cloturees" => resultat.Where(c => c.EstCloturee),
            _ => resultat
        };

        if (!string.IsNullOrWhiteSpace(Recherche))
        {
            var terme = Recherche.Trim();
            resultat = resultat.Where(c => c.Nom.Contains(terme, StringComparison.CurrentCultureIgnoreCase));
        }

        Campagnes.Clear();
        foreach (var c in resultat) Campagnes.Add(c);

        EstVide = !EstOccupe && !AErreur && Campagnes.Count == 0;
    }

    [RelayCommand]
    private Task ReessayerAsync() => ChargerAsync();

    [RelayCommand]
    private async Task NouvelleCampagneAsync()
    {
        var nom = await _alertes.SaisirAsync(
            "Nouvelle campagne", "Nom de la campagne (ex. Campagne 2026 — Tomates)", "Nom de la campagne");
        if (nom is null) return;

        var resultat = await _campagnes.CreerAsync(nom);
        if (!resultat.Succes)
        {
            await _alertes.AfficherAsync("Création impossible", resultat.Message);
            return;
        }

        await ChargerAsync();
    }

    [RelayCommand]
    private async Task OuvrirCampagneAsync(CampagneItem? item)
    {
        if (item is null) return;
        await Shell.Current.GoToAsync("campagnedetail", new Dictionary<string, object> { ["campagneId"] = item.Id });
    }
}
