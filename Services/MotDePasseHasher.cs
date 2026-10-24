using System.Security.Cryptography;
using System.Text;

namespace StockFroid.Services;

/// <summary>Hachage des mots de passe : PBKDF2-SHA256 avec sel aléatoire, comparaison à temps constant.</summary>
public static class MotDePasseHasher
{
    public const int IterationsParDefaut = 210_000;

    private const int TailleSel = 16;
    private const int TailleEmpreinte = 32;

    public static (string Sel, string Empreinte) Hacher(string motDePasse, int iterations = IterationsParDefaut)
    {
        var sel = RandomNumberGenerator.GetBytes(TailleSel);
        var empreinte = Calculer(motDePasse, sel, iterations);
        return (Convert.ToBase64String(sel), Convert.ToBase64String(empreinte));
    }

    public static bool Verifier(string motDePasse, string selBase64, string empreinteBase64, int iterations)
    {
        try
        {
            var sel = Convert.FromBase64String(selBase64);
            var attendue = Convert.FromBase64String(empreinteBase64);
            var calculee = Calculer(motDePasse, sel, iterations);
            return CryptographicOperations.FixedTimeEquals(calculee, attendue);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Calculer(string motDePasse, byte[] sel, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(motDePasse), sel, iterations,
            HashAlgorithmName.SHA256, TailleEmpreinte);
}
