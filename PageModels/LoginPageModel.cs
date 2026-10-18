using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFroid.Services;

namespace StockFroid.PageModels;

public partial class LoginPageModel : ObservableObject
{
    private readonly SessionService _session;
    private readonly IAlertService _alertes;

    public LoginPageModel(SessionService session, IAlertService alertes)
    {
        _session = session;
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

    [RelayCommand]
    private async Task SeConnecterAsync()
    {
        // Front-end uniquement : l'authentification serveur n'est pas implémentée.
        if (string.IsNullOrWhiteSpace(Identifiant) || string.IsNullOrWhiteSpace(MotDePasse))
        {
            Erreur = "Renseignez votre identifiant et votre mot de passe.";
            return;
        }

        Erreur = string.Empty;
        _session.Connecter(Identifiant);
        MotDePasse = string.Empty;
        await Shell.Current.GoToAsync("//main/campagnes");
    }

    [RelayCommand]
    private Task OuvrirInscriptionAsync() => Shell.Current.GoToAsync("register");

    [RelayCommand]
    private Task MotDePasseOublieAsync() =>
        _alertes.AfficherAsync("Récupération", "La récupération du code agent sera disponible dans une prochaine version.");
}
