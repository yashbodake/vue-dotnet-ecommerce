namespace Ecommerce.Api.Contracts;

/// <summary>
/// Admin view of a product, including category name. Includes inactive products.
/// </summary>
public sealed class AdminProductDto
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Request to create a new product.
/// </summary>
public sealed class CreateProductRequest
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request to update an existing product.
/// </summary>
public sealed class UpdateProductRequest
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Admin view of an order summary with item count.
/// </summary>
public sealed class AdminOrderDto
{
    public int OrderId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
}

/// <summary>
/// Request to update the status of an order.
/// </summary>
public sealed class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
