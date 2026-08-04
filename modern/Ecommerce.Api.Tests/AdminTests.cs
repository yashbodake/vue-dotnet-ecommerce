using System.Data;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Data;
using Ecommerce.Api.Services;
using Microsoft.Data.SqlClient;

namespace Ecommerce.Api.Tests;

/// <summary>
/// Integration tests for the AdminService using a real SQL Express database.
/// Requires: .\SQLEXPRESS with LegacyEcommerceDb seeded.
/// </summary>
public class AdminTests : IDisposable
{
    private readonly string _connectionString = "Server=.\\SQLEXPRESS;Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True";
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly List<int> _createdProductIds = [];
    private readonly List<int> _createdOrderIds = [];

    public AdminTests()
    {
        _connectionFactory = new SqlConnectionFactory(_connectionString);
    }

    #region Helpers

    private AdminService CreateAdminService() => new(_connectionFactory);

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

    #endregion

    #region Product Tests

    [Fact]
    public void GetAllProducts_ReturnsNonEmptyList()
    {
        var service = CreateAdminService();
        var products = service.GetAllProducts();

        Assert.NotEmpty(products);
        Assert.All(products, p =>
        {
            Assert.True(p.ProductId > 0);
            Assert.False(string.IsNullOrWhiteSpace(p.Name));
            Assert.False(string.IsNullOrWhiteSpace(p.CategoryName));
        });
    }

    [Fact]
    public void GetProductById_ReturnsProduct_WhenExists()
    {
        var service = CreateAdminService();
        var productId = GetFirstRealProductId();

        var product = service.GetProductById(productId);

        Assert.NotNull(product);
        Assert.Equal(productId, product.ProductId);
        Assert.False(string.IsNullOrWhiteSpace(product.Name));
        Assert.False(string.IsNullOrWhiteSpace(product.CategoryName));
    }

    [Fact]
    public void GetProductById_ReturnsNull_WhenNotExists()
    {
        var service = CreateAdminService();
        var product = service.GetProductById(999999);

        Assert.Null(product);
    }

    [Fact]
    public void CreateProduct_CreatesNewProduct()
    {
        var service = CreateAdminService();
        var categoryId = GetFirstRealCategoryId();
        var uniqueName = $"Test Product {Guid.NewGuid():N}";

        var request = new CreateProductRequest
        {
            CategoryId = categoryId,
            Name = uniqueName,
            Description = "Test description",
            Price = 12.34m,
            ThumbnailUrl = "https://example.com/test.png",
            Stock = 100,
            IsActive = true
        };

        var created = service.CreateProduct(request);
        _createdProductIds.Add(created.ProductId);

        Assert.True(created.ProductId > 0);
        Assert.Equal(categoryId, created.CategoryId);
        Assert.Equal(uniqueName, created.Name);
        Assert.Equal("Test description", created.Description);
        Assert.Equal(12.34m, created.Price);
        Assert.Equal("https://example.com/test.png", created.ThumbnailUrl);
        Assert.Equal(100, created.Stock);
        Assert.True(created.IsActive);

        var reloaded = service.GetProductById(created.ProductId);
        Assert.NotNull(reloaded);
        Assert.Equal(uniqueName, reloaded.Name);
    }

    [Fact]
    public void CreateProduct_ThrowsArgumentException_WhenNameEmpty()
    {
        var service = CreateAdminService();
        var categoryId = GetFirstRealCategoryId();

        var request = new CreateProductRequest
        {
            CategoryId = categoryId,
            Name = "",
            Price = 10.00m,
            Stock = 10
        };

        Assert.Throws<ArgumentException>(() => service.CreateProduct(request));
    }

    [Fact]
    public void CreateProduct_ThrowsArgumentException_WhenCategoryNotFound()
    {
        var service = CreateAdminService();

        var request = new CreateProductRequest
        {
            CategoryId = 999999,
            Name = $"Test Product {Guid.NewGuid():N}",
            Price = 10.00m,
            Stock = 10
        };

        Assert.Throws<ArgumentException>(() => service.CreateProduct(request));
    }

    [Fact]
    public void UpdateProduct_UpdatesFields()
    {
        var service = CreateAdminService();
        var categoryId = GetFirstRealCategoryId();
        var originalName = $"Test Product {Guid.NewGuid():N}";

        var createRequest = new CreateProductRequest
        {
            CategoryId = categoryId,
            Name = originalName,
            Description = "Original description",
            Price = 10.00m,
            Stock = 50,
            IsActive = true
        };

        var created = service.CreateProduct(createRequest);
        _createdProductIds.Add(created.ProductId);

        var updatedName = $"Updated Product {Guid.NewGuid():N}";
        var updateRequest = new UpdateProductRequest
        {
            ProductId = created.ProductId,
            CategoryId = categoryId,
            Name = updatedName,
            Description = "Updated description",
            Price = 24.99m,
            ThumbnailUrl = "https://example.com/updated.png",
            Stock = 75,
            IsActive = true
        };

        var updated = service.UpdateProduct(created.ProductId, updateRequest);

        Assert.Equal(created.ProductId, updated.ProductId);
        Assert.Equal(updatedName, updated.Name);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal(24.99m, updated.Price);
        Assert.Equal("https://example.com/updated.png", updated.ThumbnailUrl);
        Assert.Equal(75, updated.Stock);

        var reloaded = service.GetProductById(created.ProductId);
        Assert.NotNull(reloaded);
        Assert.Equal(updatedName, reloaded.Name);
        Assert.Equal(24.99m, reloaded.Price);
        Assert.Equal(75, reloaded.Stock);
    }

