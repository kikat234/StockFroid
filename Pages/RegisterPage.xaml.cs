using StockFroid.PageModels;

namespace StockFroid.Pages;

public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }
}
