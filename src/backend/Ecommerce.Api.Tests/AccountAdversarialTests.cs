using Ecommerce.Api.Data;
using Ecommerce.Api.Services;
using Microsoft.Data.SqlClient;

namespace Ecommerce.Api.Tests;

/// <summary>
/// Adversarial tests for AccountService: IDOR-style order detail access, empty/null user id,
/// and SQL injection patterns in the user id parameter.
/// </summary>
public class AccountAdversarialTests : IDisposable
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly AccountService _service;
    private readonly List<int> _createdOrderIds = [];

    public AccountAdversarialTests()
    {
        var connectionString = "Server=.\\SQLEXPRESS;Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True";
        _connectionFactory = new SqlConnectionFactory(connectionString);
        _service = new AccountService(_connectionFactory);
    }

    private int InsertTestOrder(string userId, string status)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            """
            INSERT INTO dbo.Orders (UserId, OrderDate, Status, ShippingAddress, TotalAmount)
            OUTPUT INSERTED.OrderId
            VALUES (@UserId, @OrderDate, @Status, @ShippingAddress, @TotalAmount)
            """,
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@OrderDate", DateTime.Now);
        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@ShippingAddress", "123 Account Test St (Standard)");
        command.Parameters.AddWithValue("@TotalAmount", 9.99m);

        var orderId = (int)command.ExecuteScalar()!;
        _createdOrderIds.Add(orderId);
        return orderId;
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

    [Fact]
    public void GetOrderHistory_EmptyUserId_ThrowsArgumentException()
    {
        // Scenario: empty or missing user id must be rejected at the service boundary.
        Assert.Throws<ArgumentException>(() => _service.GetOrderHistory(""));
    }

    [Fact]
    public void GetOrderHistory_WhitespaceUserId_ThrowsArgumentException()
    {
        // Scenario: whitespace-only user id must be rejected.
        Assert.Throws<ArgumentException>(() => _service.GetOrderHistory("   "));
    }

    [Fact]
    public void GetOrderHistory_SqlInjectionPattern_DoesNotLeakOtherUserOrders()
    {
        // Scenario: attacker passes a SQL-injection-like user id; parameterized query prevents data leakage.
        var userA = Guid.NewGuid().ToString("D");
        var userB = Guid.NewGuid().ToString("D");
        var orderA = InsertTestOrder(userA, "Pending");
        var orderB = InsertTestOrder(userB, "Pending");

        var maliciousUserId = $"' OR 1=1 --";
        var history = _service.GetOrderHistory(maliciousUserId);

        Assert.DoesNotContain(history, o => o.OrderId == orderA);
        Assert.DoesNotContain(history, o => o.OrderId == orderB);
    }

    [Fact]
    public void GetOrderDetail_WrongUserId_ThrowsKeyNotFoundException()
    {
        // Scenario: User A's order must not be readable by User B (IDOR).
        var userA = Guid.NewGuid().ToString("D");
        var userB = Guid.NewGuid().ToString("D");
        var orderA = InsertTestOrder(userA, "Pending");

        Assert.Throws<KeyNotFoundException>(() => _service.GetOrderDetail(orderA, userB));
    }

    [Fact]
    public void GetOrderDetail_NegativeOrderId_ThrowsKeyNotFoundException()
    {
        // Scenario: negative order id must be treated as non-existent.
        var userId = Guid.NewGuid().ToString("D");

        Assert.Throws<KeyNotFoundException>(() => _service.GetOrderDetail(-1, userId));
    }

    [Fact]
    public void GetOrderDetail_OrderWithSqlInjectionUserId_DoesNotLeak()
    {
        // Scenario: even if an attacker controls the user id parameter, the order id still scopes the query.
        var userId = Guid.NewGuid().ToString("D");
        var orderId = InsertTestOrder(userId, "Pending");

        var maliciousUserId = "' OR 1=1 --";
        Assert.Throws<KeyNotFoundException>(() => _service.GetOrderDetail(orderId, maliciousUserId));
    }

    public void Dispose()
    {
        foreach (var orderId in _createdOrderIds)
        {
            DeleteTestOrder(orderId);
        }
    }
}
