using SQLite;

namespace StockFroid.Models;

/// <summary>
/// Compte d'un agent de stockage. Le mot de passe n'est jamais stocké en clair :
/// seuls le sel et l'empreinte PBKDF2 (voir <c>MotDePasseHasher</c>) sont enregistrés.
/// </summary>
public class Agent
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Matricule ou e-mail professionnel, en minuscules (unique).</summary>
    [Indexed(Unique = true)]
    public string Identifiant { get; set; } = string.Empty;

    public string NomComplet { get; set; } = string.Empty;

    public string Affectation { get; set; } = string.Empty;

    /// <summary>Sel aléatoire (Base64), propre à chaque compte.</summary>
    public string Sel { get; set; } = string.Empty;

    /// <summary>Empreinte PBKDF2-SHA256 du mot de passe (Base64).</summary>
    public string EmpreinteMotDePasse { get; set; } = string.Empty;

    /// <summary>Nombre d'itérations utilisées, conservé pour pouvoir le relever plus tard.</summary>
    public int Iterations { get; set; }

    public DateTime DateCreation { get; set; }
}
