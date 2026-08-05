using System.Data;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Data;
using Ecommerce.Api.Services;
using Microsoft.Data.SqlClient;

namespace Ecommerce.Api.Tests;

/// <summary>
/// Integration tests for the CheckoutService using a real SQL Express database.
/// Requires: .\SQLEXPRESS with LegacyEcommerceDb seeded.
/// </summary>
public class CheckoutTests : IDisposable
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly CartService _cartService;
    private readonly CheckoutService _service;
    private readonly List<string> _userIds = [];
    private readonly List<int> _orderIds = [];

    public CheckoutTests()
    {
        var connectionString = "Server=.\\SQLEXPRESS;Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True";
        _connectionFactory = new SqlConnectionFactory(connectionString);
        _cartService = new CartService(_connectionFactory);
        _service = new CheckoutService(_connectionFactory);
    }

    [Fact]
    public void GetShippingOptions_ReturnsStandardAndExpress()
    {
        var options = _service.GetShippingOptions();

        Assert.Equal(2, options.Count);
        Assert.Contains(options, o => o.Code == "Standard");
        Assert.Contains(options, o => o.Code == "Express");
    }

    [Fact]
    public void PlaceOrder_WithValidCart_CreatesOrder()
    {
        var userId = RegisterUser();
        var product = GetFirstActiveProduct();
        if (!product.HasValue) return;

        InsertCartItem(userId, product.Value.ProductId, 1, null);

        var request = CreateValidRequest();
        var confirmation = _service.PlaceOrder(userId, request);
        _orderIds.Add(confirmation.OrderId);

        Assert.True(confirmation.OrderId > 0);
        Assert.Equal(product.Value.Price, confirmation.TotalAmount);
        Assert.Single(confirmation.Items);
        Assert.Equal(product.Value.ProductId, confirmation.Items[0].ProductId);
        Assert.Equal(1, confirmation.Items[0].Quantity);
    }

    [Fact]
    public void PlaceOrder_ClearsCartAfterOrder()
    {
        var userId = RegisterUser();
        var product = GetFirstActiveProduct();
        if (!product.HasValue) return;

        InsertCartItem(userId, product.Value.ProductId, 1, null);

        var confirmation = _service.PlaceOrder(userId, CreateValidRequest());
        _orderIds.Add(confirmation.OrderId);

        var cart = _cartService.GetCart(userId, false);
        Assert.Empty(cart.Items);
        Assert.Equal(0, _cartService.GetItemCount(userId, false));
    }

    [Fact]
    public void PlaceOrder_EmptyCart_ThrowsInvalidOperationException()
    {
        var userId = RegisterUser();

        var exception = Assert.Throws<InvalidOperationException>(
            () => _service.PlaceOrder(userId, CreateValidRequest()));

        Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlaceOrder_InvalidShipping_ThrowsArgumentException()
    {
        var userId = RegisterUser();
        var request = CreateValidRequest();
        request.ShippingMethod = "Overnight";

        Assert.Throws<ArgumentException>(() => _service.PlaceOrder(userId, request));
    }

    [Fact]
    public void PlaceOrder_InvalidCardNumber_ThrowsArgumentException()
    {
        var userId = RegisterUser();
        var request = CreateValidRequest();
        request.CardNumber = "123";

        Assert.Throws<ArgumentException>(() => _service.PlaceOrder(userId, request));
    }

    [Fact]
    public void PlaceOrder_InvalidExpiry_ThrowsArgumentException()
    {
        var userId = RegisterUser();
        var request = CreateValidRequest();
        request.CardExpiry = "13/xx";

        Assert.Throws<ArgumentException>(() => _service.PlaceOrder(userId, request));
    }

    [Fact]
    public void PlaceOrder_InvalidCvv_ThrowsArgumentException()
    {
        var userId = RegisterUser();
        var request = CreateValidRequest();
        request.CardCvv = "1";

        Assert.Throws<ArgumentException>(() => _service.PlaceOrder(userId, request));
    }

    [Fact]
    public void PlaceOrder_Oversell_ThrowsInvalidOperationException()
    {
        var userId = RegisterUser();
        var product = GetFirstActiveProduct();
        if (!product.HasValue) return;

        // Use a fixed oversell quantity that exceeds any selected product's 10-unit threshold.
        var quantity = product.Value.Stock + 5;
        InsertCartItem(userId, product.Value.ProductId, quantity, null);

        var exception = Assert.Throws<InvalidOperationException>(
            () => _service.PlaceOrder(userId, CreateValidRequest()));

        Assert.Contains("stock", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetOrderDetail_ReturnsOrder_ForOwner()
    {
        var userId = RegisterUser();
        var product = GetFirstActiveProduct();
        if (!product.HasValue) return;

        InsertCartItem(userId, product.Value.ProductId, 1, null);

        var confirmation = _service.PlaceOrder(userId, CreateValidRequest());
        _orderIds.Add(confirmation.OrderId);

        var detail = _service.GetOrderDetail(confirmation.OrderId, userId);

        Assert.Equal(confirmation.OrderId, detail.OrderId);
        Assert.Equal(confirmation.TotalAmount, detail.TotalAmount);
        Assert.Single(detail.Items);
    }

    [Fact]
    public void GetOrderDetail_ThrowsKeyNotFoundException_ForNonOwner()
    {
        var userIdA = RegisterUser();
        var userIdB = RegisterUser();
        var product = GetFirstActiveProduct();
        if (!product.HasValue) return;

        InsertCartItem(userIdA, product.Value.ProductId, 1, null);

        var confirmation = _service.PlaceOrder(userIdA, CreateValidRequest());
        _orderIds.Add(confirmation.OrderId);

        Assert.Throws<KeyNotFoundException>(() => _service.GetOrderDetail(confirmation.OrderId, userIdB));
    }

    private string RegisterUser()
    {
        var userId = Guid.NewGuid().ToString("D");
        _userIds.Add(userId);
        return userId;
    }

    private static PlaceOrderRequest CreateValidRequest()
    {
        return new PlaceOrderRequest
        {
            ShippingAddress = "123 Test St, Test City (Standard)",
            ShippingMethod = "Standard",
            CardName = "Test User",
            CardNumber = "4111111111111111",
            CardExpiry = "12/28",
            CardCvv = "123"
        };
    }

    // Test isolation: pick a product with at least 10 stock so oversell and concurrent tests
    // are not blocked by other tests having depleted the first positive-stock SKU.
    private (int ProductId, decimal Price, int Stock)? GetFirstActiveProduct()
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "SELECT TOP 1 ProductId, Price, Stock FROM dbo.Product WHERE IsActive = 1 AND Stock >= 10 ORDER BY NEWID()",
            connection);
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return (reader.GetInt32(0), reader.GetDecimal(1), reader.GetInt32(2));
        }
        return null;
    }

    private (int VariantId, int ProductId)? GetFirstVariant()
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "SELECT TOP 1 pv.ProductVariantId, pv.ProductId " +
            "FROM dbo.ProductVariant pv INNER JOIN dbo.Product p ON pv.ProductId = p.ProductId " +
            "WHERE p.IsActive = 1 AND pv.Stock >= 10 ORDER BY NEWID()",
            connection);
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return (reader.GetInt32(0), reader.GetInt32(1));
        }
        return null;
    }

    private void InsertCartItem(string userId, int productId, int quantity, int? variantId)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "INSERT INTO dbo.CartItem (UserId, SessionId, ProductId, ProductVariantId, Quantity, AddedDate) " +
            "VALUES (@UserId, NULL, @ProductId, @ProductVariantId, @Quantity, @AddedDate)",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@ProductId", productId);
        command.Parameters.AddWithValue("@ProductVariantId", variantId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Quantity", quantity);
        command.Parameters.AddWithValue("@AddedDate", DateTime.Now);
        command.ExecuteNonQuery();
    }

    private void DeleteOrder(int orderId)
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

    private void RestoreVariantStock(int variantId, int quantity)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "UPDATE dbo.ProductVariant SET Stock = Stock + @Quantity WHERE ProductVariantId = @VariantId",
            connection);
        command.Parameters.AddWithValue("@Quantity", quantity);
        command.Parameters.AddWithValue("@VariantId", variantId);
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        foreach (var orderId in _orderIds)
        {
            DeleteOrder(orderId);
        }

        foreach (var userId in _userIds)
        {
            using var cartCommand = new SqlCommand(
                "DELETE FROM dbo.CartItem WHERE UserId = @UserId OR SessionId = @UserId",
                connection);
            cartCommand.Parameters.AddWithValue("@UserId", userId);
            cartCommand.ExecuteNonQuery();
        }
    }
}
