using Microsoft.Maui.Storage;
using SQLite;
using StockFroid.Models;

namespace StockFroid.Data;

/// <summary>
/// Accès à la base SQLite locale (fonctionne hors connexion).
/// La base est créée et initialisée une seule fois, au premier accès.
/// </summary>
public class StockFroidDatabase
{
    private readonly Lazy<Task<SQLiteAsyncConnection>> _connexion;

    public StockFroidDatabase()
    {
        _connexion = new Lazy<Task<SQLiteAsyncConnection>>(InitialiserAsync);
    }

    public Task<SQLiteAsyncConnection> GetConnectionAsync() => _connexion.Value;

    private static async Task<SQLiteAsyncConnection> InitialiserAsync()
    {
        var chemin = Path.Combine(FileSystem.AppDataDirectory, "stockfroid.db3");
        var db = new SQLiteAsyncConnection(chemin);

        await db.CreateTableAsync<Campagne>();
        await db.CreateTableAsync<Lot>();
        await db.CreateTableAsync<Perte>();
        await db.CreateTableAsync<Agent>();

        // Jeu de données de démonstration (uniquement si la base est vide).
        if (await db.Table<Campagne>().CountAsync() == 0)
            await DemoData.InsererAsync(db);

        // Compte de démonstration (uniquement si aucun compte n'existe encore).
        if (await db.Table<Agent>().CountAsync() == 0)
            await DemoData.InsererAgentAsync(db);

        return db;
    }
}
