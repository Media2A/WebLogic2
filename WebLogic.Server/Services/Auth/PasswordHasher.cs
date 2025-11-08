using CL.Core.Utilities.Security;

namespace WebLogic.Server.Services.Auth;

/// <summary>
/// Password hashing utility using CL.Core hashing
/// </summary>
public static class PasswordHasher
{
    /// <summary>
    /// Hash a plaintext password using CL.Core hashing
    /// </summary>
    /// <param name="password">Plaintext password</param>
    /// <returns>Hashed password</returns>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be empty", nameof(password));
        }

        return Hashing.HashPassword(password);
    }

    /// <summary>
    /// Verify a plaintext password against a hash
    /// </summary>
    /// <param name="password">Plaintext password to verify</param>
    /// <param name="hash">Hashed password to verify against</param>
    /// <returns>True if password matches hash</returns>
    public static bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        try
        {
            return Hashing.VerifyPassword(password, hash);
        }
        catch
        {
            // Invalid hash format
            return false;
        }
    }
}
