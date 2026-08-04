namespace Ecommerce.Api.Contracts;

/// <summary>
/// Single cart line with product and variant details.
/// </summary>
public sealed class CartItemDto
{
    public int CartItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal ProductPrice { get; set; }
    public int? VariantId { get; set; }
    public string? VariantName { get; set; }
    public string? VariantSkuSuffix { get; set; }
    public decimal PriceAdjustment { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public int Stock { get; set; }
}

/// <summary>
/// Full cart view with summary numbers.
/// </summary>
public sealed class CartDto
{
    public IReadOnlyList<CartItemDto> Items { get; set; } = [];
    public int ItemCount { get; set; }
    public decimal Total { get; set; }
}

/// <summary>
/// Add item request. VariantId is optional.
/// </summary>
public sealed class AddCartItemRequest
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
/// Update quantity request.
/// </summary>
public sealed class UpdateCartQuantityRequest
{
    public int Quantity { get; set; }
}

/// <summary>
/// Merge guest cart into authenticated user cart.
/// Guest owner may be supplied explicitly or read from the cookie by the endpoint.
/// </summary>
public sealed class MergeCartRequest
{
    public string? GuestOwnerId { get; set; }
}
