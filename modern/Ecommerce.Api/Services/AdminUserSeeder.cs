using System.Security.Cryptography;
using Ecommerce.Api.Data;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Data.SqlClient;

namespace Ecommerce.Api.Services;

/// <summary>
/// Idempotent seeder that ensures the Admin role and admin@legacy.local user exist.
/// Password is hashed using ASP.NET Identity v3 PBKDF2 format for parity.
/// Skipped when environment is "Testing" (per S03 CONTEXT pitfalls).
/// </summary>
public sealed class AdminUserSeeder
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<AdminUserSeeder> _logger;

    public const string AdminRoleName = "Admin";
    public const string AdminEmail = "admin@legacy.local";
    public const string AdminPassword = "Admin123!";

    public AdminUserSeeder(ISqlConnectionFactory connectionFactory, ILogger<AdminUserSeeder> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Ensure Admin role and admin user exist. Safe to call on every startup.
    /// </summary>
    public void EnsureAdminUser()
    {
        try
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();

            EnsureAdminRole(connection);
            EnsureAdminUserInternal(connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed admin user");
        }
    }

    private void EnsureAdminRole(SqlConnection connection)
    {
        using var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.AspNetRoles WHERE Name = @Name",
            connection);
        cmd.Parameters.AddWithValue("@Name", AdminRoleName);

        var count = (int)cmd.ExecuteScalar()!;
        if (count > 0)
        {
            _logger.LogInformation("Admin role already exists");
            return;
        }

        using var insert = new SqlCommand(
            "INSERT INTO dbo.AspNetRoles (Id, Name) VALUES (@Id, @Name)",
            connection);
        insert.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
        insert.Parameters.AddWithValue("@Name", AdminRoleName);
        insert.ExecuteNonQuery();

        _logger.LogInformation("Admin role created");
    }

    private void EnsureAdminUserInternal(SqlConnection connection)
    {
        // Check if user exists
        using var findCmd = new SqlCommand(
            "SELECT Id FROM dbo.AspNetUsers WHERE Email = @Email",
            connection);
        findCmd.Parameters.AddWithValue("@Email", AdminEmail);

        var existingId = findCmd.ExecuteScalar() as string;

        string userId;
        if (existingId != null)
        {
            userId = existingId;
            // Update password hash to ensure our format works (parity with documented Admin123!)
            var passwordHash = HashPassword(AdminPassword);
            using var update = new SqlCommand(
                "UPDATE dbo.AspNetUsers SET PasswordHash = @PasswordHash WHERE Id = @Id",
                connection);
            update.Parameters.AddWithValue("@PasswordHash", passwordHash);
            update.Parameters.AddWithValue("@Id", userId);
            update.ExecuteNonQuery();
            _logger.LogInformation("Admin user already exists, password hash updated");
        }
        else
        {
            userId = Guid.NewGuid().ToString();
            var passwordHash = HashPassword(AdminPassword);
            var securityStamp = Guid.NewGuid().ToString();

            using var insert = new SqlCommand("""
                INSERT INTO dbo.AspNetUsers
                    (Id, Email, EmailConfirmed, PasswordHash, SecurityStamp, PhoneNumber,
                     PhoneNumberConfirmed, TwoFactorEnabled, LockoutEndDateUtc, LockoutEnabled,
                     AccessFailedCount, UserName)
                VALUES
                    (@Id, @Email, 1, @PasswordHash, @SecurityStamp, NULL,
                     0, 0, NULL, 1, 0, @UserName)
                """, connection);
            insert.Parameters.AddWithValue("@Id", userId);
            insert.Parameters.AddWithValue("@Email", AdminEmail);
            insert.Parameters.AddWithValue("@PasswordHash", passwordHash);
            insert.Parameters.AddWithValue("@SecurityStamp", securityStamp);
            insert.Parameters.AddWithValue("@UserName", AdminEmail);
            insert.ExecuteNonQuery();

            _logger.LogInformation("Admin user created: {Email}", AdminEmail);
        }

        // Ensure user has Admin role
        using var roleCheck = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.AspNetUserRoles WHERE UserId = @UserId AND RoleId = (SELECT Id FROM dbo.AspNetRoles WHERE Name = 'Admin')",
            connection);
        roleCheck.Parameters.AddWithValue("@UserId", userId);

        var roleCount = (int)roleCheck.ExecuteScalar()!;
        if (roleCount == 0)
        {
            using var roleInsert = new SqlCommand("""
                INSERT INTO dbo.AspNetUserRoles (UserId, RoleId)
                SELECT @UserId, Id FROM dbo.AspNetRoles WHERE Name = 'Admin'
                """, connection);
            roleInsert.Parameters.AddWithValue("@UserId", userId);
            roleInsert.ExecuteNonQuery();

            _logger.LogInformation("Admin role assigned to user");
        }
    }

    /// <summary>
    /// Hash password using ASP.NET Identity v3 format (PBKDF2 HMAC-SHA256, 10000 iterations).
    /// Format: 0x00 + 16-byte salt + 24-byte subkey, base64 encoded.
    /// </summary>
    private static string HashPassword(string password)
    {
        var salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        var subkey = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 24);

        var outputBytes = new byte[1 + 16 + 24]; // marker + salt + subkey
        outputBytes[0] = 0x00;
        Buffer.BlockCopy(salt, 0, outputBytes, 1, 16);
        Buffer.BlockCopy(subkey, 0, outputBytes, 17, 24);

        return Convert.ToBase64String(outputBytes);
    }
}