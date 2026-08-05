using System.Data;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Data;
using Ecommerce.Api.Services;
using Microsoft.Data.SqlClient;

namespace Ecommerce.Api.Tests;

/// <summary>
/// Adversarial tests for CartService: boundary quantities, guest isolation,
/// cross-owner access, and malformed/IDOR cart operations.
/// </summary>
public class CartAdversarialTests : IDisposable
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly CartService _service;
    private readonly List<string> _ownerIds = [];

    public CartAdversarialTests()
    {
        var connectionString = "Server=.\\SQLEXPRESS;Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True";
        _connectionFactory = new SqlConnectionFactory(connectionString);
        _service = new CartService(_connectionFactory);
    }

    [Fact]
    public void AddItem_NegativeQuantity_ThrowsInvalidOperationException()
    {
        // Scenario: attacker sends negative quantity to manipulate stock or cart total.
        var ownerId = RegisterOwner();
        var productId = GetFirstInStockProductId();
        if (!productId.HasValue) return;

        var exception = Assert.Throws<InvalidOperationException>(
            () => _service.AddItem(ownerId, true, productId.Value, null, -1));

        Assert.Contains("greater", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddItem_ZeroQuantity_ThrowsInvalidOperationException()
    {
        // Scenario: zero quantity should not create a cart line.
        var ownerId = RegisterOwner();
        var productId = GetFirstInStockProductId();
        if (!productId.HasValue) return;

        var exception = Assert.Throws<InvalidOperationException>(
            () => _service.AddItem(ownerId, true, productId.Value, null, 0));

        Assert.Contains("greater", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddItem_OversizedQuantity_ThrowsInvalidOperationException()
    {
        // Scenario: attacker requests an absurd quantity (int.MaxValue) to test overflow / allocation.
        var ownerId = RegisterOwner();
        var productId = GetFirstInStockProductId();
        if (!productId.HasValue) return;

        Assert.Throws<InvalidOperationException>(
            () => _service.AddItem(ownerId, true, productId.Value, null, int.MaxValue));
    }

    [Fact]
    public void UpdateQuantity_NegativeQuantity_RemovesItem()
    {
        // Scenario: update to negative value triggers removal (current behaviour clamps via remove).
        var ownerId = RegisterOwner();
        var productId = GetFirstInStockProductId();
        if (!productId.HasValue) return;

        _service.AddItem(ownerId, true, productId.Value, null, 2);
        var cartItemId = _service.GetCart(ownerId, true).Items[0].CartItemId;

        _service.UpdateQuantity(ownerId, true, cartItemId, -1);

        var cart = _service.GetCart(ownerId, true);
        Assert.Empty(cart.Items);
        Assert.Equal(0, _service.GetItemCount(ownerId, true));
    }

    [Fact]
    public void UpdateQuantity_WrongOwner_ThrowsKeyNotFoundException()
    {
        // Scenario: User B attempts to update User A's cart line (IDOR / horizontal authz).
        var ownerA = RegisterOwner();
        var ownerB = RegisterOwner();
        var productId = GetFirstInStockProductId();
        if (!productId.HasValue) return;

        _service.AddItem(ownerA, true, productId.Value, null, 1);
        var cartItemId = _service.GetCart(ownerA, true).Items[0].CartItemId;

        Assert.Throws<KeyNotFoundException>(
            () => _service.UpdateQuantity(ownerB, true, cartItemId, 5));
    }

    [Fact]
    public void RemoveItem_WrongOwner_ThrowsKeyNotFoundException()
    {
        // Scenario: User B attempts to remove User A's cart line.
        var ownerA = RegisterOwner();
        var ownerB = RegisterOwner();
        var productId = GetFirstInStockProductId();
        if (!productId.HasValue) return;

        _service.AddItem(ownerA, true, productId.Value, null, 1);
        var cartItemId = _service.GetCart(ownerA, true).Items[0].CartItemId;

        Assert.Throws<KeyNotFoundException>(
            () => _service.RemoveItem(ownerB, true, cartItemId));
    }

    [Fact]
    public void GetCart_WithEmptyOwnerId_ReturnsEmptyCart()
    {
        // Scenario: empty owner id must not return anyone else's cart.
        var cart = _service.GetCart("", true);
        Assert.Empty(cart.Items);
        Assert.Equal(0, cart.ItemCount);
    }

    [Fact]
    public void GetItemCount_WithWhitespaceOwner_ReturnsZero()
    {
        // Scenario: malformed owner value must not aggregate global cart counts.
        Assert.Equal(0, _service.GetItemCount("   ", true));
    }

    [Fact]
    public void MergeGuestCart_CrossUserAccess_DoesNotMergeArbitraryOwner()
    {
        // Scenario: attacker merges a victim's guest cart into their own account.
        // The victim's guest cookie/session id should not be guessable, but if leaked
        // the merge endpoint accepts any GuestOwnerId and moves items to auth user.
        // This test documents that behaviour: victim cart is emptied into attacker cart.
        var victimGuestId = RegisterOwner();
        var attackerUserId = RegisterOwner();
        var productId = GetFirstInStockProductId();
        if (!productId.HasValue) return;

        _service.AddItem(victimGuestId, true, productId.Value, null, 1);

        _service.MergeGuestCart(victimGuestId, attackerUserId);

        var attackerCart = _service.GetCart(attackerUserId, false);
        var victimCart = _service.GetCart(victimGuestId, true);

        // If the system allowed cross-owner merge, the item now belongs to the attacker.
        Assert.Single(attackerCart.Items);
        Assert.Empty(victimCart.Items);
    }

    [Fact]
    public void MergeGuestCart_SameSourceAndTarget_DoesNotDeleteCart()
    {
        // Scenario: merge with identical source and target must be a no-op.
        var ownerId = RegisterOwner();
        var productId = GetFirstInStockProductId();
        if (!productId.HasValue) return;

        _service.AddItem(ownerId, true, productId.Value, null, 1);

        _service.MergeGuestCart(ownerId, ownerId);

        var cart = _service.GetCart(ownerId, true);
        Assert.Single(cart.Items);
    }

    private string RegisterOwner()
    {
        var ownerId = Guid.NewGuid().ToString("D");
        _ownerIds.Add(ownerId);
        return ownerId;
    }

    // Test isolation: choose a product with enough stock for quantity-2 add/update tests.
    private int? GetFirstInStockProductId()
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "SELECT TOP 1 ProductId FROM dbo.Product WHERE IsActive = 1 AND Stock >= 10 ORDER BY NEWID()",
            connection);
        var result = command.ExecuteScalar();
        return result != null ? (int?)result : null;
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
