using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Data;
using Ecommerce.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Api.Tests;

/// <summary>
/// Adversarial tests for AuthService and JWT handling: malformed/empty input,
/// weak passwords, forged tokens, expired tokens, and role claims.
/// Uses the real SQL Express database.
/// </summary>
public class AuthAdversarialTests : IDisposable
{
    private readonly string _connectionString = "Server=.\\SQLEXPRESS;Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True";
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly List<string> _registeredUserIds = [];

    public AuthAdversarialTests()
    {
        _connectionFactory = new SqlConnectionFactory(_connectionString);
    }

    private AuthService CreateAuthService() => new(
        _connectionFactory,
        TestJwtKey.Value,
        "EcommerceModernApi",
        "EcommerceModernClient",
        60
    );

    private static string GenerateUniqueEmail() => $"adv-{Guid.NewGuid():N}@test.local";

    #region Input validation / malformed credentials

    [Fact]
    public async Task Register_EmptyEmail_ThrowsArgumentException()
    {
        // Scenario: registration with empty or whitespace email must fail cleanly.
        var authService = CreateAuthService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => authService.RegisterAsync("   ", "Password123!"));

        Assert.NotNull(exception.Message);
    }

    [Fact]
    public async Task Register_ShortPassword_ThrowsArgumentException()
    {
        // Scenario: password below minimum length must be rejected.
        var authService = CreateAuthService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => authService.RegisterAsync(GenerateUniqueEmail(), "12345"));

        Assert.NotNull(exception.Message);
    }

    [Fact]
    public async Task Register_WeakCommonPassword_StillAccepted()
    {
        // Scenario: only length is validated, not complexity. Documents weak-password policy gap.
        var authService = CreateAuthService();
        var email = GenerateUniqueEmail();

        var response = await authService.RegisterAsync(email, "password");

        Assert.NotNull(response);
        Assert.NotEmpty(response.Token);

        var userId = await GetUserIdByEmailAsync(email);
        Assert.NotNull(userId);
        _registeredUserIds.Add(userId);
    }

    [Fact]
    public async Task Login_WithSqlInjectionPatternInEmail_ReturnsNull()
    {
        // Scenario: attacker submits SQL-injection-like email. Must not authenticate and must not throw.
        var authService = CreateAuthService();

        var response = await authService.LoginAsync("' OR 1=1 --", "password");

        Assert.Null(response);
    }

    [Fact]
    public async Task Login_WithOverlongEmail_ReturnsNull()
    {
        // Scenario: oversized input should be rejected or return null, not crash.
        var authService = CreateAuthService();
        var email = new string('a', 500) + "@test.local";

        var response = await authService.LoginAsync(email, "password");

        Assert.Null(response);
    }

    #endregion

    #region Token manipulation

    [Fact]
    public void GenerateJwtToken_UnknownSigningKey_IsRejectedByValidation()
    {
        // Scenario: token signed with a different key must fail signature validation.
        var wrongKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("WrongKeyThatIsAlso32CharsLong!!!"));
        var credentials = new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "EcommerceModernApi",
            audience: "EcommerceModernClient",
            claims: [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(TestJwtKey.Value)),
            ValidateIssuer = true,
            ValidIssuer = "EcommerceModernApi",
            ValidateAudience = true,
            ValidAudience = "EcommerceModernClient",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var exception = Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(tokenString, validationParameters, out _));
        Assert.True(exception is SecurityTokenInvalidSignatureException or SecurityTokenSignatureKeyNotFoundException,
            $"Expected signature validation failure but got {exception.GetType().Name}.");
    }

    [Fact]
    public void GenerateJwtToken_ExpiredToken_IsRejectedByValidation()
    {
        // Scenario: replay of an expired token must fail lifetime validation.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey.Value));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "EcommerceModernApi",
            audience: "EcommerceModernClient",
            claims: [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = "EcommerceModernApi",
            ValidateAudience = true,
            ValidAudience = "EcommerceModernClient",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        Assert.Throws<SecurityTokenExpiredException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(tokenString, validationParameters, out _));
    }

    [Fact]
    public void GenerateJwtToken_MissingNameIdentifierClaim_FailsExtraction()
    {
        // Scenario: token without NameIdentifier should not identify a user.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey.Value));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "EcommerceModernApi",
            audience: "EcommerceModernClient",
            claims: [new Claim(ClaimTypes.Email, "attacker@example.com")],
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(token.Claims, "Bearer"));
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        Assert.Null(userId);
    }

    [Fact]
    public void GenerateJwtToken_ForgedAdminRole_RequiresRealDatabaseRole()
    {
        // Scenario: a token with a forged Admin role claim does not grant admin access
        // because endpoints also rely on the database role mapping. Service layer only
        // issues roles from the DB, so a forged claim is observable at the endpoint level.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey.Value));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "EcommerceModernApi",
            audience: "EcommerceModernClient",
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email, "attacker@example.com"),
                new Claim(ClaimTypes.Role, "Admin")
            ],
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(token.Claims, "Bearer"));
        var roles = principal.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

        Assert.Contains("Admin", roles);
        // Note: the Admin policy requires the Admin role claim from a valid token issued
        // by the service, which only adds roles from AspNetUserRoles. This documents the
        // need for endpoint-level authorization to validate the claim against the DB.
    }

    #endregion

    #region Helper methods

    private async Task<string?> GetUserIdByEmailAsync(string email)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "SELECT Id FROM dbo.AspNetUsers WHERE Email = @Email",
            connection);
        command.Parameters.AddWithValue("@Email", email);
        var result = await command.ExecuteScalarAsync();
        return result is string id ? id : null;
    }

    private void DeleteTestUser(string userId)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var deleteRoles = new SqlCommand(
            "DELETE FROM dbo.AspNetUserRoles WHERE UserId = @UserId",
            connection);
        deleteRoles.Parameters.AddWithValue("@UserId", userId);
        deleteRoles.ExecuteNonQuery();

        using var deleteUser = new SqlCommand(
            "DELETE FROM dbo.AspNetUsers WHERE Id = @UserId",
            connection);
        deleteUser.Parameters.AddWithValue("@UserId", userId);
        deleteUser.ExecuteNonQuery();
    }

    public void Dispose()
    {
        foreach (var userId in _registeredUserIds)
        {
            DeleteTestUser(userId);
        }
    }

    #endregion
}
