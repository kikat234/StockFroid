using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;
using StockFroid.PageModels.Items;
using StockFroid.Services;
using StockFroid.Utilities;

namespace StockFroid.PageModels;

public partial class BilanPageModel : ObservableObject
{
    private const double SeuilCritique = BilanCampagne.SeuilTauxCritique;

    private readonly BilanService _bilans;
    private readonly CampagneSelector _selector;
    private readonly IAlertService _alertes;
    private readonly ExportService _export;
    private int _campagneId;

    public BilanPageModel(BilanService bilans, CampagneSelector selector, IAlertService alertes, ExportService export)
    {
        _bilans = bilans;
        _selector = selector;
        _alertes = alertes;
        _export = export;
    }

    public ObservableCollection<RepartitionItem> Repartition { get; } = new();

    public ObservableCollection<LotCritiqueItem> LotsCritiques { get; } = new();

    [ObservableProperty] private bool _aCampagne;
    [ObservableProperty] private string _campagneNom = "—";
    [ObservableProperty] private string _stockBrutTexte = "0 kg";
    [ObservableProperty] private string _soldeTexte = "0 kg";
    [ObservableProperty] private string _soldePartTexte = string.Empty;
    [ObservableProperty] private string _tauxTexte = "0,0 %";
    [ObservableProperty] private string _rebutTexte = "0 kg de rebut";
    [ObservableProperty] private double _tauxProgression;
    [ObservableProperty] private Color _tauxCouleur = Palette.Ok;
    [ObservableProperty] private string _tauxStatut = string.Empty;
    [ObservableProperty] private bool _aDesLotsCritiques;
    [ObservableProperty] private string _noteTexte = string.Empty;
    [ObservableProperty] private bool _estOccupe;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AErreur))]
    private string _erreur = string.Empty;

    public bool AErreur => !string.IsNullOrEmpty(Erreur);

    public async Task ChargerAsync()
    {
        if (EstOccupe) return;

        EstOccupe = true;
        Erreur = string.Empty;
        try
        {
            var campagne = await _selector.ObtenirActiveAsync(seulementEnCours: false);
            ACampagne = campagne is not null;
            if (campagne is null) return;

            _campagneId = campagne.Id;
            CampagneNom = campagne.Nom;

            var bilan = await _bilans.CalculerAsync(_campagneId);

            StockBrutTexte = Fr.Quantite(bilan.StockInitial);
            SoldeTexte = Fr.Quantite(bilan.StockRestant);
            SoldePartTexte = $"{Fr.Pourcent(1 - bilan.TauxPerte)} valorisable";
            TauxTexte = Fr.Pourcent(bilan.TauxPerte);
            RebutTexte = $"{Fr.Quantite(bilan.TotalPerdu)} de rebut";
            TauxProgression = Math.Clamp(bilan.TauxPerte / SeuilCritique, 0, 1);

            var critique = bilan.TauxPerte >= SeuilCritique;
            TauxCouleur = critique ? Palette.Danger : Palette.Ok;
            TauxStatut = critique
                ? $"Seuil critique dépassé (≥ {Fr.Pourcent(SeuilCritique)})"
                : $"Conforme (< {Fr.Pourcent(SeuilCritique)})";

            Repartition.Clear();
            foreach (var r in bilan.Repartition) Repartition.Add(RepartitionItem.Creer(r));

            LotsCritiques.Clear();
            foreach (var c in bilan.LotsCritiques) LotsCritiques.Add(LotCritiqueItem.Creer(c));
            ADesLotsCritiques = LotsCritiques.Count > 0;

            NoteTexte = $"Calculé localement via {bilan.NbLots} fiche{(bilan.NbLots > 1 ? "s" : "")} de lots " +
                        $"et {bilan.NbConstats} constat{(bilan.NbConstats > 1 ? "s" : "")} de pertes.";
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            Erreur = "Impossible de calculer le bilan.";
        }
        finally
        {
            EstOccupe = false;
        }
    }

    [RelayCommand]
    private Task ReessayerAsync() => ChargerAsync();

    [RelayCommand]
    private async Task ChangerCampagneAsync()
    {
        var campagne = await _selector.ProposerChangementAsync(seulementEnCours: false);
        if (campagne is not null) await ChargerAsync();
    }

    [RelayCommand]
    private async Task GenererRapportAsync()
    {
        if (EstOccupe || !ACampagne) return;

        var choix = await _alertes.ChoisirAsync("Format du rapport", new[] { "PDF", "CSV (Excel)" });
        if (choix is null) return;

        var format = choix.StartsWith("PDF", StringComparison.Ordinal) ? FormatExport.Pdf : FormatExport.Csv;

        EstOccupe = true;
        try
        {
            var resultat = await _export.ExporterAsync(_campagneId, format);
            if (!resultat.Succes || resultat.Valeur is null)
            {
                await _alertes.AfficherAsync("Export impossible", resultat.Message);
                return;
            }

            // Windows : on laisse le choix entre enregistrer le fichier et le partager.
            var action = "Partager";
            if (ExportService.EnregistrementDisponible)
            {
                var choixAction = await _alertes.ChoisirAsync("Rapport prêt", new[] { "Enregistrer sous…", "Partager" });
                if (choixAction is null) return;
                action = choixAction;
            }

            if (action.StartsWith("Enregistrer", StringComparison.Ordinal))
            {
                var enregistre = await _export.EnregistrerSousAsync(resultat.Valeur);
                if (!enregistre.Succes)
                    await _alertes.AfficherAsync("Enregistrement impossible", enregistre.Message);
                else if (enregistre.Valeur is not null)
                    await _alertes.AfficherAsync("Rapport enregistré", enregistre.Valeur);
                return;
            }

            var partage = await _export.PartagerAsync(resultat.Valeur, $"Bilan {CampagneNom}");
            if (!partage.Succes)
                await _alertes.AfficherAsync("Rapport créé", partage.Message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await _alertes.AfficherAsync("Export impossible", "Le rapport n'a pas pu être généré.");
        }
        finally
        {
            EstOccupe = false;
        }
    }
}