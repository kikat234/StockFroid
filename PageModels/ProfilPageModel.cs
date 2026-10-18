using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFroid.Services;

namespace StockFroid.PageModels;

public partial class ProfilPageModel : ObservableObject
{
    private readonly SessionService _session;

    public ProfilPageModel(SessionService session) => _session = session;

    [ObservableProperty]
    private string _nomAgent = "Agent";

    public void Charger() => NomAgent = _session.NomAgent;

    [RelayCommand]
    private async Task SeDeconnecterAsync()
    {
        _session.Deconnecter();
        await Shell.Current.GoToAsync("//login");
    }
}
