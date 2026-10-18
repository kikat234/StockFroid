using StockFroid.PageModels;

namespace StockFroid.Pages;

public partial class LotDetailPage : ContentPage
{
    private readonly LotDetailPageModel _model;

    public LotDetailPage(LotDetailPageModel model)
    {
        InitializeComponent();
        BindingContext = _model = model;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _model.ChargerAsync(); // recharge au retour de la déclaration de perte
    }
}
