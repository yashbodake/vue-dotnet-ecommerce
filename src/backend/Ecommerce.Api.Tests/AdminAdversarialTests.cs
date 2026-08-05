using System.Data;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Data;
using Ecommerce.Api.Services;
using Microsoft.Data.SqlClient;

namespace Ecommerce.Api.Tests;

/// <summary>
/// Adversarial tests for AdminService: invalid state transitions, boundary values,
/// oversized input, and IDOR-style product/order mutations.
/// </summary>
public class AdminAdversarialTests : IDisposable
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly AdminService _service;
    private readonly List<int> _createdProductIds = [];
    private readonly List<int> _createdOrderIds = [];

    public AdminAdversarialTests()
    {
        var connectionString = "Server=.\\SQLEXPRESS;Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True";
        _connectionFactory = new SqlConnectionFactory(connectionString);
        _service = new AdminService(_connectionFactory);
    }

    #region Helpers

    private int GetFirstRealCategoryId()
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "SELECT TOP 1 CategoryId FROM dbo.Category",
            connection);
        var result = command.ExecuteScalar();
        Assert.NotNull(result);
        return (int)result;
    }

    private int GetFirstRealProductId()
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "SELECT TOP 1 ProductId FROM dbo.Product",
            connection);
        var result = command.ExecuteScalar();
        Assert.NotNull(result);
        return (int)result;
    }

    private void DeleteTestProduct(int productId)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "DELETE FROM dbo.Product WHERE ProductId = @ProductId",
            connection);
        command.Parameters.AddWithValue("@ProductId", productId);
        command.ExecuteNonQuery();
    }

    private void RestoreProductActive(int productId)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "UPDATE dbo.Product SET IsActive = 1 WHERE ProductId = @ProductId",
            connection);
        command.Parameters.AddWithValue("@ProductId", productId);
        command.ExecuteNonQuery();
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
        command.Parameters.AddWithValue("@ShippingAddress", "123 Admin Test St (Standard)");
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

    #endregion

    #region Product boundary tests

    [Fact]
    public void CreateProduct_NegativePrice_ThrowsArgumentException()
    {
        // Scenario: negative price must be rejected before reaching the database.
        var request = new CreateProductRequest
        {
            CategoryId = GetFirstRealCategoryId(),
            Name = $"Negative Price {Guid.NewGuid():N}",
            Description = "Test",
            Price = -1.00m,
            Stock = 10,
            IsActive = true
        };

        Assert.Throws<ArgumentException>(() => _service.CreateProduct(request));
    }

    [Fact]
    public void CreateProduct_NegativeStock_ThrowsArgumentException()
    {
        // Scenario: negative stock must be rejected.
        var request = new CreateProductRequest
        {
            CategoryId = GetFirstRealCategoryId(),
            Name = $"Negative Stock {Guid.NewGuid():N}",
            Description = "Test",
            Price = 10.00m,
            Stock = -5,
            IsActive = true
        };

        Assert.Throws<ArgumentException>(() => _service.CreateProduct(request));
    }

    [Fact]
    public void CreateProduct_ZeroPrice_ThrowsArgumentException()
    {
        // Scenario: zero-price product is likely unintentional and should be rejected.
        var request = new CreateProductRequest
        {
            CategoryId = GetFirstRealCategoryId(),
            Name = $"Zero Price {Guid.NewGuid():N}",
            Description = "Test",
            Price = 0m,
            Stock = 10,
            IsActive = true
        };

        Assert.Throws<ArgumentException>(() => _service.CreateProduct(request));
    }

    [Fact]
    public void CreateProduct_OversizedName_DoesNotCrash()
    {
        // Scenario: attacker submits a product name larger than typical DB columns.
        var request = new CreateProductRequest
        {
            CategoryId = GetFirstRealCategoryId(),
            Name = new string('A', 5000),
            Description = "Test",
            Price = 10.00m,
            Stock = 10,
            IsActive = true
        };

        try
        {
            var created = _service.CreateProduct(request);
            _createdProductIds.Add(created.ProductId);
            Assert.False(string.IsNullOrEmpty(created.Name));
        }
        catch (SqlException ex) when (ex.Number is 8152 or 2628 or 2714 or 103)
        {
            // String/parameter truncation errors are acceptable defensive behaviour.
        }
        catch (Exception ex)
        {
            Assert.True(ex is ArgumentException or InvalidOperationException,
                $"Unexpected exception type: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [Fact]
    public void UpdateProduct_NonExistentCategory_ThrowsArgumentException()
    {
        // Scenario: update a product to a category that does not exist.
        var createRequest = new CreateProductRequest
        {
            CategoryId = GetFirstRealCategoryId(),
            Name = $"Update Category Test {Guid.NewGuid():N}",
            Description = "Test",
            Price = 10.00m,
            Stock = 10,
            IsActive = true
        };

        var created = _service.CreateProduct(createRequest);
        _createdProductIds.Add(created.ProductId);

        var updateRequest = new UpdateProductRequest
        {
            ProductId = created.ProductId,
            CategoryId = 999999,
            Name = created.Name,
            Description = "Updated",
            Price = 12.00m,
            Stock = 10,
            IsActive = true
        };

        Assert.Throws<ArgumentException>(() => _service.UpdateProduct(created.ProductId, updateRequest));
    }

    [Fact]
    public void SoftDeleteProduct_AlreadyDeletedProduct_ThrowsKeyNotFoundException()
    {
        // Scenario: soft-deleting the same product twice should not crash but return not found.
        var createRequest = new CreateProductRequest
        {
            CategoryId = GetFirstRealCategoryId(),
            Name = $"Double Delete {Guid.NewGuid():N}",
            Description = "Test",
            Price = 10.00m,
            Stock = 10,
            IsActive = true
        };

        var created = _service.CreateProduct(createRequest);
        _createdProductIds.Add(created.ProductId);

        _service.SoftDeleteProduct(created.ProductId);

        Assert.Throws<KeyNotFoundException>(() => _service.SoftDeleteProduct(created.ProductId));
    }

    #endregion

    #region Order status tests

    [Fact]
    public void UpdateOrderStatus_EmptyStatus_ThrowsArgumentException()
    {
        // Scenario: empty or null status should be rejected.
        var userId = Guid.NewGuid().ToString("D");
        var orderId = InsertTestOrder(userId, "Pending");

        Assert.Throws<ArgumentException>(() => _service.UpdateOrderStatus(orderId, ""));
    }

    [Fact]
    public void UpdateOrderStatus_InvalidStatus_ThrowsArgumentException()
    {
        // Scenario: status not in the allowed set must be rejected.
        var userId = Guid.NewGuid().ToString("D");
        var orderId = InsertTestOrder(userId, "Pending");

        Assert.Throws<ArgumentException>(() => _service.UpdateOrderStatus(orderId, "Hacked"));
    }

    [Fact]
    public void GetProductById_NegativeId_ReturnsNull()
    {
        // Scenario: negative product id must return null cleanly.
        var product = _service.GetProductById(-1);
        Assert.Null(product);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        foreach (var productId in _createdProductIds)
        {
            RestoreProductActive(productId);
            DeleteTestProduct(productId);
        }

        foreach (var orderId in _createdOrderIds)
        {
            DeleteTestOrder(orderId);
        }
    }

    #endregion
}
