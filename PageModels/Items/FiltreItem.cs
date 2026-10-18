using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StockFroid.PageModels.Items;

/// <summary>Pastille de filtre réutilisable (Toutes / En cours / Périmés…).</summary>
public partial class FiltreItem : ObservableObject
{
    private readonly Action<FiltreItem> _surChoix;

    public FiltreItem(string cle, string libelle, Action<FiltreItem> surChoix)
    {
        Cle = cle;
        _libelle = libelle;
        _surChoix = surChoix;
    }

    public string Cle { get; }

    [ObservableProperty]
    private string _libelle;

    [ObservableProperty]
    private bool _estActif;

    [RelayCommand]
    private void Choisir() => _surChoix(this);
}
