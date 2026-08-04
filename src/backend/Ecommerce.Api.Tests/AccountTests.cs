using System.Data;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Data;
using Ecommerce.Api.Services;
using Microsoft.Data.SqlClient;

namespace Ecommerce.Api.Tests;

/// <summary>
/// Integration tests for the Account API (register + order history) using a real SQL Express database.
/// Requires: .\SQLEXPRESS with LegacyEcommerceDb seeded (AspNetUsers, Orders, OrderItems, Product, CartItem tables).
/// </summary>
public class AccountTests : IDisposable
{
    private readonly string _connectionString = "Server=.\\SQLEXPRESS;Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True";
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly List<string> _registeredUserIds = [];
    private readonly List<int> _createdOrderIds = [];
    private readonly List<(int ProductId, int Quantity)> _restockItems = [];

    public AccountTests()
    {
        _connectionFactory = new SqlConnectionFactory(_connectionString);
    }

    #region Helpers

    private AuthService CreateAuthService() => new(
        _connectionFactory,
        "ThisIsAVeryLongSigningKeyForJWT_32CharsOrMore!",
        "EcommerceModernApi",
        "EcommerceModernClient",
        60
    );

    private AccountService CreateAccountService() => new(_connectionFactory);

    private CheckoutService CreateCheckoutService() => new(_connectionFactory);

    private CartService CreateCartService() => new(_connectionFactory);

    private static string GenerateUniqueEmail() => $"test-{Guid.NewGuid():N}@test.local";

    private async Task<string> RegisterUniqueUserAsync(string? email = null, string password = "Password123!")
    {
        email ??= GenerateUniqueEmail();
        var authService = CreateAuthService();
        var response = await authService.RegisterAsync(email, password);
        Assert.NotNull(response);
        Assert.NotEmpty(response.Token);

        var userId = await GetUserIdByEmailAsync(email);
        Assert.NotNull(userId);
        _registeredUserIds.Add(userId);
        return userId;
    }

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

