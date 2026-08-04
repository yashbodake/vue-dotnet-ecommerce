namespace Ecommerce.Api.Data;

/// <summary>
/// Plain POCO for Orders table - no EF attributes, no navigation properties.
/// </summary>
public sealed class Order
{
    public int OrderId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Plain POCO for OrderItem table - no EF attributes, no navigation properties.
/// </summary>
public sealed class OrderItem
{
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
