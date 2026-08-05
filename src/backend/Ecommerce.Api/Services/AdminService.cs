using System.Data;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Data;
using Microsoft.Data.SqlClient;

namespace Ecommerce.Api.Services;

/// <summary>
/// Admin service using native parameterized SQL. Provides product CRUD (soft-delete),
/// category lookup for product forms, and order status management.
/// </summary>
public sealed class AdminService
{
    private readonly ISqlConnectionFactory _connectionFactory;

    private static readonly HashSet<string> ValidOrderStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending", "Processing", "Shipped", "Delivered", "Cancelled"
    };

    public AdminService(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// List all products including inactive ones, joined with category name.
    /// </summary>
    public List<AdminProductDto> GetAllProducts()
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand("""
            SELECT
                p.ProductId,
                p.CategoryId,
                c.Name AS CategoryName,
                p.Name,
                p.Description,
                p.Price,
                p.ThumbnailUrl,
                p.Stock,
                p.IsActive
            FROM dbo.Product p
            INNER JOIN dbo.Category c ON p.CategoryId = c.CategoryId
            ORDER BY p.Name
            """, connection);

        var products = new List<AdminProductDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            products.Add(MapAdminProduct(reader));
        }

        return products;
    }

    /// <summary>
    /// Get a single product by id including inactive products.
    /// </summary>
    public AdminProductDto? GetProductById(int productId)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand("""
            SELECT
                p.ProductId,
                p.CategoryId,
                c.Name AS CategoryName,
                p.Name,
                p.Description,
                p.Price,
                p.ThumbnailUrl,
                p.Stock,
                p.IsActive
            FROM dbo.Product p
            INNER JOIN dbo.Category c ON p.CategoryId = c.CategoryId
            WHERE p.ProductId = @ProductId
            """, connection);

        command.Parameters.AddWithValue("@ProductId", productId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return MapAdminProduct(reader);
    }

    /// <summary>
    /// Create a new product. Validates name, price, and category existence.
    /// </summary>
    public AdminProductDto CreateProduct(CreateProductRequest request)
    {
        ValidateProductRequest(request.Name, request.Price, request.CategoryId, request.Stock);

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            EnsureCategoryExists(connection, transaction, request.CategoryId);

            int productId;
            using (var command = new SqlCommand(
                """
                INSERT INTO dbo.Product (CategoryId, Name, Description, Price, ThumbnailUrl, Stock, IsActive, CreatedDate)
                OUTPUT INSERTED.ProductId
                VALUES (@CategoryId, @Name, @Description, @Price, @ThumbnailUrl, @Stock, @IsActive, @CreatedDate)
                """,
                connection, transaction))
            {
                command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
                command.Parameters.AddWithValue("@Name", request.Name);
                command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
                command.Parameters.AddWithValue("@Price", request.Price);
                command.Parameters.AddWithValue("@ThumbnailUrl", (object?)request.ThumbnailUrl ?? DBNull.Value);
                command.Parameters.AddWithValue("@Stock", request.Stock);
                command.Parameters.AddWithValue("@IsActive", request.IsActive);
                command.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                productId = (int)command.ExecuteScalar()!;
            }

            transaction.Commit();

            // Re-read to return consistent data including category name.
            var product = GetProductById(productId);
            if (product == null)
            {
                throw new InvalidOperationException("Product was created but could not be reloaded.");
            }

            return product;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Update an existing product. Throws KeyNotFoundException if the product does not exist.
    /// </summary>
    public AdminProductDto UpdateProduct(int productId, UpdateProductRequest request)
    {
        ValidateProductRequest(request.Name, request.Price, request.CategoryId, request.Stock);

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            EnsureCategoryExists(connection, transaction, request.CategoryId);

            using (var command = new SqlCommand(
                """
                UPDATE dbo.Product
                SET CategoryId = @CategoryId,
                    Name = @Name,
                    Description = @Description,
                    Price = @Price,
                    ThumbnailUrl = @ThumbnailUrl,
                    Stock = @Stock,
                    IsActive = @IsActive
                WHERE ProductId = @ProductId
                """,
                connection, transaction))
            {
                command.Parameters.AddWithValue("@ProductId", productId);
                command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
                command.Parameters.AddWithValue("@Name", request.Name);
                command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
                command.Parameters.AddWithValue("@Price", request.Price);
                command.Parameters.AddWithValue("@ThumbnailUrl", (object?)request.ThumbnailUrl ?? DBNull.Value);
                command.Parameters.AddWithValue("@Stock", request.Stock);
                command.Parameters.AddWithValue("@IsActive", request.IsActive);

                var rows = command.ExecuteNonQuery();
                if (rows == 0)
                {
                    throw new KeyNotFoundException("Product not found.");
                }
            }

            transaction.Commit();

            var product = GetProductById(productId);
            if (product == null)
            {
                throw new InvalidOperationException("Product was updated but could not be reloaded.");
            }

            return product;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Soft-delete a product by setting IsActive = 0.
    /// </summary>
    public void SoftDeleteProduct(int productId)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();

        // Idempotent-correct semantics: only an active product may be soft-deleted.
        // If the product does not exist or is already inactive, treat it as not found.
        using (var checkCommand = new SqlCommand(
            "SELECT IsActive FROM dbo.Product WHERE ProductId = @ProductId",
            connection))
        {
            checkCommand.Parameters.AddWithValue("@ProductId", productId);

            using var reader = checkCommand.ExecuteReader();
            if (!reader.Read() || reader.GetBoolean(0) == false)
            {
                throw new KeyNotFoundException("Product not found.");
            }
        }

        using var command = new SqlCommand(
            "UPDATE dbo.Product SET IsActive = 0 WHERE ProductId = @ProductId AND IsActive = 1",
            connection);

        command.Parameters.AddWithValue("@ProductId", productId);

        var rows = command.ExecuteNonQuery();
        if (rows == 0)
        {
            throw new KeyNotFoundException("Product not found.");
        }
    }

    /// <summary>
    /// List all categories ordered by name. Used for product form dropdown.
    /// </summary>
    public List<CategoryDto> GetAllCategories()
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand("""
            SELECT CategoryId, Name, ParentCategoryId, DisplayOrder
            FROM dbo.Category
            ORDER BY Name
            """, connection);

        var categories = new List<CategoryDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            categories.Add(new CategoryDto
            {
                CategoryId = reader.GetInt32(0),
                Name = reader.GetString(1),
                ParentCategoryId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                DisplayOrder = reader.GetInt32(3)
            });
        }

        return categories;
    }

    /// <summary>
    /// List all orders. Optional status filter. Ordered by OrderDate DESC.
    /// </summary>
    public List<AdminOrderDto> GetAllOrders(string? statusFilter = null)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();

        var sql = """
            SELECT
                o.OrderId,
                o.UserId,
                u.Email AS UserEmail,
                o.OrderDate,
                o.Status,
                o.ShippingAddress,
                o.TotalAmount,
                COALESCE(oi.ItemCount, 0) AS ItemCount
            FROM dbo.Orders o
            LEFT JOIN dbo.AspNetUsers u ON o.UserId = u.Id
            LEFT JOIN (
                SELECT OrderId, COUNT(*) AS ItemCount
                FROM dbo.OrderItem
                GROUP BY OrderId
            ) oi ON oi.OrderId = o.OrderId
            """;

        using var command = new SqlCommand();
        command.Connection = connection;

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            sql += " WHERE o.Status = @Status";
            command.Parameters.AddWithValue("@Status", statusFilter);
        }

        sql += " ORDER BY o.OrderDate DESC";

        command.CommandText = sql;

        var orders = new List<AdminOrderDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            orders.Add(MapAdminOrder(reader));
        }

        return orders;
    }

    /// <summary>
    /// Update the status of an order. Validates status value.
    /// </summary>
    public AdminOrderDto UpdateOrderStatus(int orderId, string status)
    {
        if (string.IsNullOrWhiteSpace(status) || !ValidOrderStatuses.Contains(status))
        {
            throw new ArgumentException("Invalid order status.");
        }

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            using (var command = new SqlCommand(
                "UPDATE dbo.Orders SET Status = @Status WHERE OrderId = @OrderId",
                connection, transaction))
            {
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@OrderId", orderId);

                var rows = command.ExecuteNonQuery();
                if (rows == 0)
                {
                    throw new KeyNotFoundException("Order not found.");
                }
            }

            using (var readCommand = new SqlCommand("""
                SELECT TOP 1
                    o.OrderId,
                    o.UserId,
                    u.Email AS UserEmail,
                    o.OrderDate,
                    o.Status,
                    o.ShippingAddress,
                    o.TotalAmount,
                    COALESCE(oi.ItemCount, 0) AS ItemCount
                FROM dbo.Orders o
                LEFT JOIN dbo.AspNetUsers u ON o.UserId = u.Id
                LEFT JOIN (
                    SELECT OrderId, COUNT(*) AS ItemCount
                    FROM dbo.OrderItem
                    GROUP BY OrderId
                ) oi ON oi.OrderId = o.OrderId
                WHERE o.OrderId = @OrderId
                """, connection, transaction))
            {
                readCommand.Parameters.AddWithValue("@OrderId", orderId);

                using var reader = readCommand.ExecuteReader();
                if (!reader.Read())
                {
                    throw new InvalidOperationException("Order was updated but could not be reloaded.");
                }

                var order = MapAdminOrder(reader);

                reader.Close();
                transaction.Commit();
                return order;
            }
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Get a single order by id for admin view.
    /// </summary>
    private AdminOrderDto? GetOrderById(int orderId)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand("""
            SELECT TOP 1
                o.OrderId,
                o.UserId,
                u.Email AS UserEmail,
                o.OrderDate,
                o.Status,
                o.ShippingAddress,
                o.TotalAmount,
                (SELECT COUNT(*) FROM dbo.OrderItem oi WHERE oi.OrderId = o.OrderId) AS ItemCount
            FROM dbo.Orders o
            LEFT JOIN dbo.AspNetUsers u ON o.UserId = u.Id
            WHERE o.OrderId = @OrderId
            """, connection);

        command.Parameters.AddWithValue("@OrderId", orderId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return MapAdminOrder(reader);
    }

    private static void ValidateProductRequest(string name, decimal price, int categoryId, int stock)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.");
        }

        if (price <= 0)
        {
            throw new ArgumentException("Price must be greater than zero.");
        }

        if (stock < 0)
        {
            throw new ArgumentException("Stock cannot be negative.");
        }

        if (categoryId <= 0)
        {
            throw new ArgumentException("A valid category is required.");
        }
    }

    private static void EnsureCategoryExists(SqlConnection connection, SqlTransaction transaction, int categoryId)
    {
        using var command = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.Category WHERE CategoryId = @CategoryId",
            connection, transaction);

        command.Parameters.AddWithValue("@CategoryId", categoryId);

        var count = (int)command.ExecuteScalar()!;
        if (count == 0)
        {
            throw new ArgumentException("Category does not exist.");
        }
    }

    private static AdminProductDto MapAdminProduct(IDataReader reader)
    {
        return new AdminProductDto
        {
            ProductId = reader.GetInt32(0),
            CategoryId = reader.GetInt32(1),
            CategoryName = reader.GetString(2),
            Name = reader.GetString(3),
            Description = reader.IsDBNull(4) ? null : reader.GetString(4),
            Price = reader.GetDecimal(5),
            ThumbnailUrl = reader.IsDBNull(6) ? null : reader.GetString(6),
            Stock = reader.GetInt32(7),
            IsActive = reader.GetBoolean(8)
        };
    }

    private static AdminOrderDto MapAdminOrder(IDataReader reader)
    {
        return new AdminOrderDto
        {
            OrderId = reader.GetInt32(0),
            UserId = reader.GetString(1),
            UserEmail = reader.IsDBNull(2) ? null : reader.GetString(2),
            OrderDate = reader.GetDateTime(3),
            Status = reader.GetString(4),
            ShippingAddress = reader.GetString(5),
            TotalAmount = reader.GetDecimal(6),
            ItemCount = reader.GetInt32(7)
        };
    }
}
