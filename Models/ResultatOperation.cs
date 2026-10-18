namespace StockFroid.Models;

public record ResultatOperation(bool Succes, string Message)
{
    public static ResultatOperation Ok(string message = "") => new(true, message);
    public static ResultatOperation Echec(string message) => new(false, message);
}

public record ResultatOperation<T>(bool Succes, string Message, T? Valeur = default)
{
    public static ResultatOperation<T> Ok(T valeur, string message = "") => new(true, message, valeur);
    public static ResultatOperation<T> Echec(string message) => new(false, message);
}
