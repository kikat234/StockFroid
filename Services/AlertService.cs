namespace StockFroid.Services;

/// <summary>Abstraction des boîtes de dialogue, pour que les PageModels ne dépendent pas de l'interface.</summary>
public interface IAlertService
{
    Task AfficherAsync(string titre, string message);
    Task<bool> ConfirmerAsync(string titre, string message, string accepter = "Oui", string annuler = "Non");
    Task<string?> SaisirAsync(string titre, string message, string placeholder = "");
    Task<string?> ChoisirAsync(string titre, IReadOnlyList<string> options);
}

public class AlertService : IAlertService
{
    public Task AfficherAsync(string titre, string message) =>
        Shell.Current.DisplayAlert(titre, message, "OK");

    public Task<bool> ConfirmerAsync(string titre, string message, string accepter = "Oui", string annuler = "Non") =>
        Shell.Current.DisplayAlert(titre, message, accepter, annuler);

    public async Task<string?> SaisirAsync(string titre, string message, string placeholder = "")
    {
        var saisie = await Shell.Current.DisplayPromptAsync(titre, message, "Valider", "Annuler", placeholder);
        return saisie;
    }

    public async Task<string?> ChoisirAsync(string titre, IReadOnlyList<string> options)
    {
        var choix = await Shell.Current.DisplayActionSheet(titre, "Annuler", null, options.ToArray());
        return string.IsNullOrEmpty(choix) || choix == "Annuler" ? null : choix;
    }
}
