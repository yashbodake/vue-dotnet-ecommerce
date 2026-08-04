using System.Text.RegularExpressions;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Data;
using Microsoft.Data.SqlClient;

namespace Ecommerce.Api.Services;

/// <summary>
/// Account service for authenticated users: order history and order detail.
/// All SQL is native and parameterized.
/// </summary>
public sealed partial class AccountService
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AccountService(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Returns the order history for the current user, most recent first.
    /// </summary>
    public List<OrderSummaryDto> GetOrderHistory(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();

        var orders = new List<OrderSummaryDto>();
        using var command = new SqlCommand("""
            SELECT o.OrderId, o.OrderDate, o.Status, o.TotalAmount, COUNT(oi.OrderItemId) AS ItemCount
            FROM dbo.Orders o
            LEFT JOIN dbo.OrderItem oi ON o.OrderId = oi.OrderId
            WHERE o.UserId = @UserId
            GROUP BY o.OrderId, o.OrderDate, o.Status, o.TotalAmount
            ORDER BY o.OrderDate DESC
            """, connection);
        command.Parameters.AddWithValue("@UserId", userId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            orders.Add(new OrderSummaryDto
            {
                OrderId = reader.GetInt32(0),
                OrderDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                TotalAmount = reader.GetDecimal(3),
                ItemCount = reader.GetInt32(4)
            });
        }

        return orders;
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

        var items = new List<OrderItemDto>();
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
                var unitPrice = reader.GetDecimal(4);
                var quantity = reader.GetInt32(3);
                items.Add(new OrderItemDto
                {
                    ProductId = reader.GetInt32(0),
                    ProductName = reader.GetString(1),
                    VariantName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    LineTotal = unitPrice * quantity
                });
            }
        }

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
}
