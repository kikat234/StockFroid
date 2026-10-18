using Microsoft.Maui.ApplicationModel;

namespace StockFroid;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Interface claire uniquement (palette de la maquette), quel que soit le thème du téléphone.
        UserAppTheme = AppTheme.Light;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new Window(new AppShell());
}
