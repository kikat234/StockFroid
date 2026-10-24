using SQLite;
using StockFroid.Data;
using StockFroid.Models;

namespace StockFroid.Services;

/// <summary>Comptes agents stockés dans la base SQLite locale (inscription, connexion).</summary>
public class AuthService
{
    public const int LongueurMinMotDePasse = 8;

    private const string MessageIdentifiantsInvalides = "Identifiant ou mot de passe incorrect.";
    private const string MessageCompteExistant = "Un compte existe déjà avec ce matricule ou cet e-mail.";

    private readonly StockFroidDatabase _database;
    private readonly SessionService _session;

    public AuthService(StockFroidDatabase database, SessionService session)
    {
        _database = database;
        _session = session;
    }

    /// <summary>Un identifiant est comparé sans tenir compte des espaces de bord ni de la casse.</summary>
    public static string NormaliserIdentifiant(string? identifiant) =>
        (identifiant ?? string.Empty).Trim().ToLowerInvariant();

    public async Task<ResultatOperation<Agent>> InscrireAsync(
        string nomComplet, string identifiant, string affectation, string motDePasse)
    {
        nomComplet = (nomComplet ?? string.Empty).Trim();
        var id = NormaliserIdentifiant(identifiant);
        motDePasse ??= string.Empty;

        if (nomComplet.Length == 0)
            return ResultatOperation<Agent>.Echec("Le nom et le prénom sont obligatoires.");
        if (id.Length == 0)
            return ResultatOperation<Agent>.Echec("Le matricule ou l'e-mail professionnel est obligatoire.");
        if (id.Length > 80 || nomComplet.Length > 80)
            return ResultatOperation<Agent>.Echec("Le nom ou l'identifiant est trop long (80 caractères maximum).");
        if (motDePasse.Length < LongueurMinMotDePasse)
            return ResultatOperation<Agent>.Echec($"Le mot de passe doit comporter au moins {LongueurMinMotDePasse} caractères.");

        var db = await _database.GetConnectionAsync();
        if (await db.Table<Agent>().Where(a => a.Identifiant == id).FirstOrDefaultAsync() is not null)
            return ResultatOperation<Agent>.Echec(MessageCompteExistant);

        // Le hachage est volontairement coûteux : on le sort du thread d'interface.
        var (sel, empreinte) = await Task.Run(() => MotDePasseHasher.Hacher(motDePasse));

        var agent = new Agent
        {
            Identifiant = id,
            NomComplet = nomComplet,
            Affectation = affectation ?? string.Empty,
            Sel = sel,
            EmpreinteMotDePasse = empreinte,
            Iterations = MotDePasseHasher.IterationsParDefaut,
            DateCreation = DateTime.Now
        };

        try
        {
            await db.InsertAsync(agent);
        }
        catch (SQLiteException)
        {
            // Filet de sécurité : deux inscriptions simultanées avec le même identifiant (index unique).
            return ResultatOperation<Agent>.Echec(MessageCompteExistant);
        }

        _session.Connecter(agent);
        return ResultatOperation<Agent>.Ok(agent, "Compte créé.");
    }

    public async Task<ResultatOperation<Agent>> ConnecterAsync(string identifiant, string motDePasse)
    {
        var id = NormaliserIdentifiant(identifiant);
        motDePasse ??= string.Empty;

        if (id.Length == 0 || motDePasse.Length == 0)
            return ResultatOperation<Agent>.Echec("Renseignez votre identifiant et votre mot de passe.");

        var db = await _database.GetConnectionAsync();
        var agent = await db.Table<Agent>().Where(a => a.Identifiant == id).FirstOrDefaultAsync();

        bool valide;
        if (agent is null)
        {
            // Même coût de calcul qu'un vrai compte, pour ne pas révéler quels identifiants existent.
            await Task.Run(() => MotDePasseHasher.Hacher(motDePasse));
            valide = false;
        }
        else
        {
            var compte = agent;
            valide = await Task.Run(() => MotDePasseHasher.Verifier(
                motDePasse, compte.Sel, compte.EmpreinteMotDePasse, compte.Iterations));
        }

        if (!valide || agent is null)
            return ResultatOperation<Agent>.Echec(MessageIdentifiantsInvalides);

        _session.Connecter(agent);
        return ResultatOperation<Agent>.Ok(agent);
    }
}
