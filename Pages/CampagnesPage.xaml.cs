using StockFroid.PageModels;

namespace StockFroid.Pages;

public partial class CampagnesPage : ContentPage
{
    private readonly CampagnesPageModel _model;

    public CampagnesPage(CampagnesPageModel model)
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
