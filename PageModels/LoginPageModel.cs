using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFroid.Services;

namespace StockFroid.PageModels;

public partial class LoginPageModel : ObservableObject
{
    private readonly AuthService _auth;
    private readonly IAlertService _alertes;

    public LoginPageModel(AuthService auth, IAlertService alertes)
    {
        _auth = auth;
        _alertes = alertes;
    }

    [ObservableProperty]
    private string _identifiant = string.Empty;

    [ObservableProperty]
    private string _motDePasse = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TexteBoutonAffichage))]
    private bool _motDePasseMasque = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AErreur))]
    private string _erreur = string.Empty;

    public bool AErreur => !string.IsNullOrEmpty(Erreur);

    public string TexteBoutonAffichage => MotDePasseMasque ? "Afficher" : "Masquer";

    [RelayCommand]
    private void BasculerMotDePasse() => MotDePasseMasque = !MotDePasseMasque;

    [ObservableProperty]
    private bool _estOccupe;

    [RelayCommand]
    private async Task SeConnecterAsync()
    {
        if (EstOccupe) return;

        EstOccupe = true;
        Erreur = string.Empty;
        try
        {
            var resultat = await _auth.ConnecterAsync(Identifiant, MotDePasse);
            if (!resultat.Succes)
            {
                Erreur = resultat.Message;
                return;
            }

            MotDePasse = string.Empty;
            await Shell.Current.GoToAsync("//main/campagnes");
        }
        finally
        {
            EstOccupe = false;
        }
    }

    [RelayCommand]
    private Task OuvrirInscriptionAsync() => Shell.Current.GoToAsync("register");

    [RelayCommand]
    private Task MotDePasseOublieAsync() =>
        _alertes.AfficherAsync("Récupération", "La récupération du code agent sera disponible dans une prochaine version.");
}
