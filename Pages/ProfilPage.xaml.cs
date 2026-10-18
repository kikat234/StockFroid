using StockFroid.PageModels;

namespace StockFroid.Pages;

public partial class ProfilPage : ContentPage
{
    private readonly ProfilPageModel _model;

    public ProfilPage(ProfilPageModel model)
    {
        InitializeComponent();
        BindingContext = _model = model;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _model.Charger();
    }
}
