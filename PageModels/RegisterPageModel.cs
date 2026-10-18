using System.Collections.ObjectModel;
using System.Net.Mail;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFroid.Services;

namespace StockFroid.PageModels;

public partial class RegisterPageModel : ObservableObject
{
    private static readonly Dictionary<string, string> Affectations = new()
    {
        ["Entrepôt Nord – Zone maraîchère"] = "+2,0 °C / +4,0 °C",
        ["Entrepôt Centre – Tubercules et plantains"] = "Stockage ventilé (sans froid)",
        ["Magasin de coopérative – Céréales"] = "Stockage sec et ventilé"
    };

    private readonly SessionService _session;

    public RegisterPageModel(SessionService session) => _session = session;

    public List<string> ListeAffectations { get; } = Affectations.Keys.ToList();

    public ObservableCollection<string> Erreurs { get; } = new();

    [ObservableProperty]
    private string _nomComplet = string.Empty;

    [ObservableProperty]
    private string _matricule = string.Empty;

    [ObservableProperty]
    private string? _affectationChoisie;

    [ObservableProperty]
    private string _temperatureTexte = "—";

    [ObservableProperty]
    private string _motDePasse = string.Empty;

    [ObservableProperty]
    private string _confirmation = string.Empty;

    [ObservableProperty]
    private bool _attestationAcceptee;

    [ObservableProperty]
    private bool _aDesErreurs;

    partial void OnAffectationChoisieChanged(string? value) =>
        TemperatureTexte = value is not null && Affectations.TryGetValue(value, out var t) ? t : "—";

    [RelayCommand]
    private async Task CreerCompteAsync()
    {
        Erreurs.Clear();

        if (string.IsNullOrWhiteSpace(NomComplet))
            Erreurs.Add("Le nom et le prénom sont obligatoires.");

        if (string.IsNullOrWhiteSpace(Matricule))
            Erreurs.Add("Le matricule ou l'e-mail professionnel est obligatoire.");
        else if (Matricule.Contains('@') && !MailAddress.TryCreate(Matricule.Trim(), out _))
            Erreurs.Add("L'adresse e-mail n'est pas valide.");

        if (AffectationChoisie is null)
            Erreurs.Add("Choisissez votre affectation.");

        if (MotDePasse.Length < 8)
            Erreurs.Add("Le mot de passe doit comporter au moins 8 caractères.");
        else if (MotDePasse != Confirmation)
            Erreurs.Add("Les deux mots de passe ne correspondent pas.");

        if (!AttestationAcceptee)
            Erreurs.Add("Vous devez attester avoir pris connaissance du protocole.");

        ADesErreurs = Erreurs.Count > 0;
        if (ADesErreurs) return;

        // Front-end uniquement : le compte n'est pas encore enregistré côté serveur.
        _session.Connecter(NomComplet);
        await Shell.Current.GoToAsync("//main/campagnes");
    }

    [RelayCommand]
    private Task RetourConnexionAsync() => Shell.Current.GoToAsync("..");
}
