namespace Ecommerce.Api.Data;

/// <summary>
/// Plain POCO for CartItem table - no EF attributes, no navigation properties.
/// </summary>
public sealed class CartItem
{
    public int CartItemId { get; set; }
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public DateTime AddedDate { get; set; }
}
