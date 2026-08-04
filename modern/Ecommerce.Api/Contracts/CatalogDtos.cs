namespace Ecommerce.Api.Contracts;

/// <summary>
/// Public DTO for Product - matches legacy CoreProduct shape.
/// Uses PascalCase (System.Text.Json camelCase by default).
/// </summary>
public sealed class ProductDto
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
/// Public DTO for Category - matches legacy CoreCategory shape.
/// </summary>
public sealed class CategoryDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Public DTO for ProductImage.
/// </summary>
public sealed class ProductImageDto
{
    public int ProductImageId { get; set; }
    public int ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Public DTO for ProductVariant.
/// </summary>
public sealed class ProductVariantDto
{
    public int ProductVariantId { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SkuSuffix { get; set; }
    public int Stock { get; set; }
    public decimal PriceAdjustment { get; set; }
}

/// <summary>
/// Product detail view model with images and variants.
/// </summary>
public sealed class ProductDetailDto
{
    public ProductDto Product { get; set; } = new();
    public IReadOnlyList<ProductImageDto> Images { get; set; } = [];
    public IReadOnlyList<ProductVariantDto> Variants { get; set; } = [];
    public int? SelectedVariantId { get; set; }
}

/// <summary>
/// Paged result wrapper for product listings.
/// </summary>
public sealed class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// Filter criteria for product search - matches legacy ProductFilterCriteria.
/// </summary>
public sealed class ProductFilterCriteria
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public List<int>? CategoryIds { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool InStockOnly { get; set; }
    public string? Search { get; set; }
    public string SortBy { get; set; } = "name"; // name, price_asc, price_desc, newest
}
