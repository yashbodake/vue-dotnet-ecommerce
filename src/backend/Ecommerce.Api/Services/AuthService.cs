using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Data;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Api.Services;

/// <summary>
/// Authentication service using native SQL against AspNetUsers.
/// Implements ASP.NET Identity password hashing for parity.
/// </summary>
public sealed class AuthService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly string _jwtSigningKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpirationMinutes;

    public AuthService(
        ISqlConnectionFactory connectionFactory,
        string jwtSigningKey,
        string jwtIssuer,
        string jwtAudience,
        int jwtExpirationMinutes)
    {
        _connectionFactory = connectionFactory;
        _jwtSigningKey = jwtSigningKey;
        _jwtIssuer = jwtIssuer;
        _jwtAudience = jwtAudience;
        _jwtExpirationMinutes = jwtExpirationMinutes;
    }

    /// <summary>
    /// Authenticate user and return JWT token.
    /// Uses ASP.NET Identity password hashing (PBKDF2 with HMAC-SHA256).
    /// </summary>
    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();

        // Find user by email
        var user = await GetUserByEmailAsync(connection, email);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return null;
        }

        // Verify password using ASP.NET Identity hasher (PBKDF2)
        if (!VerifyPassword(password, user.PasswordHash))
        {
            return null;
        }

        // Get user roles
        var roles = await GetUserRolesAsync(connection, user.Id);

        // Generate JWT token
        var token = GenerateJwtToken(user.Id, user.Email!, roles);

        return new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = user.Email!,
            Roles = roles,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes)
        };
    }

    /// <summary>
    /// Get user info by ID for /api/auth/me endpoint.
    /// </summary>
    public async Task<UserInfo?> GetUserInfoAsync(string userId)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        
        var user = await GetUserByIdAsync(connection, userId);
        if (user == null)
        {
            return null;
        }

        var roles = await GetUserRolesAsync(connection, user.Id);

        return new UserInfo
        {
            UserId = user.Id,
            Email = user.Email ?? user.UserName,
            Roles = roles
        };
    }

    private static async Task<AspNetUser?> GetUserByEmailAsync(SqlConnection connection, string email)
    {
        using var cmd = new SqlCommand("""
            SELECT Id, Email, EmailConfirmed, PasswordHash, SecurityStamp, PhoneNumber,
                   PhoneNumberConfirmed, TwoFactorEnabled, LockoutEndDateUtc, LockoutEnabled,
                   AccessFailedCount, UserName
            FROM dbo.AspNetUsers
            WHERE Email = @Email
            """, connection);
        cmd.Parameters.AddWithValue("@Email", email);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new AspNetUser
        {
            Id = reader.GetString(0),
            Email = reader.IsDBNull(1) ? null : reader.GetString(1),
            EmailConfirmed = reader.GetBoolean(2),
            PasswordHash = reader.IsDBNull(3) ? null : reader.GetString(3),
            SecurityStamp = reader.IsDBNull(4) ? null : reader.GetString(4),
            PhoneNumber = reader.IsDBNull(5) ? null : reader.GetString(5),
            PhoneNumberConfirmed = reader.GetBoolean(6),
            TwoFactorEnabled = reader.GetBoolean(7),
            LockoutEndDateUtc = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
            LockoutEnabled = reader.GetBoolean(9),
            AccessFailedCount = reader.GetInt32(10),
            UserName = reader.GetString(11)
        };
    }

    private static async Task<AspNetUser?> GetUserByIdAsync(SqlConnection connection, string userId)
    {
        using var cmd = new SqlCommand("""
            SELECT Id, Email, EmailConfirmed, PasswordHash, SecurityStamp, PhoneNumber,
                   PhoneNumberConfirmed, TwoFactorEnabled, LockoutEndDateUtc, LockoutEnabled,
                   AccessFailedCount, UserName
            FROM dbo.AspNetUsers
            WHERE Id = @UserId
            """, connection);
        cmd.Parameters.AddWithValue("@UserId", userId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new AspNetUser
        {
            Id = reader.GetString(0),
            Email = reader.IsDBNull(1) ? null : reader.GetString(1),
            EmailConfirmed = reader.GetBoolean(2),
            PasswordHash = reader.IsDBNull(3) ? null : reader.GetString(3),
            SecurityStamp = reader.IsDBNull(4) ? null : reader.GetString(4),
            PhoneNumber = reader.IsDBNull(5) ? null : reader.GetString(5),
            PhoneNumberConfirmed = reader.GetBoolean(6),
            TwoFactorEnabled = reader.GetBoolean(7),
            LockoutEndDateUtc = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
            LockoutEnabled = reader.GetBoolean(9),
            AccessFailedCount = reader.GetInt32(10),
            UserName = reader.GetString(11)
        };
    }

    private static async Task<List<string>> GetUserRolesAsync(SqlConnection connection, string userId)
    {
        var roles = new List<string>();
        using var cmd = new SqlCommand("""
            SELECT r.Name
            FROM dbo.AspNetUserRoles ur
            INNER JOIN dbo.AspNetRoles r ON ur.RoleId = r.Id
            WHERE ur.UserId = @UserId
            """, connection);
        cmd.Parameters.AddWithValue("@UserId", userId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            roles.Add(reader.GetString(0));
        }

        return roles;
    }

    /// <summary>
    /// Verify password using ASP.NET Identity PBKDF2 hashing.
    /// Legacy uses: HMACSHA256, 10000 iterations, 256-bit salt, 192-bit subkey.
    /// </summary>
    private static bool VerifyPassword(string password, string hashedPassword)
    {
        // ASP.NET Identity v3 format: {0x00, salt (16 bytes), subkey (24 bytes)}
        var bytes = Convert.FromBase64String(hashedPassword);
        if (bytes.Length < 41) // 1 (marker) + 16 (salt) + 24 (subkey)
        {
            return false;
        }

        // Skip version byte (0x00)
        var salt = new byte[16];
        Array.Copy(bytes, 1, salt, 0, 16);

        var expectedSubkey = new byte[24];
        Array.Copy(bytes, 17, expectedSubkey, 0, 24);

        // Generate subkey with same parameters
        var actualSubkey = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 24);

        return FixedTimeEquals(actualSubkey, expectedSubkey);
    }

    /// <summary>
    /// Register a new customer. Returns a JWT token response on success.
    /// Throws ArgumentException for validation failures (duplicate email, short password).
    /// </summary>
    public async Task<RegisterResponse?> RegisterAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.");
        }

        if (string.IsNullOrEmpty(password) || password.Length < 6)
        {
            throw new ArgumentException("Password must be at least 6 characters.");
        }

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();

        var existingUser = await GetUserByEmailAsync(connection, email);
        if (existingUser != null)
        {
            throw new ArgumentException("Email is already registered.");
        }

        var userId = Guid.NewGuid().ToString();
        var passwordHash = HashPassword(password);
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
        insert.Parameters.AddWithValue("@Email", email);
        insert.Parameters.AddWithValue("@PasswordHash", passwordHash);
        insert.Parameters.AddWithValue("@SecurityStamp", securityStamp);
        insert.Parameters.AddWithValue("@UserName", email);

        await insert.ExecuteNonQueryAsync();

        var roles = new List<string>();
        var token = GenerateJwtToken(userId, email, roles);

        return new RegisterResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = email,
            Roles = roles,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes)
        };
    }

    private static bool FixedTimeEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var result = 0;
        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
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

    private JwtSecurityToken GenerateJwtToken(string userId, string email, IReadOnlyList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
            signingCredentials: credentials
        );

        return token;
    }
}
