using System.Security.Cryptography;
using System.Text;
using FitCore.Identity.Application.Abstractions;

namespace FitCore.Identity.Infrastructure.Security;

// Formato almacenado: PBKDF2-SHA256$<iteraciones>$<salt base64>$<hash base64>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Prefix = "PBKDF2-SHA256";
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 600_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string storedValue, string password, out bool needsRehash)
    {
        needsRehash = false;
        var parts = storedValue.Split('$');

        if (parts.Length != 4 || parts[0] != Prefix)
        {
            // Registro anterior al hashing: contraseña en texto plano.
            needsRehash = true;
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(storedValue),
                Encoding.UTF8.GetBytes(password));
        }

        if (!int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        var valid = CryptographicOperations.FixedTimeEquals(actual, expected);
        needsRehash = valid && iterations < Iterations;
        return valid;
    }
}
