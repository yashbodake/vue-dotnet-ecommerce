using System.Data;
using System.Text;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Data;
using Microsoft.Data.SqlClient;

namespace Ecommerce.Api.Services;

/// <summary>
/// Catalog service using native parameterized SQL queries.
/// Parity with legacy ProductService.Filter() logic.
/// </summary>
public sealed class ProductCatalogService
{
    private const int MaxSearchLength = 100;

    private readonly ISqlConnectionFactory _connectionFactory;

    public ProductCatalogService(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Get categories that have at least one active product.
    /// Parity: ProductService.GetCategories()
    /// </summary>
    public IReadOnlyList<CategoryDto> GetCategories()
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand("""
            SELECT DISTINCT
                c.CategoryId,
                c.Name,
                c.ParentCategoryId,
                c.DisplayOrder
            FROM dbo.Category c
            INNER JOIN dbo.Product p ON c.CategoryId = p.CategoryId
            WHERE p.IsActive = 1
            ORDER BY c.DisplayOrder, c.Name
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
    /// Get product detail with images and variants.
    /// Parity: ProductService.GetDetail()
    /// </summary>
    public ProductDetailDto? GetProductDetail(int productId)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();

        // Batch the product, images, and variant reads into a single command / single round-trip
        using var command = new SqlCommand("""
            SELECT ProductId, CategoryId, Name, Description, Price, ThumbnailUrl, Stock, IsActive, CreatedDate
            FROM dbo.Product
            WHERE ProductId = @ProductId AND IsActive = 1;

            SELECT ProductImageId, ProductId, Url, DisplayOrder
            FROM dbo.ProductImage
            WHERE ProductId = @ProductId
            ORDER BY DisplayOrder;

            SELECT ProductVariantId, ProductId, Name, SkuSuffix, Stock, PriceAdjustment
            FROM dbo.ProductVariant
            WHERE ProductId = @ProductId;
            """, connection);
        command.Parameters.AddWithValue("@ProductId", productId);

        ProductDto? product = null;
        var images = new List<ProductImageDto>();
        var variants = new List<ProductVariantDto>();

        using var reader = command.ExecuteReader();

        // First result set: product
        if (reader.Read())
        {
            product = MapProduct(reader);
        }

        if (product == null) return null;

        // Second result set: images
        if (reader.NextResult())
        {
            while (reader.Read())
            {
                images.Add(new ProductImageDto
                {
                    ProductImageId = reader.GetInt32(0),
                    ProductId = reader.GetInt32(1),
                    Url = reader.GetString(2),
                    DisplayOrder = reader.GetInt32(3)
                });
            }
        }

        // Third result set: variants
        if (reader.NextResult())
        {
            while (reader.Read())
            {
                variants.Add(new ProductVariantDto
                {
                    ProductVariantId = reader.GetInt32(0),
                    ProductId = reader.GetInt32(1),
                    Name = reader.GetString(2),
                    SkuSuffix = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Stock = reader.GetInt32(4),
                    PriceAdjustment = reader.GetDecimal(5)
                });
            }
        }

        return new ProductDetailDto
        {
            Product = product,
            Images = images,
            Variants = variants,
            SelectedVariantId = variants.FirstOrDefault()?.ProductVariantId
        };
    }

    /// <summary>
    /// Filter/search/sort products with paging.
    /// Parity: ProductService.Filter() - uses OFFSET/FETCH for paging
    /// </summary>
    public PagedResultDto<ProductDto> FilterProducts(ProductFilterCriteria criteria)
    {
        var page = criteria.Page < 1 ? 1 : criteria.Page;
        var pageSize = criteria.PageSize < 1 ? 12 : criteria.PageSize;
        var offset = (page - 1) * pageSize;

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();

        // Build WHERE clause dynamically
        var whereClauses = new List<string> { "p.IsActive = 1" };
        var parameters = new Dictionary<string, object>();

        if (criteria.CategoryIds != null && criteria.CategoryIds.Count > 0)
        {
            var paramNames = new List<string>();
            for (int i = 0; i < criteria.CategoryIds.Count; i++)
            {
                var paramName = $"@catId{i}";
                parameters[paramName] = criteria.CategoryIds[i];
                paramNames.Add(paramName);
            }
            whereClauses.Add($"p.CategoryId IN ({string.Join(",", paramNames)})");
        }

        if (criteria.MinPrice.HasValue)
        {
            parameters["@minPrice"] = criteria.MinPrice.Value;
            whereClauses.Add("p.Price >= @minPrice");
        }

        if (criteria.MaxPrice.HasValue)
        {
            parameters["@maxPrice"] = criteria.MaxPrice.Value;
            whereClauses.Add("p.Price <= @maxPrice");
        }

        if (criteria.InStockOnly)
        {
            whereClauses.Add("p.Stock > 0");
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var searchTerm = criteria.Search.Trim();
            if (searchTerm.Length > MaxSearchLength)
            {
                searchTerm = searchTerm[..MaxSearchLength];
            }

            parameters["@search"] = $"%{searchTerm}%";
            whereClauses.Add("(p.Name LIKE @search OR (p.Description IS NOT NULL AND p.Description LIKE @search))");
        }

        var whereSql = string.Join(" AND ", whereClauses);

        // Build ORDER BY
        var orderBySql = (criteria.SortBy ?? "name").ToLowerInvariant() switch
        {
            "price_asc" => "p.Price ASC, p.Name ASC",
            "price_desc" => "p.Price DESC, p.Name DESC",
            "newest" => "p.CreatedDate DESC, p.Name ASC",
            _ => "p.Name ASC"
        };

        // Build count query
        var countSql = $"""
            SELECT COUNT(*)
            FROM dbo.Product p
            WHERE {whereSql}
            """;

        using var cmdCount = new SqlCommand(countSql, connection);
        foreach (var kvp in parameters)
        {
            cmdCount.Parameters.AddWithValue(kvp.Key, kvp.Value);
        }
        var totalCount = (int)cmdCount.ExecuteScalar()!;

        // Build data query with OFFSET/FETCH
        var dataSql = $"""
            SELECT p.ProductId, p.CategoryId, p.Name, p.Description, p.Price, p.ThumbnailUrl, p.Stock, p.IsActive, p.CreatedDate
            FROM dbo.Product p
            WHERE {whereSql}
            ORDER BY {orderBySql}
            OFFSET @offset ROWS
            FETCH NEXT @pageSize ROWS ONLY
            """;

        using var cmdData = new SqlCommand(dataSql, connection);
        foreach (var kvp in parameters)
        {
            cmdData.Parameters.AddWithValue(kvp.Key, kvp.Value);
        }
        cmdData.Parameters.AddWithValue("@offset", offset);
        cmdData.Parameters.AddWithValue("@pageSize", pageSize);

        var products = new List<ProductDto>();
        using (var reader = cmdData.ExecuteReader())
        {
            while (reader.Read())
            {
                products.Add(MapProduct(reader));
            }
        }

        return new PagedResultDto<ProductDto>
        {
            Items = products,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private static ProductDto MapProduct(IDataReader reader)
    {
        return new ProductDto
        {
            ProductId = reader.GetInt32(0),
            CategoryId = reader.GetInt32(1),
            Name = reader.GetString(2),
            Description = reader.IsDBNull(3) ? null : reader.GetString(3),
            Price = reader.GetDecimal(4),
            ThumbnailUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
            Stock = reader.GetInt32(6),
            IsActive = reader.GetBoolean(7),
            CreatedDate = reader.GetDateTime(8)
        };
    }
}
