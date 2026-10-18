using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFroid.Models;

namespace StockFroid.PageModels.Items;

/// <summary>Choix de cause de perte (bouton radio : un seul sélectionné à la fois).</summary>
public partial class CauseItem : ObservableObject
{
    private readonly Action<CauseItem> _surChoix;

    public CauseItem(CauseDePerte cause, Action<CauseItem> surChoix)
    {
        Cause = cause;
        _surChoix = surChoix;
    }

    public CauseDePerte Cause { get; }
    public string Libelle => Cause.Libelle();
    public string Description => Cause.Description();

    [ObservableProperty]
    private bool _estSelectionnee;

    [RelayCommand]
    private void Choisir() => _surChoix(this);
}
