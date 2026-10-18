using StockFroid.PageModels;

namespace StockFroid.Pages;

public partial class BilanPage : ContentPage
{
    private readonly BilanPageModel _model;

    public BilanPage(BilanPageModel model)
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