    private (int ProductId, decimal Price, int Stock)? GetFirstActiveProduct()
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "SELECT TOP 1 ProductId, Price, Stock FROM dbo.Product WHERE IsActive = 1 AND Stock > 0",
            connection);
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return (reader.GetInt32(0), reader.GetDecimal(1), reader.GetInt32(2));
        }
        return null;
    }

    private void InsertCartItem(string userId, int productId, int quantity)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "INSERT INTO dbo.CartItem (UserId, SessionId, ProductId, ProductVariantId, Quantity, AddedDate) " +
            "VALUES (@UserId, NULL, @ProductId, NULL, @Quantity, @AddedDate)",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@ProductId", productId);
        command.Parameters.AddWithValue("@Quantity", quantity);
        command.Parameters.AddWithValue("@AddedDate", DateTime.Now);
        command.ExecuteNonQuery();
    }

    private int InsertTestOrder(string userId, decimal totalAmount)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "INSERT INTO dbo.Orders (UserId, OrderDate, Status, ShippingAddress, TotalAmount) " +
            "OUTPUT INSERTED.OrderId " +
            "VALUES (@UserId, @OrderDate, @Status, @ShippingAddress, @TotalAmount)",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@OrderDate", DateTime.Now);
        command.Parameters.AddWithValue("@Status", "Pending");
        command.Parameters.AddWithValue("@ShippingAddress", "123 Test St (Standard)");
        command.Parameters.AddWithValue("@TotalAmount", totalAmount);

        var orderId = (int)command.ExecuteScalar()!;
        _createdOrderIds.Add(orderId);
        return orderId;
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

    private void DeleteTestOrder(int orderId)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var deleteItems = new SqlCommand(
            "DELETE FROM dbo.OrderItem WHERE OrderId = @OrderId",
            connection);
        deleteItems.Parameters.AddWithValue("@OrderId", orderId);
        deleteItems.ExecuteNonQuery();

        using var deleteOrder = new SqlCommand(
            "DELETE FROM dbo.Orders WHERE OrderId = @OrderId",
            connection);
        deleteOrder.Parameters.AddWithValue("@OrderId", orderId);
        deleteOrder.ExecuteNonQuery();
    }

    private static PlaceOrderRequest CreateValidCheckoutRequest() => new()
    {
        ShippingAddress = "123 Test St, Test City (Standard)",
        ShippingMethod = "Standard",
        CardName = "Test User",
        CardNumber = "4111111111111111",
        CardExpiry = "12/28",
        CardCvv = "123"
    };

    private void RestoreProductStock(int productId, int quantity)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "UPDATE dbo.Product SET Stock = Stock + @Quantity WHERE ProductId = @ProductId",
            connection);
        command.Parameters.AddWithValue("@Quantity", quantity);
        command.Parameters.AddWithValue("@ProductId", productId);
        command.ExecuteNonQuery();
    }

    #endregion

    #region Register Tests

    [Fact]
    public async Task Register_WithNewEmail_ReturnsTokenAndEmptyRoles()
    {
        var email = GenerateUniqueEmail();
        var authService = CreateAuthService();

        var response = await authService.RegisterAsync(email, "Password123!");

        Assert.NotNull(response);
        Assert.NotEmpty(response.Token);
        Assert.Equal(email, response.Email);
        Assert.Empty(response.Roles);

        var userId = await GetUserIdByEmailAsync(email);
        Assert.NotNull(userId);
        _registeredUserIds.Add(userId);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ThrowsArgumentException()
    {
        var email = GenerateUniqueEmail();
        var authService = CreateAuthService();

        var first = await authService.RegisterAsync(email, "Password123!");
        Assert.NotNull(first);

        var userId = await GetUserIdByEmailAsync(email);
        Assert.NotNull(userId);
        _registeredUserIds.Add(userId);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => authService.RegisterAsync(email, "Password123!"));

        Assert.Contains("already", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithShortPassword_ThrowsArgumentException()
    {
        var authService = CreateAuthService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => authService.RegisterAsync(GenerateUniqueEmail(), "123"));

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task Register_WithEmptyEmail_ThrowsArgumentException()
    {
        var authService = CreateAuthService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => authService.RegisterAsync("", "Password123!"));

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task Register_ThenLogin_Works()
    {
        var email = GenerateUniqueEmail();
        var password = "Password123!";
        var authService = CreateAuthService();

        var registerResponse = await authService.RegisterAsync(email, password);
        Assert.NotNull(registerResponse);
        Assert.NotEmpty(registerResponse.Token);

        var userId = await GetUserIdByEmailAsync(email);
        Assert.NotNull(userId);
        _registeredUserIds.Add(userId);

        var loginResponse = await authService.LoginAsync(email, password);
        Assert.NotNull(loginResponse);
        Assert.NotEmpty(loginResponse.Token);
        Assert.Equal(email, loginResponse.Email);
    }

    #endregion

    #region Order History Tests

    [Fact]
    public void GetOrderHistory_ReturnsUserOrders()
    {
        var userId = Guid.NewGuid().ToString("D");
        _registeredUserIds.Add(userId);

        var orderId1 = InsertTestOrder(userId, 29.99m);
        var orderId2 = InsertTestOrder(userId, 49.99m);

        var accountService = CreateAccountService();
        var history = accountService.GetOrderHistory(userId);

        Assert.Equal(2, history.Count);
        Assert.Contains(history, o => o.OrderId == orderId1 && o.TotalAmount == 29.99m);
        Assert.Contains(history, o => o.OrderId == orderId2 && o.TotalAmount == 49.99m);
    }

    [Fact]
    public void GetOrderHistory_EmptyUser_ReturnsEmptyList()
    {
        var emptyUserId = Guid.NewGuid().ToString("D");
        _registeredUserIds.Add(emptyUserId); // Track for cleanup even though no DB rows exist

        var accountService = CreateAccountService();
        var history = accountService.GetOrderHistory(emptyUserId);

        Assert.Empty(history);
    }

    [Fact]
    public void GetOrderDetail_ReturnsOrder_ForOwner()
    {
        var userId = Guid.NewGuid().ToString("D");
        _registeredUserIds.Add(userId);

        var orderId = InsertTestOrder(userId, 99.99m);

        var accountService = CreateAccountService();
        var detail = accountService.GetOrderDetail(orderId, userId);

        Assert.NotNull(detail);
        Assert.Equal(orderId, detail.OrderId);
        Assert.Equal(99.99m, detail.TotalAmount);
        Assert.Equal("Pending", detail.Status);
    }

    [Fact]
    public void GetOrderDetail_ThrowsKeyNotFoundException_ForNonOwner()
    {
        var userIdA = Guid.NewGuid().ToString("D");
        var userIdB = Guid.NewGuid().ToString("D");
        _registeredUserIds.Add(userIdA);
        _registeredUserIds.Add(userIdB);

        var orderId = InsertTestOrder(userIdA, 19.99m);

        var accountService = CreateAccountService();
        Assert.Throws<KeyNotFoundException>(() => accountService.GetOrderDetail(orderId, userIdB));
    }

    [Fact]
    public void GetOrderDetail_ThrowsKeyNotFoundException_ForNonExistentOrder()
    {
        var userId = Guid.NewGuid().ToString("D");
        _registeredUserIds.Add(userId);

        var accountService = CreateAccountService();
        Assert.Throws<KeyNotFoundException>(() => accountService.GetOrderDetail(int.MaxValue, userId));
    }

    [Fact]
    public void GetOrderHistory_ViaCheckout_ReturnsCreatedOrders()
    {
        var userId = Guid.NewGuid().ToString("D");
        _registeredUserIds.Add(userId);

        var product = GetFirstActiveProduct();
        if (!product.HasValue)
        {
            return; // Skip if no seed products are available
        }

        var checkoutService = CreateCheckoutService();
        var cartService = CreateCartService();

        InsertCartItem(userId, product.Value.ProductId, 1);
        var confirmation = checkoutService.PlaceOrder(userId, CreateValidCheckoutRequest());
        _createdOrderIds.Add(confirmation.OrderId);
        _restockItems.Add((product.Value.ProductId, 1));

        var accountService = CreateAccountService();
        var history = accountService.GetOrderHistory(userId);

        Assert.Single(history);
        Assert.Equal(confirmation.OrderId, history[0].OrderId);
        Assert.Equal(confirmation.TotalAmount, history[0].TotalAmount);
        Assert.Equal(1, history[0].ItemCount);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        foreach (var orderId in _createdOrderIds)
        {
            DeleteTestOrder(orderId);
        }

        foreach (var (productId, quantity) in _restockItems)
        {
            RestoreProductStock(productId, quantity);
        }

        foreach (var userId in _registeredUserIds)
        {
            DeleteTestUser(userId);

            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            using var cartCommand = new SqlCommand(
                "DELETE FROM dbo.CartItem WHERE UserId = @UserId",
                connection);
            cartCommand.Parameters.AddWithValue("@UserId", userId);
            cartCommand.ExecuteNonQuery();
        }
    }

    #endregion
}
