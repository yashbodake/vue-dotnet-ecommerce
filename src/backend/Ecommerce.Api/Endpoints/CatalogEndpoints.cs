using Ecommerce.Api.Contracts;
using Ecommerce.Api.Services;

namespace Ecommerce.Api.Endpoints;

/// <summary>
/// Catalog endpoints: categories, products, product detail.
/// All queries use parameterized SQL via ProductCatalogService.
/// </summary>
public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var catalog = app.MapGroup("/api");

        // GET /api/categories - list categories with active products
        catalog.MapGet("/categories", (ProductCatalogService service) =>
        {
            var categories = service.GetCategories();
            return Results.Ok(categories);
        })
        .WithName("GetCategories")
        .WithOpenApi();

        // GET /api/products - filter/search/sort/paging
        // Query params: page, pageSize, categoryIds, minPrice, maxPrice, inStockOnly, search, sortBy
        catalog.MapGet("/products", (ProductCatalogService service, HttpContext context) =>
        {
            var criteria = ExtractFilterCriteria(context.Request.Query);
            var result = service.FilterProducts(criteria);
            return Results.Ok(result);
        })
        .WithName("GetProducts")
        .WithOpenApi();

        // GET /api/products/{id} - product detail with images and variants
        catalog.MapGet("/products/{id:int}", (ProductCatalogService service, int id) =>
        {
            var detail = service.GetProductDetail(id);
            return detail is not null ? Results.Ok(detail) : Results.NotFound();
        })
        .WithName("GetProductDetail")
        .WithOpenApi();
    }

    private static ProductFilterCriteria ExtractFilterCriteria(IQueryCollection query)
    {
        var criteria = new ProductFilterCriteria();

        if (int.TryParse(query["page"], out var page))
            criteria.Page = page;

        if (int.TryParse(query["pageSize"], out var pageSize))
            criteria.PageSize = pageSize;

        if (decimal.TryParse(query["minPrice"], out var minPrice))
            criteria.MinPrice = minPrice;

        if (decimal.TryParse(query["maxPrice"], out var maxPrice))
            criteria.MaxPrice = maxPrice;

        criteria.InStockOnly = query["inStockOnly"].FirstOrDefault()?.ToLower() == "true";

        if (query.TryGetValue("search", out var search))
            criteria.Search = search.FirstOrDefault();

        if (query.TryGetValue("sortBy", out var sortBy))
            criteria.SortBy = sortBy.FirstOrDefault() ?? "name";

        // Parse categoryIds as comma-separated list
        if (query.TryGetValue("categoryIds", out var categoryIds))
        {
            var ids = categoryIds.FirstOrDefault()?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            if (ids != null && ids.Count > 0)
                criteria.CategoryIds = ids;
        }

        return criteria;
    }
}
