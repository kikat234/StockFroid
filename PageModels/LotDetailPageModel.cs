using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;
using StockFroid.Models;
using StockFroid.PageModels.Items;
using StockFroid.Services;
using StockFroid.Utilities;

namespace StockFroid.PageModels;

public partial class LotDetailPageModel : ObservableObject, IQueryAttributable
{
    private readonly LotService _lots;
    private readonly PerteService _pertes;
    private readonly CampagneService _campagnes;

    public LotDetailPageModel(LotService lots, PerteService pertes, CampagneService campagnes)
    {
        _lots = lots;
        _pertes = pertes;
        _campagnes = campagnes;
    }

    public int LotId { get; private set; }

    public ObservableCollection<PerteItem> Pertes { get; } = new();

    [ObservableProperty] private string _bandeauTitre = string.Empty;
    [ObservableProperty] private string _bandeauMessage = string.Empty;
    [ObservableProperty] private Color _bandeauFond = Colors.Transparent;
    [ObservableProperty] private Color _bandeauCouleur = Colors.Black;

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _produit = string.Empty;
    [ObservableProperty] private string _campagneNom = string.Empty;
    [ObservableProperty] private string _lieuTexte = string.Empty;
    [ObservableProperty] private string _stockNetTexte = string.Empty;
    [ObservableProperty] private string _stockInitialTexte = string.Empty;
    [ObservableProperty] private string _pertesTexte = string.Empty;
    [ObservableProperty] private string _recolteTexte = string.Empty;
    [ObservableProperty] private string _dureeTexte = string.Empty;
    [ObservableProperty] private string _peremptionTexte = string.Empty;
    [ObservableProperty] private Color _peremptionCouleur = Colors.Black;
    [ObservableProperty] private string _historiqueTitre = "Historique des pertes";
    [ObservableProperty] private bool _aDesPertes;
    [ObservableProperty] private bool _aucunePerte;
    [ObservableProperty] private bool _peutDeclarer;
    [ObservableProperty] private bool _estOccupe;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AErreur))]
    private string _erreur = string.Empty;

    public bool AErreur => !string.IsNullOrEmpty(Erreur);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("lotId", out var valeur) && valeur is int id)
        {
            LotId = id;
            _ = ChargerAsync();
        }
    }

    public async Task ChargerAsync()
    {
        if (LotId == 0 || EstOccupe) return;

        EstOccupe = true;
        Erreur = string.Empty;
        try
        {
            var lotStock = await _lots.GetLotAvecStockAsync(LotId);
            if (lotStock is null)
            {
                Erreur = "Lot introuvable.";
                return;
            }

            var lot = lotStock.Lot;
            var campagne = await _campagnes.GetAsync(lot.CampagneId);
            var pertes = await _pertes.GetPertesDuLotAsync(LotId);

            var maintenant = DateTime.Now;
            var date = lot.DatePeremption;
            var etat = PeremptionCalculator.Evaluer(date, maintenant);
            var jours = PeremptionCalculator.JoursRestants(date, maintenant);

            switch (etat)
            {
                case EtatPeremption.Perime:
                    BandeauTitre = $"✖ Périmé depuis le {Fr.Date(date)} (J+{-jours})";
                    BandeauMessage = "La date limite de conservation est dépassée. Triage immédiat ou réexpédition requis.";
                    BandeauFond = Palette.DangerFond;
                    BandeauCouleur = Palette.Danger;
                    break;
                case EtatPeremption.Bientot:
                    BandeauTitre = jours == 0
                        ? "⚠ Expire aujourd'hui"
                        : $"⚠ Expire dans {Fr.Jours(jours)} ({Fr.Date(date)})";
                    BandeauMessage = "Prévoir la vente ou la transformation de ce lot en priorité.";
                    BandeauFond = Palette.AttentionFond;
                    BandeauCouleur = Palette.Attention;
                    break;
                default:
                    BandeauTitre = $"✓ Lot conforme — {Fr.Jours(jours)} restants";
                    BandeauMessage = "Aucune action requise pour le moment.";
                    BandeauFond = Palette.OkFond;
                    BandeauCouleur = Palette.Ok;
                    break;
            }

            Code = $"#{lot.Code}";
            Produit = lot.Produit;
            CampagneNom = campagne?.Nom ?? string.Empty;
            LieuTexte = lot.LieuStockage;
            StockNetTexte = Fr.Quantite(lotStock.Restant);
            StockInitialTexte = $"/ {Fr.Quantite(lot.QuantiteInitiale)} initial";
            PertesTexte = lotStock.Perdu > 0 ? $"-{Fr.Quantite(lotStock.Perdu)}" : "Aucune";
            RecolteTexte = Fr.DateLongue(lot.DateRecolte);
            DureeTexte = Fr.Jours(lot.DureeConservationJours);
            PeremptionTexte = Fr.DateLongue(date);
            PeremptionCouleur = BandeauCouleur;

            Pertes.Clear();
            foreach (var p in pertes) Pertes.Add(PerteItem.Creer(p));
            ADesPertes = pertes.Count > 0;
            AucunePerte = pertes.Count == 0;
            HistoriqueTitre = $"Historique des pertes ({pertes.Count} constat{(pertes.Count > 1 ? "s" : "")})";
            PeutDeclarer = lotStock.Restant > 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            Erreur = "Impossible de charger ce lot.";
        }
        finally
        {
            EstOccupe = false;
        }
    }

    [RelayCommand]
    private Task ReessayerAsync() => ChargerAsync();

    [RelayCommand]
    private async Task DeclarerPerteAsync()
    {
        await Shell.Current.GoToAsync("declarationperte", new Dictionary<string, object> { ["lotId"] = LotId });
    }
}
