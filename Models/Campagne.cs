using SQLite;

namespace StockFroid.Models;

public class Campagne
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Nom { get; set; } = string.Empty;

    public DateTime DateDebut { get; set; }

    /// <summary>Vide tant que la campagne est en cours.</summary>
    public DateTime? DateFin { get; set; }

    [Ignore]
    public bool EstEnCours => DateFin is null;
}
