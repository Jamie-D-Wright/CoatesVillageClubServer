namespace VillageClub.Auth.Services;

/// <summary>
/// Service for secure password hashing and verification using BCrypt.
/// </summary>
public class PasswordHashService : IPasswordHashService
{
    /// <summary>
    /// Hashes a password using BCrypt with work factor 12.
    /// </summary>
    /// <param name="password">Plain text password.</param>
    /// <returns>Hashed password.</returns>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// Verifies a password against a hash.
    /// </summary>
    /// <param name="password">Plain text password to verify.</param>
    /// <param name="hash">Hashed password to verify against.</param>
    /// <returns>True if password matches hash, false otherwise.</returns>
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            // Invalid hash format or other BCrypt errors
            return false;
        }
    }
}
