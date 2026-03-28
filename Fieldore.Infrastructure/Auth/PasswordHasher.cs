using System.Security.Cryptography;

namespace Fieldore.Infrastructure.Auth;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public (string Hash, string Salt) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool VerifyPassword(string password, string passwordHash, string passwordSalt)
    {
        var salt = Convert.FromBase64String(passwordSalt);
        var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        var existingHash = Convert.FromBase64String(passwordHash);

        return CryptographicOperations.FixedTimeEquals(computedHash, existingHash);
    }
}
