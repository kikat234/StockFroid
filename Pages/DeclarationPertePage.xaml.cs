using StockFroid.PageModels;

namespace StockFroid.Pages;

public partial class DeclarationPertePage : ContentPage
{
    public DeclarationPertePage(DeclarationPertePageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }
}
