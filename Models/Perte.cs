using SQLite;

namespace StockFroid.Models;

public class Perte
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int LotId { get; set; }

    public DateTime DateConstat { get; set; }

    public double QuantitePerdue { get; set; }

    public CauseDePerte Cause { get; set; }

    public string? Commentaire { get; set; }
}
