using Ecommerce.Api.Contracts;
using Ecommerce.Api.Data;
using Ecommerce.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Api.Tests;

/// <summary>
/// Auth + seeder tests against real SQL Express (host).
/// Requires: .\SQLEXPRESS with LegacyEcommerceDb seeded (AspNetUsers/AspNetRoles tables).
/// </summary>
public class AuthTests
{
    private readonly string _connectionString = "Server=.\\SQLEXPRESS;Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True";

    private SqlConnectionFactory CreateFactory() => new(_connectionString);

    private AuthService CreateAuthService(SqlConnectionFactory? factory = null)
    {
        factory ??= CreateFactory();
        return new AuthService(
            factory,
            TestJwtKey.Value,
            "EcommerceModernApi",
            "EcommerceModernClient",
            60
        );
    }

    [Fact]
    public async Task Login_WithValidAdminCredentials_ReturnsJwtWithAdminRole()
    {
        // Arrange - ensure seeder has run
        var factory = CreateFactory();
        var seederLogger = new LoggerFactory().CreateLogger<AdminUserSeeder>();
        var seeder = new AdminUserSeeder(factory, seederLogger);
        seeder.EnsureAdminUser();

        var authService = CreateAuthService(factory);

        // Act
        var response = await authService.LoginAsync("admin@legacy.local", "Admin123!");

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Token);
        Assert.Equal("admin@legacy.local", response.Email);
        Assert.Contains("Admin", response.Roles);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var authService = CreateAuthService();

        // Act
        var response = await authService.LoginAsync("admin@legacy.local", "WrongPassword123!");

        // Assert
        Assert.Null(response);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        var authService = CreateAuthService();

        // Act
        var response = await authService.LoginAsync("nobody@nowhere.local", "Password123!");

        // Assert
        Assert.Null(response);
    }

    [Fact]
    public async Task Login_WithEmptyCredentials_ReturnsNull()
    {
        // Arrange
        var authService = CreateAuthService();

        // Act
        var response = await authService.LoginAsync("", "");

        // Assert
        Assert.Null(response);
    }

    [Fact]
    public async Task GetUserInfo_WithValidUserId_ReturnsUserInfo()
    {
        // Arrange - ensure seeder has run and get user ID
        var factory = CreateFactory();
        var seederLogger = new LoggerFactory().CreateLogger<AdminUserSeeder>();
        var seeder = new AdminUserSeeder(factory, seederLogger);
        seeder.EnsureAdminUser();

        // Get the admin user's ID from DB
        string userId;
        using (var conn = (SqlConnection)factory.CreateConnection())
        using (var cmd = new SqlCommand(
            "SELECT Id FROM dbo.AspNetUsers WHERE Email = @Email",
            (SqlConnection)conn))
        {
            cmd.Parameters.AddWithValue("@Email", "admin@legacy.local");
            userId = (string)cmd.ExecuteScalar()!;
        }

        var authService = CreateAuthService(factory);

        // Act
        var userInfo = await authService.GetUserInfoAsync(userId);

        // Assert
        Assert.NotNull(userInfo);
        Assert.Equal("admin@legacy.local", userInfo.Email);
        Assert.Contains("Admin", userInfo.Roles);
    }

    [Fact]
    public async Task GetUserInfo_WithInvalidUserId_ReturnsNull()
    {
        // Arrange
        var authService = CreateAuthService();

        // Act
        var userInfo = await authService.GetUserInfoAsync("nonexistent-user-id");

        // Assert
        Assert.Null(userInfo);
    }

    [Fact]
    public void AdminUserSeeder_IsIdempotent_DoesNotDuplicateOnMultipleRuns()
    {
        // Arrange
        var factory = CreateFactory();
        var seederLogger = new LoggerFactory().CreateLogger<AdminUserSeeder>();
        var seeder = new AdminUserSeeder(factory, seederLogger);

        // Act - run seeder twice
        seeder.EnsureAdminUser();
        seeder.EnsureAdminUser();

        // Assert - only one admin user exists
        using var conn = (SqlConnection)factory.CreateConnection();
        using var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.AspNetUsers WHERE Email = @Email",
            conn);
        cmd.Parameters.AddWithValue("@Email", "admin@legacy.local");
        var count = (int)cmd.ExecuteScalar()!;
        Assert.Equal(1, count);

        // Assert - only one admin role exists
        using var cmdRole = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.AspNetRoles WHERE Name = 'Admin'",
            conn);
        var roleCount = (int)cmdRole.ExecuteScalar()!;
        Assert.Equal(1, roleCount);

        // Assert - only one user-role mapping
        using var cmdMapping = new SqlCommand("""
            SELECT COUNT(*)
            FROM dbo.AspNetUserRoles ur
            INNER JOIN dbo.AspNetUsers u ON ur.UserId = u.Id
            INNER JOIN dbo.AspNetRoles r ON ur.RoleId = r.Id
            WHERE u.Email = 'admin@legacy.local' AND r.Name = 'Admin'
            """, conn);
        var mappingCount = (int)cmdMapping.ExecuteScalar()!;
        Assert.Equal(1, mappingCount);
    }

    [Fact]
    public void AdminUserSeeder_CreatesUserThatCanLogin()
    {
        // Arrange
        var factory = CreateFactory();
        var seederLogger = new LoggerFactory().CreateLogger<AdminUserSeeder>();
        var seeder = new AdminUserSeeder(factory, seederLogger);
        seeder.EnsureAdminUser();

        var authService = CreateAuthService(factory);

        // Act - login immediately after seeding
        var response = authService.LoginAsync("admin@legacy.local", "Admin123!").Result;

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Token);
    }
}