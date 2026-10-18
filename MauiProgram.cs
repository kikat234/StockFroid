using Microsoft.Extensions.Logging;
using StockFroid.Data;
using StockFroid.PageModels;
using StockFroid.Pages;
using StockFroid.Services;

namespace StockFroid;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Données et services (partie back-end locale)
        builder.Services.AddSingleton<StockFroidDatabase>();
        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<IAlertService, AlertService>();
        builder.Services.AddSingleton<CampagneService>();
        builder.Services.AddSingleton<LotService>();
        builder.Services.AddSingleton<PerteService>();
        builder.Services.AddSingleton<BilanService>();
        builder.Services.AddSingleton<CampagneSelector>();

        // MVVM : chaque page reçoit son PageModel par injection de dépendances
        builder.Services.AddTransient<LoginPageModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPageModel>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<CampagnesPageModel>();
        builder.Services.AddTransient<CampagnesPage>();
        builder.Services.AddTransient<CampagneDetailPageModel>();
        builder.Services.AddTransient<CampagneDetailPage>();
        builder.Services.AddTransient<NouveauLotPageModel>();
        builder.Services.AddTransient<NouveauLotPage>();
        builder.Services.AddTransient<LotDetailPageModel>();
        builder.Services.AddTransient<LotDetailPage>();
        builder.Services.AddTransient<DeclarationPertePageModel>();
        builder.Services.AddTransient<DeclarationPertePage>();
        builder.Services.AddTransient<BilanPageModel>();
        builder.Services.AddTransient<BilanPage>();
        builder.Services.AddTransient<ProfilPageModel>();
        builder.Services.AddTransient<ProfilPage>();

        return builder.Build();
    }
}
