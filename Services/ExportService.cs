using System.Diagnostics;
using System.Text;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;
using StockFroid.Data;
using StockFroid.Models;

namespace StockFroid.Services;

public enum FormatExport
{
    Pdf,
    Csv
}

/// <summary>Génère les rapports de campagne (PDF ou CSV) dans le cache de l'application et les partage.</summary>
public class ExportService
{
    private readonly StockFroidDatabase _database;
    private readonly BilanService _bilans;
    private readonly CampagneService _campagnes;

    public ExportService(StockFroidDatabase database, BilanService bilans, CampagneService campagnes)
    {
        _database = database;
        _bilans = bilans;
        _campagnes = campagnes;
    }

    /// <summary>Rassemble les données du rapport (bilan, lots, historique des pertes) d'une campagne.</summary>
    public async Task<RapportCampagne?> PreparerAsync(int campagneId)
    {
        var campagne = await _campagnes.GetAsync(campagneId);
        if (campagne is null) return null;

        var bilan = await _bilans.CalculerAsync(campagneId);
        var codes = bilan.Lots.ToDictionary(l => l.Lot.Id, l => l.Lot.Code);

        var db = await _database.GetConnectionAsync();
        var pertes = (await db.Table<Perte>().ToListAsync())
            .Where(p => codes.ContainsKey(p.LotId))
            .OrderByDescending(p => p.DateConstat)
            .Select(p => new LignePerte(codes[p.LotId], p))
            .ToList();

        return new RapportCampagne(campagne, bilan, pertes, DateTime.Now);
    }

    /// <summary>Crée le fichier du rapport et renvoie son chemin.</summary>
    public async Task<ResultatOperation<string>> ExporterAsync(int campagneId, FormatExport format)
    {
        var rapport = await PreparerAsync(campagneId);
        if (rapport is null)
            return ResultatOperation<string>.Echec("Campagne introuvable.");

        var dossier = Path.Combine(FileSystem.CacheDirectory, "exports");
        Directory.CreateDirectory(dossier);

        var extension = format == FormatExport.Pdf ? "pdf" : "csv";
        var chemin = Path.Combine(dossier, RapportBuilder.NomDeFichier(rapport, extension));

        if (format == FormatExport.Pdf)
        {
            var pdf = await Task.Run(() => RapportBuilder.ConstruirePdf(rapport));
            await File.WriteAllBytesAsync(chemin, pdf);
        }
        else
        {
            // UTF-8 avec BOM : Excel reconnaît ainsi les accents.
            await File.WriteAllTextAsync(chemin, RapportBuilder.ConstruireCsv(rapport), new UTF8Encoding(true));
        }

        return ResultatOperation<string>.Ok(chemin, "Rapport généré.");
    }

    /// <summary>Vrai si « Enregistrer sous… » est disponible sur cette plateforme (Windows uniquement).</summary>
    public static bool EnregistrementDisponible =>
#if WINDOWS
        true;
#else
        false;
#endif

    /// <summary>
    /// Ouvre le dialogue « Enregistrer sous… » pour copier le rapport où l'utilisateur le souhaite.
    /// Valeur null avec Succes = true : l'utilisateur a annulé.
    /// </summary>
    public async Task<ResultatOperation<string?>> EnregistrerSousAsync(string chemin)
    {
#if WINDOWS
        try
        {
            var fenetre = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as MauiWinUIWindow;
            if (fenetre is null)
                return ResultatOperation<string?>.Echec("Fenêtre introuvable : utilisez « Partager ».");

            var extension = Path.GetExtension(chemin);
            var selecteur = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
                SuggestedFileName = Path.GetFileNameWithoutExtension(chemin)
            };
            selecteur.FileTypeChoices.Add(
                extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) ? "Document PDF" : "Fichier CSV",
                new List<string> { extension });
            WinRT.Interop.InitializeWithWindow.Initialize(selecteur, fenetre.WindowHandle);

            var destination = await selecteur.PickSaveFileAsync();
            if (destination is null)
                return ResultatOperation<string?>.Ok(null);

            await Windows.Storage.FileIO.WriteBytesAsync(destination, await File.ReadAllBytesAsync(chemin));
            return ResultatOperation<string?>.Ok(destination.Path, "Rapport enregistré.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return ResultatOperation<string?>.Echec("Le rapport n'a pas pu être enregistré à cet emplacement.");
        }
#else
        await Task.CompletedTask;
        return ResultatOperation<string?>.Echec("L'enregistrement direct n'est pas disponible ici : utilisez « Partager ».");
#endif
    }

    /// <summary>Ouvre la feuille de partage du système (enregistrer, envoyer, ouvrir avec…).</summary>
    public async Task<ResultatOperation> PartagerAsync(string chemin, string titre)
    {
        try
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = titre,
                File = new ShareFile(chemin)
            });
            return ResultatOperation.Ok();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return ResultatOperation.Echec($"Le rapport a été créé mais ne peut pas être partagé sur cet appareil.\n\n{chemin}");
        }
    }
}