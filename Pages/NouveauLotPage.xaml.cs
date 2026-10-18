using StockFroid.PageModels;

namespace StockFroid.Pages;

public partial class NouveauLotPage : ContentPage
{
    private readonly NouveauLotPageModel _model;

    public NouveauLotPage(NouveauLotPageModel model)
    {
        InitializeComponent();
        BindingContext = _model = model;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _model.ChargerAsync();
    }
}
