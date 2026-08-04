using System.Data;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Data;
using Ecommerce.Api.Services;
using Microsoft.Data.SqlClient;

namespace Ecommerce.Api.Tests;

/// <summary>
/// Integration tests for the CartService using a real SQL Express database.
/// Requires: .\SQLEXPRESS with LegacyEcommerceDb seeded.
/// </summary>
public class CartTests : IDisposable
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly CartService _service;
    private readonly List<string> _ownerIds = [];

    public CartTests()
    {
        var connectionString = "Server=.\\SQLEXPRESS;Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True";
        _connectionFactory = new SqlConnectionFactory(connectionString);
        _service = new CartService(_connectionFactory);
    }

    [Fact]
    public void AddItem_GuestCart_CreatesLineAndReturnsCorrectCount()
    {
        var ownerId = RegisterOwner();
        var productId = GetFirstInStockProductId();
        if (!productId.HasValue) return;

        _service.AddItem(ownerId, true, productId.Value, null, 2);

        var cart = _service.GetCart(ownerId, true);
        Assert.Single(cart.Items);
        Assert.Equal(2, cart.Items[0].Quantity);
        Assert.Equal(2, _service.GetItemCount(ownerId, true));
    }

    [Fact]
    public void AddItem_AuthenticatedUserCart_CreatesLine()
    {
        var ownerId = RegisterOwner();
        var productId = GetFirstInStockProductId();
        if (!productId.HasValue) return;

        _service.AddItem(ownerId, false, productId.Value, null, 3);

        var cart = _service.GetCart(ownerId, false);
        Assert.Single(cart.Items);
        Assert.Equal(3, cart.Items[0].Quantity);
    }

    [Fact]
    public void AddItem_MergesExistingLine_WhenSameProductVariant()
    {
        var ownerId = RegisterOwner();
        var productId = GetFirstInStockProductId();
        if (!productId.HasValue) return;

        _service.AddItem(ownerId, true, productId.Value, null, 2);
        _service.AddItem(ownerId, true, productId.Value, null, 3);

        var cart = _service.GetCart(ownerId, true);
        Assert.Single(cart.Items);
        Assert.Equal(5, cart.Items[0].Quantity);
        Assert.Equal(5, _service.GetItemCount(ownerId, true));
    }

    [Fact]
    public void AddItem_WithVariant_CreatesLineWithVariantDetails()
    {
        var ownerId = RegisterOwner();
        var variant = GetFirstInStockVariant();
        if (!variant.HasValue) return;

        _service.AddItem(ownerId, true, variant.Value.ProductId, variant.Value.VariantId, 1);

        var cart = _service.GetCart(ownerId, true);
        Assert.Single(cart.Items);
        Assert.NotNull(cart.Items[0].VariantId);
        Assert.Equal(variant.Value.VariantId, cart.Items[0].VariantId);
        Assert.False(string.IsNullOrEmpty(cart.Items[0].VariantName));
    }

    [Fact]
    public void AddItem_Oversell_ThrowsInvalidOperationException()
    {
        var ownerId = RegisterOwner();
        var product = GetFirstInStockProductWithStock();
        if (!product.HasValue) return;

        var exception = Assert.Throws<InvalidOperationException>(
            () => _service.AddItem(ownerId, true, product.Value.ProductId, null, product.Value.Stock + 1));

        Assert.Contains("stock", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddItem_InactiveProduct_ThrowsInvalidOperationException()
    {
        var ownerId = RegisterOwner();
        var productId = GetFirstInactiveProductId();
        if (!productId.HasValue) return;

        var exception = Assert.Throws<InvalidOperationException>(
            () => _service.AddItem(ownerId, true, productId.Value, null, 1));

        Assert.Contains("available", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateQuantity_ChangesQuantity()
    {
        var ownerId = RegisterOwner();
        var productId = GetFirstInStockProductId();
        if (!productId.HasValue) return;

        _service.AddItem(ownerId, true, productId.Value, null, 2);
        var cartItemId = _service.GetCart(ownerId, true).Items[0].CartItemId;

        _service.UpdateQuantity(ownerId, true, cartItemId, 7);

        var cart = _service.GetCart(ownerId, true);
        Assert.Single(cart.Items);
        Assert.Equal(7, cart.Items[0].Quantity);
    }

    [Fact]
    public void UpdateQuantity_ToZero_RemovesItem()
    {
        var ownerId = RegisterOwner();
        var productId = GetFirstInStockProductId();
        if (!productId.HasValue) return;

        _service.AddItem(ownerId, true, productId.Value, null, 2);
        var cartItemId = _service.GetCart(ownerId, true).Items[0].CartItemId;

        _service.UpdateQuantity(ownerId, true, cartItemId, 0);

        var cart = _service.GetCart(ownerId, true);
        Assert.Empty(cart.Items);
        Assert.Equal(0, _service.GetItemCount(ownerId, true));
    }

    [Fact]
    public void UpdateQuantity_Oversell_ThrowsInvalidOperationException()
    {
        var ownerId = RegisterOwner();
        var product = GetFirstInStockProductWithStock();
        if (!product.HasValue) return;

        _service.AddItem(ownerId, true, product.Value.ProductId, null, 1);
        var cartItemId = _service.GetCart(ownerId, true).Items[0].CartItemId;

        var exception = Assert.Throws<InvalidOperationException>(
            () => _service.UpdateQuantity(ownerId, true, cartItemId, product.Value.Stock + 1));

        Assert.Contains("stock", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateQuantity_NotFound_ThrowsKeyNotFoundException()
    {
        var ownerId = RegisterOwner();
        Assert.Throws<KeyNotFoundException>(() => _service.UpdateQuantity(ownerId, true, -1, 1));
    }

    [Fact]
    public void RemoveItem_RemovesFromCart()
    {
        var ownerId = RegisterOwner();
        var productId = GetFirstInStockProductId();
        if (!productId.HasValue) return;

        _service.AddItem(ownerId, true, productId.Value, null, 2);
        var cartItemId = _service.GetCart(ownerId, true).Items[0].CartItemId;

        _service.RemoveItem(ownerId, true, cartItemId);

        var cart = _service.GetCart(ownerId, true);
        Assert.Empty(cart.Items);
        Assert.Equal(0, _service.GetItemCount(ownerId, true));
    }

    [Fact]
    public void RemoveItem_NotFound_ThrowsKeyNotFoundException()
    {
        var ownerId = RegisterOwner();
        Assert.Throws<KeyNotFoundException>(() => _service.RemoveItem(ownerId, true, -1));
    }

    [Fact]
    public void MergeGuestCart_MovesGuestItemsToUserCart()
    {
        var guestOwnerId = RegisterOwner();
        var authUserId = RegisterOwner();
        var productId = GetFirstInStockProductId();
        if (!productId.HasValue) return;

        _service.AddItem(guestOwnerId, true, productId.Value, null, 3);
        _service.MergeGuestCart(guestOwnerId, authUserId);

        var guestCart = _service.GetCart(guestOwnerId, true);
        var userCart = _service.GetCart(authUserId, false);

        Assert.Empty(guestCart.Items);
        Assert.Single(userCart.Items);
        Assert.Equal(3, userCart.Items[0].Quantity);
    }

    [Fact]
    public void MergeGuestCart_SumsQuantities_WhenSameProductVariant()
    {
        var guestOwnerId = RegisterOwner();
        var authUserId = RegisterOwner();
        var productId = GetFirstInStockProductId();
        if (!productId.HasValue) return;

        _service.AddItem(guestOwnerId, true, productId.Value, null, 2);
        _service.AddItem(authUserId, false, productId.Value, null, 4);
        _service.MergeGuestCart(guestOwnerId, authUserId);

        var guestCart = _service.GetCart(guestOwnerId, true);
        var userCart = _service.GetCart(authUserId, false);

        Assert.Empty(guestCart.Items);
        Assert.Single(userCart.Items);
        Assert.Equal(6, userCart.Items[0].Quantity);
    }

    [Fact]
    public void ClearCart_RemovesAllItems()
    {
        var ownerId = RegisterOwner();
        var productId = GetFirstInStockProductId();
        if (!productId.HasValue) return;

        _service.AddItem(ownerId, true, productId.Value, null, 1);
        _service.AddItem(ownerId, true, productId.Value, null, 1);

        _service.ClearCart(ownerId, true);

        var cart = _service.GetCart(ownerId, true);
        Assert.Empty(cart.Items);
        Assert.Equal(0, _service.GetItemCount(ownerId, true));
    }

    private string RegisterOwner()
    {
        var ownerId = Guid.NewGuid().ToString("D");
        _ownerIds.Add(ownerId);
        return ownerId;
    }

    private int? GetFirstInStockProductId()
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "SELECT TOP 1 ProductId FROM dbo.Product WHERE IsActive = 1 AND Stock > 0",
            connection);
        var result = command.ExecuteScalar();
        return result != null ? (int?)result : null;
    }

    private (int ProductId, int Stock)? GetFirstInStockProductWithStock()
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "SELECT TOP 1 ProductId, Stock FROM dbo.Product WHERE IsActive = 1 AND Stock > 0",
            connection);
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return (reader.GetInt32(0), reader.GetInt32(1));
        }
        return null;
    }

    private int? GetFirstInactiveProductId()
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "SELECT TOP 1 ProductId FROM dbo.Product WHERE IsActive = 0",
            connection);
        var result = command.ExecuteScalar();
        return result != null ? (int?)result : null;
    }

    private (int ProductId, int VariantId, int Stock)? GetFirstInStockVariant()
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "SELECT TOP 1 pv.ProductId, pv.ProductVariantId, pv.Stock " +
            "FROM dbo.ProductVariant pv INNER JOIN dbo.Product p ON pv.ProductId = p.ProductId " +
            "WHERE p.IsActive = 1 AND pv.Stock > 0",
            connection);
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2));
        }
        return null;
    }

    public void Dispose()
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        foreach (var ownerId in _ownerIds)
        {
            using var sessionCommand = new SqlCommand(
                "DELETE FROM dbo.CartItem WHERE SessionId = @OwnerId",
                connection);
            sessionCommand.Parameters.AddWithValue("@OwnerId", ownerId);
            sessionCommand.ExecuteNonQuery();

            using var userCommand = new SqlCommand(
                "DELETE FROM dbo.CartItem WHERE UserId = @OwnerId",
                connection);
            userCommand.Parameters.AddWithValue("@OwnerId", ownerId);
            userCommand.ExecuteNonQuery();
        }
    }
}
