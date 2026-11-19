using Chinnaphat_8272_L005.Models;
using Microsoft.AspNetCore.Identity;

namespace Chinnaphat_8272_L005.Helpers;

public static class PasswordSecurity
{
    private static readonly PasswordHasher<User> Hasher = new();

    public static string Hash(User user, string password) => Hasher.HashPassword(user, password);

    public static bool IsModernHash(string storedPassword) =>
        storedPassword.StartsWith("AQAAAA", StringComparison.Ordinal);

    /// <summary>
    /// Verifies a modern ASP.NET Identity hash. Legacy plaintext passwords are accepted once,
    /// then upgraded to a salted hash immediately after successful authentication.
    /// </summary>
    public static bool VerifyAndUpgrade(User user, string password, out bool upgraded)
    {
        upgraded = false;
        var result = Hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = Hasher.HashPassword(user, password);
            upgraded = true;
            return true;
        }

        if (result == PasswordVerificationResult.Success) return true;

        if (!string.Equals(user.PasswordHash, password, StringComparison.Ordinal)) return false;

        user.PasswordHash = Hasher.HashPassword(user, password);
        upgraded = true;
        return true;
    }
}
