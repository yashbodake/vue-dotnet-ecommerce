namespace Ecommerce.Api.Data;

/// <summary>
/// Plain POCO for Product table - no EF attributes, no navigation properties.
/// Matches column names exactly for simple SQL mapping.
/// </summary>
public sealed class Product
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// Plain POCO for Category table.
/// </summary>
public sealed class Category
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Plain POCO for ProductImage table.
/// </summary>
public sealed class ProductImage
{
    public int ProductImageId { get; set; }
    public int ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Plain POCO for ProductVariant table.
/// </summary>
public sealed class ProductVariant
{
    public int ProductVariantId { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SkuSuffix { get; set; }
    public int Stock { get; set; }
    public decimal PriceAdjustment { get; set; }
}
