using System;
using System.IO;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace StockFroid;

public partial class App : Application
{
    public App()
    {
        try
        {
            InitializeComponent();
            // Interface claire uniquement (palette de la maquette), quel que soit le thème du téléphone.
            UserAppTheme = AppTheme.Light;
        }
        catch (Exception ex)
        {
            // Loger l'exception au démarrage pour diagnostic
            try
            {
                var path = Path.Combine(FileSystem.AppDataDirectory, "startup_error.txt");
                File.WriteAllText(path, ex.ToString());
            }
            catch { /* ignore */ }
        }
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        CreateWindowSafe();

    private Window CreateWindowSafe()
    {
        try
        {
            return new Window(new AppShell());
        }
        catch (Exception ex)
        {
            try
            {
                var path = Path.Combine(FileSystem.AppDataDirectory, "startup_error.txt");
                File.WriteAllText(path, ex.ToString());
            }
            catch { }

            // Retourne une fenêtre minimale affichant l'erreur afin que l'app reste démarrée.
            var page = new ContentPage
            {
                Content = new ScrollView
                {
                    Content = new Label { Text = "Erreur de démarrage : " + ex.Message }
                }
            };
            return new Window(page);
        }
    }
}
