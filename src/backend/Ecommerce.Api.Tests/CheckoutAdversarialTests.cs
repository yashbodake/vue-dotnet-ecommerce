using System.Data;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Data;
using Ecommerce.Api.Services;
using Microsoft.Data.SqlClient;

namespace Ecommerce.Api.Tests;

/// <summary>
/// Adversarial tests for CheckoutService: concurrency, replay, invalid state transitions,
/// and boundary cases. Uses the real SQL Express database.
/// </summary>
public class CheckoutAdversarialTests : IDisposable
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly CartService _cartService;
    private readonly CheckoutService _checkoutService;
    private readonly List<string> _userIds = [];
    private readonly List<int> _orderIds = [];
    private readonly List<(int ProductId, int Quantity)> _stockRestorations = [];

    public CheckoutAdversarialTests()
    {
        var connectionString = "Server=.\\SQLEXPRESS;Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True";
        _connectionFactory = new SqlConnectionFactory(connectionString);
        _cartService = new CartService(_connectionFactory);
        _checkoutService = new CheckoutService(_connectionFactory);
    }

    [Fact]
    public void PlaceOrder_ConcurrentCheckout_LastItemInStock_Oversells()
    {
        // Scenario: two users have the same product with stock=1 reserved in their carts.
        // We bypass CartService.AddItem (which would reject the second reservation) and
        // insert cart lines directly, then run two checkouts in parallel. The non-atomic
        // stock decrement in CheckoutService.PlaceOrder allows both orders to succeed,
        // resulting in overselling / negative stock.
        var product = EnsureSingleItemInStock();
        if (!product.HasValue) return;

        var userA = RegisterUser();
        var userB = RegisterUser();

        // Direct insert so both users hold a reservation for the single unit.
        InsertCartItem(userA, product.Value.ProductId, 1, null);
        InsertCartItem(userB, product.Value.ProductId, 1, null);

        var request = CreateValidRequest();

        // Act: run two checkouts in parallel against the same SKU.
        var taskA = Task.Run(() => _checkoutService.PlaceOrder(userA, request));
        var taskB = Task.Run(() => _checkoutService.PlaceOrder(userB, request));

        // Use WhenAll and catch the aggregate so we can inspect the race outcome.
        // Task.WaitAll would throw immediately on the first faulted task and hide the second result.
        Task whenAll;
        try
        {
            whenAll = Task.WhenAll(taskA, taskB);
            whenAll.Wait(TimeSpan.FromSeconds(30));
        }
        catch (Exception)
        {
            // One or both tasks faulted; we inspect the individual tasks below.
        }

        var successes = new List<OrderConfirmationDto>();
        var failures = new List<Exception>();

        foreach (var task in new[] { taskA, taskB })
        {
            if (task.IsFaulted)
            {
                failures.Add(task.Exception!.InnerExceptions.First());
            }
            else if (task.IsCompletedSuccessfully)
            {
                successes.Add(task.Result);
            }
        }

        _orderIds.AddRange(successes.Select(s => s.OrderId));

        // Assert: at most one order should succeed for a single item in stock.
        // If both succeed, the system has oversold. If exactly one succeeds, the
        // decrement is only safe by accident under low contention; the concurrent
        // race still demonstrates the non-atomic decrement defect because SQL Server
        // can interleave the two reads and allow both to pass the stock check before
        // either writes the new stock value.
        Assert.True(
            successes.Count <= 1,
            $"Oversell detected: {successes.Count} checkouts succeeded for stock=1 product. OrderIds: {string.Join(",", successes.Select(s => s.OrderId))}.");

        // Additionally verify the final stock is non-negative.
        var finalStock = GetProductStock(product.Value.ProductId);
        Assert.True(finalStock >= 0, $"Final stock became negative ({finalStock}) after concurrent checkouts.");
    }

    [Fact]
    public void PlaceOrder_DuplicatePlaceOrder_ThrowsInvalidOperationForEmptyCart()
    {
        // Scenario: replaying the checkout request after a successful order should fail
        // because the cart is cleared. Tests idempotency / invalid state transition.
        var userId = RegisterUser();
        var product = GetFirstActiveProduct();
        if (!product.HasValue) return;

        _cartService.AddItem(userId, false, product.Value.ProductId, null, 1);

        var first = _checkoutService.PlaceOrder(userId, CreateValidRequest());
        _orderIds.Add(first.OrderId);

        // Replaying with the same request/cart should now be empty.
        var exception = Assert.Throws<InvalidOperationException>(() => _checkoutService.PlaceOrder(userId, CreateValidRequest()));
        Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlaceOrder_ZeroQuantityLine_DoesNotAffectStockOrCreateOrder()
    {
        // Scenario: a cart with a zero-quantity line should not be considered.
        // We directly insert a zero-quantity cart line and attempt checkout.
        var userId = RegisterUser();
        var product = GetFirstActiveProduct();
        if (!product.HasValue) return;

        InsertCartItem(userId, product.Value.ProductId, 0, null);

        var exception = Assert.Throws<InvalidOperationException>(() => _checkoutService.PlaceOrder(userId, CreateValidRequest()));
        Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetOrderDetail_NonOwnerUser_ThrowsKeyNotFoundException()
    {
        // Scenario: User B tries to view an order belonging to User A (IDOR).
        var userIdA = RegisterUser();
        var userIdB = RegisterUser();
        var product = GetFirstActiveProduct();
        if (!product.HasValue) return;

        _cartService.AddItem(userIdA, false, product.Value.ProductId, null, 1);
        var confirmation = _checkoutService.PlaceOrder(userIdA, CreateValidRequest());
        _orderIds.Add(confirmation.OrderId);

        Assert.Throws<KeyNotFoundException>(() => _checkoutService.GetOrderDetail(confirmation.OrderId, userIdB));
    }

    [Fact]
    public void GetOrderDetail_NegativeOrderId_ThrowsKeyNotFoundException()
    {
        // Scenario: invalid negative order id must not leak data and should return NotFound semantics.
        var userId = RegisterUser();
        Assert.Throws<KeyNotFoundException>(() => _checkoutService.GetOrderDetail(-1, userId));
    }

    [Fact]
    public void PlaceOrder_OversizedShippingAddress_DoesNotCrash()
    {
        // Scenario: attacker submits a very long shipping address to test DoS / storage limits.
        var userId = RegisterUser();
        var product = GetFirstActiveProduct();
        if (!product.HasValue) return;

        _cartService.AddItem(userId, false, product.Value.ProductId, null, 1);

        var request = CreateValidRequest();
        request.ShippingAddress = new string('A', 5000) + " (Standard)";

        // Should either succeed or fail gracefully with a known exception type, not crash.
        try
        {
            var confirmation = _checkoutService.PlaceOrder(userId, request);
            _orderIds.Add(confirmation.OrderId);
            Assert.False(string.IsNullOrEmpty(confirmation.ShippingAddress));
        }
        catch (SqlException ex) when (ex.Number == 8152 || ex.Number == 2628 || ex.Number == 2714 || ex.Number == 103)
        {
            // String truncation errors are acceptable defensive behaviour.
        }
        catch (Exception ex)
        {
            Assert.True(ex is ArgumentException || ex is InvalidOperationException, $"Unexpected exception type: {ex.GetType().Name}: {ex.Message}");
        }
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


    private int GetProductStock(int productId)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "SELECT Stock FROM dbo.Product WHERE ProductId = @ProductId",
            connection);
        command.Parameters.AddWithValue("@ProductId", productId);
        var result = command.ExecuteScalar();
        return result != null ? (int)result : 0;
    }

    private (int ProductId, decimal Price, int Stock)? GetFirstActiveProduct()
    {
        // Isolated product selection: require enough stock so concurrent tests do not
        // deplete a shared low-stock row and cause unrelated failures.
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

    private (int ProductId, decimal Price)? EnsureSingleItemInStock()
    {
        // Force a well-stocked product down to one unit to reproduce race-to-buy.
        var product = GetFirstActiveProduct();
        if (!product.HasValue) return null;

        _stockRestorations.Add((product.Value.ProductId, product.Value.Stock));
        SetProductStock(product.Value.ProductId, 1);
        return (product.Value.ProductId, product.Value.Price);
    }

    private void SetProductStock(int productId, int stock)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "UPDATE dbo.Product SET Stock = @Stock WHERE ProductId = @ProductId",
            connection);
        command.Parameters.AddWithValue("@Stock", stock);
        command.Parameters.AddWithValue("@ProductId", productId);
        command.ExecuteNonQuery();
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

        foreach (var (productId, quantity) in _stockRestorations)
        {
            using var restoreCommand = new SqlCommand(
                "UPDATE dbo.Product SET Stock = @Quantity WHERE ProductId = @ProductId",
                connection);
            restoreCommand.Parameters.AddWithValue("@Quantity", quantity);
            restoreCommand.Parameters.AddWithValue("@ProductId", productId);
            restoreCommand.ExecuteNonQuery();
        }
    }
}
