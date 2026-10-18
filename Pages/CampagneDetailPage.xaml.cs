using StockFroid.PageModels;

namespace StockFroid.Pages;

public partial class CampagneDetailPage : ContentPage
{
    private readonly CampagneDetailPageModel _model;

    public CampagneDetailPage(CampagneDetailPageModel model)
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
