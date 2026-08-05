using System.Data;
using System.Text.RegularExpressions;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Data;
using Microsoft.Data.SqlClient;

namespace Ecommerce.Api.Services;

/// <summary>
/// Checkout service using native parameterized SQL. Creates orders, decrements stock,
/// clears the authenticated cart, and returns order confirmations. Card data is validated
/// for the demo but is never persisted.
/// </summary>
public sealed partial class CheckoutService
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public CheckoutService(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Returns the fixed list of supported shipping options.
    /// </summary>
    public IReadOnlyList<ShippingOption> GetShippingOptions()
    {
        return new List<ShippingOption>
        {
            new()
            {
                Code = "Standard",
                Name = "Standard Shipping",
                Description = "3-5 business days",
                EstimatedDays = "3-5"
            },
            new()
            {
                Code = "Express",
                Name = "Express Shipping",
                Description = "1-2 business days",
                EstimatedDays = "1-2"
            }
        };
    }

    /// <summary>
    /// Places an order for the authenticated user. Validates shipping, card demo fields,
    /// cart contents and stock; creates the order and items; decrements stock; clears the
    /// user's cart. All writes happen inside a single SQL transaction.
    /// </summary>
    public OrderConfirmationDto PlaceOrder(string userId, PlaceOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(request.ShippingMethod) ||
            !request.ShippingMethod.Equals("Standard", StringComparison.OrdinalIgnoreCase) &&
            !request.ShippingMethod.Equals("Express", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid shipping method. Use Standard or Express.");
        }

        ValidateCardFields(request);

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Load the authenticated user's cart with product/variant details.
            var cartLines = LoadCartLines(connection, transaction, userId);
            var validCartLines = cartLines.Where(l => l.Quantity > 0).ToList();
            if (validCartLines.Count == 0)
            {
                throw new InvalidOperationException("Your cart is empty.");
            }

            // Batch load product and variant details (with row locks) for all cart lines.
            var productLookup = LoadProductStockInfo(connection, transaction, validCartLines);
            var variantLookup = LoadVariantStockInfo(connection, transaction, validCartLines);

            // Re-validate each product line: active + sufficient stock, then decrement.
            decimal orderTotal = 0;
            var validatedLines = new List<CartLine>();
            foreach (var line in validCartLines)
            {
                var (unitPrice, productName, variantName) = ValidateAndDecrementStock(
                    connection, transaction, line, productLookup, variantLookup);

                var lineTotal = unitPrice * line.Quantity;
                orderTotal += lineTotal;

                validatedLines.Add(line with
                {
                    UnitPrice = unitPrice,
                    ProductName = productName,
                    VariantName = variantName
                });
            }

            // Insert the order row.
            var shippingMethod = request.ShippingMethod;
            var shippingAddress = request.ShippingAddress;
            int orderId;
            using (var orderCommand = new SqlCommand(
                "INSERT INTO dbo.Orders (UserId, OrderDate, Status, ShippingAddress, TotalAmount) " +
                "OUTPUT INSERTED.OrderId " +
                "VALUES (@UserId, @OrderDate, @Status, @ShippingAddress, @TotalAmount)",
                connection, transaction))
            {
                orderCommand.Parameters.AddWithValue("@UserId", userId);
                orderCommand.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                orderCommand.Parameters.AddWithValue("@Status", "Pending");
                orderCommand.Parameters.AddWithValue("@ShippingAddress", shippingAddress);
                orderCommand.Parameters.AddWithValue("@TotalAmount", orderTotal);

                orderId = (int)orderCommand.ExecuteScalar()!;
            }

            // Insert order items.
            foreach (var line in validatedLines)
            {
                using var itemCommand = new SqlCommand(
                    "INSERT INTO dbo.OrderItem (OrderId, ProductId, ProductVariantId, Quantity, UnitPrice) " +
                    "VALUES (@OrderId, @ProductId, @ProductVariantId, @Quantity, @UnitPrice)",
                    connection, transaction);

                itemCommand.Parameters.AddWithValue("@OrderId", orderId);
                itemCommand.Parameters.AddWithValue("@ProductId", line.ProductId);
                itemCommand.Parameters.AddWithValue("@ProductVariantId", line.ProductVariantId ?? (object)DBNull.Value);
                itemCommand.Parameters.AddWithValue("@Quantity", line.Quantity);
                itemCommand.Parameters.AddWithValue("@UnitPrice", line.UnitPrice);
                itemCommand.ExecuteNonQuery();
            }

            // Clear the authenticated user's cart.
            using (var clearCommand = new SqlCommand(
                "DELETE FROM dbo.CartItem WHERE UserId = @UserId",
                connection, transaction))
            {
                clearCommand.Parameters.AddWithValue("@UserId", userId);
                clearCommand.ExecuteNonQuery();
            }

            transaction.Commit();

            return BuildOrderConfirmation(orderId, DateTime.Now, "Pending", shippingAddress, shippingMethod, orderTotal, validatedLines);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Loads an order by id for the given user. Returns 404 semantics via
    /// KeyNotFoundException when the order does not exist or is not owned by the user.
    /// </summary>
    public OrderConfirmationDto GetOrderDetail(int orderId, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();

        string status;
        string shippingAddress;
        decimal totalAmount;
        DateTime orderDate;
        string shippingMethod = string.Empty;

        using (var orderCommand = new SqlCommand(
            "SELECT OrderDate, Status, ShippingAddress, TotalAmount " +
            "FROM dbo.Orders " +
            "WHERE OrderId = @OrderId AND UserId = @UserId",
            connection))
        {
            orderCommand.Parameters.AddWithValue("@OrderId", orderId);
            orderCommand.Parameters.AddWithValue("@UserId", userId);

            using var reader = orderCommand.ExecuteReader();
            if (!reader.Read())
            {
                throw new KeyNotFoundException("Order not found.");
            }

            orderDate = reader.GetDateTime(0);
            status = reader.GetString(1);
            shippingAddress = reader.GetString(2);
            totalAmount = reader.GetDecimal(3);
        }

        // The modern schema stores shipping method inside the formatted address string
        // (legacy parity). Extract it when present at the end in parentheses.
        var match = ShippingMethodRegex().Match(shippingAddress);
        if (match.Success)
        {
            shippingMethod = match.Groups[1].Value;
        }

        var lines = new List<CartLine>();
        using (var itemCommand = new SqlCommand(
            "SELECT oi.ProductId, p.Name AS ProductName, " +
            "pv.Name AS VariantName, oi.Quantity, oi.UnitPrice " +
            "FROM dbo.OrderItem oi " +
            "INNER JOIN dbo.Product p ON oi.ProductId = p.ProductId " +
            "LEFT JOIN dbo.ProductVariant pv ON oi.ProductVariantId = pv.ProductVariantId " +
            "WHERE oi.OrderId = @OrderId " +
            "ORDER BY oi.OrderItemId",
            connection))
        {
            itemCommand.Parameters.AddWithValue("@OrderId", orderId);

            using var reader = itemCommand.ExecuteReader();
            while (reader.Read())
            {
                lines.Add(new CartLine
                {
                    ProductId = reader.GetInt32(0),
                    ProductName = reader.GetString(1),
                    VariantName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Quantity = reader.GetInt32(3),
                    UnitPrice = reader.GetDecimal(4)
                });
            }
        }

        return BuildOrderConfirmation(orderId, orderDate, status, shippingAddress, shippingMethod, totalAmount, lines);
    }

    private static void ValidateCardFields(PlaceOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CardName))
        {
            throw new ArgumentException("Card name is required.");
        }

        var cardDigits = new string(request.CardNumber?.Where(char.IsDigit).ToArray() ?? []);
        if (cardDigits.Length < 12)
        {
            throw new ArgumentException("Invalid card number.");
        }

        if (string.IsNullOrWhiteSpace(request.CardExpiry) ||
            !ExpiryRegex().IsMatch(request.CardExpiry.Trim()))
        {
            throw new ArgumentException("Invalid card expiry.");
        }

        var cvv = request.CardCvv?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cvv) || cvv.Length < 3)
        {
            throw new ArgumentException("Invalid card CVV.");
        }
    }

    private List<CartLine> LoadCartLines(SqlConnection connection, SqlTransaction transaction, string userId)
    {
        var lines = new List<CartLine>();
        using var command = new SqlCommand(
            "SELECT ci.CartItemId, ci.ProductId, ci.ProductVariantId, ci.Quantity, " +
            "p.Price AS ProductPrice, COALESCE(pv.PriceAdjustment, 0) AS PriceAdjustment " +
            "FROM dbo.CartItem ci " +
            "INNER JOIN dbo.Product p ON ci.ProductId = p.ProductId " +
            "LEFT JOIN dbo.ProductVariant pv ON ci.ProductVariantId = pv.ProductVariantId " +
            "WHERE ci.UserId = @UserId " +
            "ORDER BY ci.AddedDate",
            connection, transaction);

        command.Parameters.AddWithValue("@UserId", userId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lines.Add(new CartLine
            {
                CartItemId = reader.GetInt32(0),
                ProductId = reader.GetInt32(1),
                ProductVariantId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                Quantity = reader.GetInt32(3),
                UnitPrice = reader.GetDecimal(4) + reader.GetDecimal(5)
            });
        }

        return lines;
    }

    private static Dictionary<int, ProductStockInfo> LoadProductStockInfo(
        SqlConnection connection,
        SqlTransaction transaction,
        List<CartLine> lines)
    {
        var lookup = new Dictionary<int, ProductStockInfo>();
        var productIds = lines
            .Select(l => l.ProductId)
            .Distinct()
            .ToList();

        if (productIds.Count == 0)
        {
            return lookup;
        }

        var parameterNames = string.Join(", ", productIds.Select((_, i) => $"@ProductId{i}"));
        using var command = new SqlCommand(
            "SELECT ProductId, Name, Price, Stock, IsActive " +
            "FROM dbo.Product WITH (UPDLOCK, ROWLOCK) " +
            $"WHERE ProductId IN ({parameterNames})",
            connection, transaction);

        for (var i = 0; i < productIds.Count; i++)
        {
            command.Parameters.AddWithValue($"@ProductId{i}", productIds[i]);
        }

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lookup[reader.GetInt32(0)] = new ProductStockInfo(
                reader.GetString(1),
                reader.GetDecimal(2),
                reader.GetInt32(3),
                reader.GetBoolean(4));
        }

        return lookup;
    }

    private static Dictionary<(int ProductId, int VariantId), VariantStockInfo> LoadVariantStockInfo(
        SqlConnection connection,
        SqlTransaction transaction,
        List<CartLine> lines)
    {
        var lookup = new Dictionary<(int ProductId, int VariantId), VariantStockInfo>();
        var variants = lines
            .Where(l => l.ProductVariantId.HasValue)
            .Select(l => (l.ProductId, VariantId: l.ProductVariantId!.Value))
            .Distinct()
            .ToList();

        if (variants.Count == 0)
        {
            return lookup;
        }

        var parameterNames = string.Join(", ", variants.Select((_, i) => $"@VariantId{i}"));
        using var command = new SqlCommand(
            "SELECT ProductId, ProductVariantId, Name, Stock, PriceAdjustment " +
            "FROM dbo.ProductVariant WITH (UPDLOCK, ROWLOCK) " +
            $"WHERE ProductVariantId IN ({parameterNames})",
            connection, transaction);

        for (var i = 0; i < variants.Count; i++)
        {
            command.Parameters.AddWithValue($"@VariantId{i}", variants[i].VariantId);
        }

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lookup[(reader.GetInt32(0), reader.GetInt32(1))] = new VariantStockInfo(
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetDecimal(4));
        }

        return lookup;
    }

    private static (decimal unitPrice, string productName, string? variantName) ValidateAndDecrementStock(
        SqlConnection connection, SqlTransaction transaction, CartLine line,
        Dictionary<int, ProductStockInfo> productLookup,
        Dictionary<(int ProductId, int VariantId), VariantStockInfo> variantLookup)
    {
        if (!productLookup.TryGetValue(line.ProductId, out var productInfo))
        {
            throw new InvalidOperationException($"Product {line.ProductId} is not available.");
        }

        if (!productInfo.IsActive)
        {
            throw new InvalidOperationException($"Product '{productInfo.Name}' is not available.");
        }

        line.UnitPrice = productInfo.Price;

        int? variantStock = null;
        string? variantName = null;
        if (line.ProductVariantId.HasValue)
        {
            if (!variantLookup.TryGetValue((line.ProductId, line.ProductVariantId.Value), out var variantInfo))
            {
                throw new InvalidOperationException("Selected product variant is not valid.");
            }

            variantName = variantInfo.Name;
            variantStock = variantInfo.Stock;
            line.UnitPrice += variantInfo.PriceAdjustment;
        }

        var effectiveStock = variantStock ?? productInfo.Stock;
        if (effectiveStock < line.Quantity)
        {
            throw new InvalidOperationException($"Not enough stock available for '{productInfo.Name}'.");
        }

        // Decrement product stock when there is no variant; otherwise decrement variant stock.
        if (line.ProductVariantId.HasValue)
        {
            using var updateVariant = new SqlCommand(
                "UPDATE dbo.ProductVariant SET Stock = Stock - @Quantity " +
                "WHERE ProductVariantId = @ProductVariantId",
                connection, transaction);
            updateVariant.Parameters.AddWithValue("@Quantity", line.Quantity);
            updateVariant.Parameters.AddWithValue("@ProductVariantId", line.ProductVariantId.Value);
            updateVariant.ExecuteNonQuery();
        }
        else
        {
            using var updateProduct = new SqlCommand(
                "UPDATE dbo.Product SET Stock = Stock - @Quantity " +
                "WHERE ProductId = @ProductId",
                connection, transaction);
            updateProduct.Parameters.AddWithValue("@Quantity", line.Quantity);
            updateProduct.Parameters.AddWithValue("@ProductId", line.ProductId);
            updateProduct.ExecuteNonQuery();
        }

        return (line.UnitPrice, productInfo.Name, variantName);
    }

    private readonly record struct ProductStockInfo(string Name, decimal Price, int Stock, bool IsActive);
    private readonly record struct VariantStockInfo(string Name, int Stock, decimal PriceAdjustment);

    private static OrderConfirmationDto BuildOrderConfirmation(
        int orderId,
        DateTime orderDate,
        string status,
        string shippingAddress,
        string shippingMethod,
        decimal totalAmount,
        IEnumerable<CartLine> lines)
    {
        var items = lines.Select(line => new OrderItemDto
        {
            ProductId = line.ProductId,
            ProductName = line.ProductName,
            VariantName = line.VariantName,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            LineTotal = line.UnitPrice * line.Quantity
        }).ToList();

        return new OrderConfirmationDto
        {
            OrderId = orderId,
            OrderDate = orderDate,
            Status = status,
            ShippingAddress = shippingAddress,
            ShippingMethod = shippingMethod,
            TotalAmount = totalAmount,
            Items = items
        };
    }

    [GeneratedRegex(@"\(([^)]+)\)$", RegexOptions.Compiled)]
    private static partial Regex ShippingMethodRegex();

    [GeneratedRegex(@"^(0[1-9]|1[0-2])\/[0-9]{2}$", RegexOptions.Compiled)]
    private static partial Regex ExpiryRegex();

    private sealed record CartLine
    {
        public int CartItemId { get; init; }
        public int ProductId { get; init; }
        public int? ProductVariantId { get; init; }
        public int Quantity { get; init; }
        public decimal UnitPrice { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? VariantName { get; set; }
    }
}
