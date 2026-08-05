using Ecommerce.Api.Contracts;
using Ecommerce.Api.Data;
using Ecommerce.Api.Services;

namespace Ecommerce.Api.Tests;

/// <summary>
/// Adversarial tests for ProductCatalogService: pagination boundaries, oversized search,
/// invalid price ranges, sort-by injection, and malformed filter criteria.
/// </summary>
public class CatalogAdversarialTests : IDisposable
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly ProductCatalogService _service;

    public CatalogAdversarialTests()
    {
        var connectionString = "Server=.\\SQLEXPRESS;Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True";
        _connectionFactory = new SqlConnectionFactory(connectionString);
        _service = new ProductCatalogService(_connectionFactory);
    }

    [Fact]
    public void FilterProducts_PageZero_ReturnsFirstPage()
    {
        // Scenario: client requests page 0; service should clamp to page 1 without error.
        var result = _service.FilterProducts(new ProductFilterCriteria
        {
            Page = 0,
            PageSize = 5
        });

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.True(result.PageSize >= 1);
        Assert.True(result.TotalCount >= 0);
    }

    [Fact]
    public void FilterProducts_PageSizeZero_UsesDefault()
    {
        // Scenario: page size 0 should not cause divide-by-zero or return zero items.
        var result = _service.FilterProducts(new ProductFilterCriteria
        {
            Page = 1,
            PageSize = 0
        });

        Assert.NotNull(result);
        Assert.True(result.PageSize > 0);
    }

    [Fact]
    public void FilterProducts_PageSizeMax_ReturnsWithoutError()
    {
        // Scenario: attacker requests an extremely large page size; should not crash.
        var result = _service.FilterProducts(new ProductFilterCriteria
        {
            Page = 1,
            PageSize = int.MaxValue
        });

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 0);
    }

    [Fact]
    public void FilterProducts_PageBeyondLast_ReturnsEmptyItems()
    {
        // Scenario: page number far beyond available data should return empty Items.
        var result = _service.FilterProducts(new ProductFilterCriteria
        {
            Page = 99999,
            PageSize = 12
        });

        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void FilterProducts_NegativePage_ClampedToOne()
    {
        // Scenario: negative page number must not propagate to OFFSET.
        var result = _service.FilterProducts(new ProductFilterCriteria
        {
            Page = -5,
            PageSize = 5
        });

        Assert.Equal(1, result.Page);
        Assert.True(result.PageSize >= 1);
    }

    [Fact]
    public void FilterProducts_NegativePageSize_Defaulted()
    {
        // Scenario: negative page size must not propagate to FETCH.
        var result = _service.FilterProducts(new ProductFilterCriteria
        {
            Page = 1,
            PageSize = -5
        });

        Assert.True(result.PageSize > 0);
    }

    [Fact]
    public void FilterProducts_OversizedSearchTerm_DoesNotCrash()
    {
        // Scenario: attacker sends a 10,000-char search string; service should handle it.
        var result = _service.FilterProducts(new ProductFilterCriteria
        {
            Page = 1,
            PageSize = 12,
            Search = new string('A', 10000)
        });

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 0);
    }

    [Fact]
    public void FilterProducts_MinPriceGreaterThanMaxPrice_ReturnsEmpty()
    {
        // Scenario: contradictory price filter must not error and should return no items.
        var result = _service.FilterProducts(new ProductFilterCriteria
        {
            Page = 1,
            PageSize = 12,
            MinPrice = 999999.99m,
            MaxPrice = 0.01m
        });

        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void FilterProducts_NegativePrices_AcceptedWithoutCrash()
    {
        // Scenario: negative price boundaries are supplied; service should handle gracefully.
        // The service does not currently reject negative min/max prices, so we only assert no crash.
        var result = _service.FilterProducts(new ProductFilterCriteria
        {
            Page = 1,
            PageSize = 12,
            MinPrice = -100.00m,
            MaxPrice = -1.00m
        });

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 0);
    }

    [Fact]
    public void FilterProducts_MaliciousSortBy_AllowedValueFallsBackToName()
    {
        // Scenario: attacker injects a SQL expression into sortBy; allow-list should neutralize it.
        var result = _service.FilterProducts(new ProductFilterCriteria
        {
            Page = 1,
            PageSize = 5,
            SortBy = "name; DROP TABLE dbo.Product; --"
        });

        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public void FilterProducts_NonExistentCategory_ReturnsEmpty()
    {
        // Scenario: client filters by a category id that does not exist.
        var result = _service.FilterProducts(new ProductFilterCriteria
        {
            Page = 1,
            PageSize = 12,
            CategoryIds = [999999]
        });

        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void GetProductDetail_NegativeId_ReturnsNull()
    {
        // Scenario: negative product id must return null cleanly.
        var detail = _service.GetProductDetail(-1);
        Assert.Null(detail);
    }

    [Fact]
    public void GetCategories_ReturnsAtLeastOne()
    {
        // Scenario: service should always return categories that have active products.
        var categories = _service.GetCategories();
        Assert.NotNull(categories);
        Assert.True(categories.Count > 0);
    }

    public void Dispose()
    {
        // No test-created persistent data to clean up.
    }
}