    [Fact]
    public void UpdateProduct_ThrowsKeyNotFoundException_WhenNotExists()
    {
        var service = CreateAdminService();
        var categoryId = GetFirstRealCategoryId();

        var request = new UpdateProductRequest
        {
            ProductId = 999999,
            CategoryId = categoryId,
            Name = "Missing Product",
            Price = 10.00m,
            Stock = 10,
            IsActive = true
        };

        Assert.Throws<KeyNotFoundException>(() => service.UpdateProduct(999999, request));
    }

    [Fact]
    public void SoftDeleteProduct_SetsIsActiveFalse()
    {
        var service = CreateAdminService();
        var categoryId = GetFirstRealCategoryId();
        var uniqueName = $"Test Product {Guid.NewGuid():N}";

        var createRequest = new CreateProductRequest
        {
            CategoryId = categoryId,
            Name = uniqueName,
            Price = 10.00m,
            Stock = 10,
            IsActive = true
        };

        var created = service.CreateProduct(createRequest);
        _createdProductIds.Add(created.ProductId);

        service.SoftDeleteProduct(created.ProductId);

        var product = service.GetProductById(created.ProductId);
        Assert.NotNull(product);
        Assert.False(product.IsActive);

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "SELECT IsActive FROM dbo.Product WHERE ProductId = @ProductId",
            connection);
        command.Parameters.AddWithValue("@ProductId", created.ProductId);
        var isActive = (bool)command.ExecuteScalar()!;
        Assert.False(isActive);

        RestoreProductActive(created.ProductId);
    }

    [Fact]
    public void SoftDeleteProduct_ThrowsKeyNotFoundException_WhenNotExists()
    {
        var service = CreateAdminService();
        Assert.Throws<KeyNotFoundException>(() => service.SoftDeleteProduct(999999));
    }

    #endregion

    #region Category Tests

    [Fact]
    public void GetAllCategories_ReturnsNonEmptyList()
    {
        var service = CreateAdminService();
        var categories = service.GetAllCategories();

        Assert.NotEmpty(categories);
        Assert.All(categories, c =>
        {
            Assert.True(c.CategoryId > 0);
            Assert.False(string.IsNullOrWhiteSpace(c.Name));
        });
    }

    #endregion

    #region Order Tests

    [Fact]
    public void GetAllOrders_ReturnsOrders()
    {
        var service = CreateAdminService();
        var orders = service.GetAllOrders();

        Assert.NotNull(orders);
    }

    [Fact]
    public void GetAllOrders_WithStatusFilter()
    {
        var service = CreateAdminService();
        var userId = Guid.NewGuid().ToString("D");
        var orderId = InsertTestOrder(userId, "Pending");

        var orders = service.GetAllOrders("Pending");

        Assert.Contains(orders, o => o.OrderId == orderId && o.Status == "Pending");
    }

    [Fact]
    public void UpdateOrderStatus_UpdatesStatus()
    {
        var service = CreateAdminService();
        var userId = Guid.NewGuid().ToString("D");
        var orderId = InsertTestOrder(userId, "Pending");

        var updated = service.UpdateOrderStatus(orderId, "Shipped");

        Assert.Equal(orderId, updated.OrderId);
        Assert.Equal("Shipped", updated.Status);

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "SELECT Status FROM dbo.Orders WHERE OrderId = @OrderId",
            connection);
        command.Parameters.AddWithValue("@OrderId", orderId);
        var status = (string)command.ExecuteScalar()!;
        Assert.Equal("Shipped", status);
    }

    [Fact]
    public void UpdateOrderStatus_ThrowsArgumentException_WhenInvalidStatus()
    {
        var service = CreateAdminService();
        Assert.Throws<ArgumentException>(() => service.UpdateOrderStatus(999999, "InvalidStatus"));
    }

    [Fact]
    public void UpdateOrderStatus_ThrowsKeyNotFoundException_WhenOrderNotExists()
    {
        var service = CreateAdminService();
        Assert.Throws<KeyNotFoundException>(() => service.UpdateOrderStatus(999999, "Shipped"));
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
