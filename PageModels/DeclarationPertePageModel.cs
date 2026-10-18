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

public partial class DeclarationPertePageModel : ObservableObject, IQueryAttributable
{
    private readonly LotService _lots;
    private readonly PerteService _pertes;
    private double _stockRestant;

    public DeclarationPertePageModel(LotService lots, PerteService pertes)
    {
        _lots = lots;
        _pertes = pertes;

        Causes = new ObservableCollection<CauseItem>(
            Enum.GetValues<CauseDePerte>().Select(c => new CauseItem(c, ChoisirCause)));
        Causes[0].EstSelectionnee = true;
    }

    public int LotId { get; private set; }

    public ObservableCollection<CauseItem> Causes { get; }

    public DateTime DateMax => DateTime.Today;

    [ObservableProperty] private DateTime _dateMin = DateTime.Today.AddYears(-1);
    [ObservableProperty] private DateTime _dateConstat = DateTime.Today;
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _produit = string.Empty;
    [ObservableProperty] private string _lieuTexte = string.Empty;
    [ObservableProperty] private string _stockTexte = string.Empty;
    [ObservableProperty] private string _quantiteTexte = string.Empty;
    [ObservableProperty] private string _commentaire = string.Empty;
    [ObservableProperty] private string _stockEstimeTexte = "—";
    [ObservableProperty] private Color _stockEstimeCouleur = Palette.Ok;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeutValider))]
    private bool _estOccupe;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AErreur))]
    private string _erreur = string.Empty;

    public bool AErreur => !string.IsNullOrEmpty(Erreur);

    public bool PeutValider => !EstOccupe;

    partial void OnQuantiteTexteChanged(string value) => MettreAJourStockEstime();

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
        if (LotId == 0) return;

        try
        {
            var lotStock = await _lots.GetLotAvecStockAsync(LotId);
            if (lotStock is null)
            {
                Erreur = "Lot introuvable.";
                return;
            }

            _stockRestant = lotStock.Restant;
            Code = $"Lot #{lotStock.Lot.Code}";
            Produit = lotStock.Lot.Produit;
            LieuTexte = lotStock.Lot.LieuStockage;
            StockTexte = Fr.Quantite(lotStock.Restant);
            DateMin = lotStock.Lot.DateRecolte.Date;
            MettreAJourStockEstime();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            Erreur = "Impossible de charger le lot.";
        }
    }

    private void ChoisirCause(CauseItem choisie)
    {
        foreach (var c in Causes) c.EstSelectionnee = c == choisie;
    }

    /// <summary>Affiche en direct le stock restant estimé après déduction de la perte saisie.</summary>
    private void MettreAJourStockEstime()
    {
        if (!Fr.TryParseDouble(QuantiteTexte, out var quantite) || quantite <= 0)
        {
            StockEstimeTexte = Fr.Quantite(_stockRestant);
            StockEstimeCouleur = Palette.Ok;
            return;
        }

        if (quantite > _stockRestant)
        {
            StockEstimeTexte = "Dépasse le stock";
            StockEstimeCouleur = Palette.Danger;
            return;
        }

        StockEstimeTexte = Fr.Quantite(_stockRestant - quantite);
        StockEstimeCouleur = Palette.Ok;
    }

    [RelayCommand]
    private async Task ValiderAsync()
    {
        if (EstOccupe) return;
        Erreur = string.Empty;

        if (!Fr.TryParseDouble(QuantiteTexte, out var quantite))
        {
            Erreur = "Saisissez une quantité valide.";
            return;
        }

        var cause = Causes.FirstOrDefault(c => c.EstSelectionnee);
        if (cause is null)
        {
            Erreur = "Choisissez une cause.";
            return;
        }

        EstOccupe = true;
        try
        {
            // Back-end : les règles métier (stock restant, dates) sont dans PerteService.
            var resultat = await _pertes.DeclarerPerteAsync(LotId, DateConstat, quantite, cause.Cause, Commentaire);
            if (!resultat.Succes)
            {
                Erreur = resultat.Message;
                return;
            }

            await Shell.Current.GoToAsync("..");
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

    [RelayCommand]
    private Task AnnulerAsync() => Shell.Current.GoToAsync("..");
}
