using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using SQLite;
using StockFroid.Models;

namespace StockFroid.Data;

/// <summary>
/// Fournit une connexion SQLite partagée et initialise les tables de l'application.
/// </summary>
public class StockFroidDatabase
{
    private readonly string _dbPath;
    private SQLiteAsyncConnection? _connection;

    public StockFroidDatabase()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "stockfroid.db3");
    }

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
            return _connection;

        _connection = new SQLiteAsyncConnection(_dbPath);

        // Crée les tables si nécessaire
        await _connection.CreateTableAsync<Campagne>();
        await _connection.CreateTableAsync<Lot>();
        await _connection.CreateTableAsync<Perte>();

        return _connection;
    }
}
