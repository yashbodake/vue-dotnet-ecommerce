using System.Data;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Data;
using Ecommerce.Api.Services;
using Microsoft.Data.SqlClient;

namespace Ecommerce.Api.Tests;

/// <summary>
/// Integration tests for catalog endpoints using real SQL Express database.
/// Requires: .\SQLEXPRESS with LegacyEcommerceDb seeded.
/// </summary>
public class CatalogEndpointsTests : IDisposable
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly ProductCatalogService _service;

    public CatalogEndpointsTests()
    {
        // Use same connection string as appsettings.json for host testing
        var connectionString = "Server=.\\SQLEXPRESS;Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True";
        _connectionFactory = new SqlConnectionFactory(connectionString);
        _service = new ProductCatalogService(_connectionFactory);
    }

    [Fact]
    public void GetCategories_ReturnsNonEmptyList_WhenActiveProductsExist()
    {
        // Act
        var categories = _service.GetCategories();

        // Assert
        Assert.NotNull(categories);
        // Categories should have at least one item if DB is seeded
        Assert.NotEmpty(categories);
        // Verify category properties are populated
        var category = categories.First();
        Assert.NotEmpty(category.Name);
        Assert.True(category.CategoryId > 0);
    }

    [Fact]
    public void GetProductDetail_ReturnsProduct_WhenProductIdExists()
    {
        // Arrange - get first product ID from database
        var firstProduct = GetFirstProductId();
        if (firstProduct == null)
        {
            // Skip if no products in DB
            return;
        }

        // Act
        var detail = _service.GetProductDetail(firstProduct.Value);

        // Assert
        Assert.NotNull(detail);
        Assert.NotNull(detail.Product);
        Assert.NotEmpty(detail.Product.Name);
        Assert.True(detail.Product.IsActive);
    }

    [Fact]
    public void GetProductDetail_ReturnsNull_WhenProductIdDoesNotExist()
    {
        // Act
        var detail = _service.GetProductDetail(-1);

        // Assert
        Assert.Null(detail);
    }

    [Fact]
    public void FilterProducts_ReturnsPagedResult_WithDefaultCriteria()
    {
        // Act
        var result = _service.FilterProducts(new ProductFilterCriteria());

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.True(result.Page >= 1);
        Assert.True(result.PageSize >= 1);
        // TotalCount should match Items count when all items fit on one page
        if (result.TotalCount <= result.PageSize)
        {
            Assert.Equal(result.TotalCount, result.Items.Count);
        }
    }

    [Fact]
    public void FilterProducts_AppliesPageSize_LimitsResults()
    {
        // Arrange
        var criteria = new ProductFilterCriteria { Page = 1, PageSize = 5 };

        // Act
        var result = _service.FilterProducts(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Items.Count <= 5);
        Assert.Equal(1, result.Page);
        Assert.Equal(5, result.PageSize);
    }

    [Fact]
    public void FilterProducts_AppliesSearch_FiltersByName()
    {
        // Arrange - use a search term that should match something in seeded DB
        var criteria = new ProductFilterCriteria { Search = "a", Page = 1, PageSize = 100 };

        // Act
        var result = _service.FilterProducts(criteria);

        // Assert
        Assert.NotNull(result);
        // All returned products should contain "a" in name or description
        foreach (var product in result.Items)
        {
            Assert.True(
                product.Name.Contains("a", StringComparison.OrdinalIgnoreCase) ||
                (product.Description?.Contains("a", StringComparison.OrdinalIgnoreCase) ?? false),
                $"Product '{product.Name}' should contain 'a'"
            );
        }
    }

    [Fact]
    public void FilterProducts_SortByPriceAsc_ReturnsAscendingOrder()
    {
        // Arrange
        var criteria = new ProductFilterCriteria { SortBy = "price_asc", Page = 1, PageSize = 100 };

        // Act
        var result = _service.FilterProducts(criteria);

        // Assert
        Assert.NotNull(result);
        if (result.Items.Count > 1)
        {
            for (int i = 1; i < result.Items.Count; i++)
            {
                Assert.True(result.Items[i].Price >= result.Items[i - 1].Price);
            }
        }
    }

    [Fact]
    public void FilterProducts_SortByPriceDesc_ReturnsDescendingOrder()
    {
        // Arrange
        var criteria = new ProductFilterCriteria { SortBy = "price_desc", Page = 1, PageSize = 100 };

        // Act
        var result = _service.FilterProducts(criteria);

        // Assert
        Assert.NotNull(result);
        if (result.Items.Count > 1)
        {
            for (int i = 1; i < result.Items.Count; i++)
            {
                Assert.True(result.Items[i].Price <= result.Items[i - 1].Price);
            }
        }
    }

    [Fact]
    public void FilterProducts_InStockOnly_ExcludesZeroStock()
    {
        // Arrange
        var criteria = new ProductFilterCriteria { InStockOnly = true, Page = 1, PageSize = 100 };

        // Act
        var result = _service.FilterProducts(criteria);

        // Assert
        Assert.NotNull(result);
        foreach (var product in result.Items)
        {
            Assert.True(product.Stock > 0, $"Product '{product.Name}' should be in stock");
        }
    }

    [Fact]
    public void FilterProducts_PriceRange_FiltersCorrectly()
    {
        // Arrange
        var criteria = new ProductFilterCriteria
        {
            MinPrice = 10,
            MaxPrice = 100,
            Page = 1,
            PageSize = 100
        };

        // Act
        var result = _service.FilterProducts(criteria);

        // Assert
        Assert.NotNull(result);
        foreach (var product in result.Items)
        {
            Assert.True(product.Price >= 10 && product.Price <= 100);
        }
    }

    [Fact]
    public void FilterProducts_CategoryIds_FiltersByCategory()
    {
        // Arrange - get first category ID
        var categories = _service.GetCategories();
        if (categories.Count == 0)
        {
            return; // Skip if no categories
        }

        var firstCategoryId = categories.First().CategoryId;
        var criteria = new ProductFilterCriteria
        {
            CategoryIds = [firstCategoryId],
            Page = 1,
            PageSize = 100
        };

        // Act
        var result = _service.FilterProducts(criteria);

        // Assert
        Assert.NotNull(result);
        foreach (var product in result.Items)
        {
            Assert.Equal(firstCategoryId, product.CategoryId);
        }
    }

    [Fact]
    public void FilterProducts_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        var criteria1 = new ProductFilterCriteria { Page = 1, PageSize = 5 };
        var criteria2 = new ProductFilterCriteria { Page = 2, PageSize = 5 };

        // Act
        var page1 = _service.FilterProducts(criteria1);
        var page2 = _service.FilterProducts(criteria2);

        // Assert
        Assert.NotNull(page1);
        Assert.NotNull(page2);
        Assert.Equal(1, page1.Page);
        Assert.Equal(2, page2.Page);
        // Page numbers should be different
        Assert.NotEqual(page1.Page, page2.Page);
        // If we have enough items, page 2 should have items
        if (page1.TotalCount > 5)
        {
            Assert.True(page2.Items.Count > 0, "Page 2 should have items when total > 5");
        }
    }

    private int? GetFirstProductId()
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var command = new SqlCommand(
            "SELECT TOP 1 ProductId FROM dbo.Product WHERE IsActive = 1",
            connection);
        var result = command.ExecuteScalar();
        return result != null ? (int?)result : null;
    }

    public void Dispose()
    {
        // Connection factory and service don't need explicit disposal
        // in this simple test setup
    }
}
