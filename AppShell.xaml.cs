using StockFroid.Pages;

namespace StockFroid;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pages ouvertes « par-dessus » les onglets (avec bouton retour).
        Routing.RegisterRoute("register", typeof(RegisterPage));
        Routing.RegisterRoute("campagnedetail", typeof(CampagneDetailPage));
        Routing.RegisterRoute("lotdetail", typeof(LotDetailPage));
        Routing.RegisterRoute("declarationperte", typeof(DeclarationPertePage));
    }
}
