using Ecommerce.Api.Contracts;
using Ecommerce.Api.Services;

namespace Ecommerce.Api.Endpoints;

/// <summary>
/// Admin endpoints: product CRUD (soft-delete), categories, and order status.
/// All endpoints require JWT authentication and the Admin role via the "Admin" policy.
/// </summary>
public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin")
            .RequireAuthorization("Admin");

        // GET /api/admin/products - list all products including inactive
        admin.MapGet("/products", (AdminService adminService) =>
        {
            var products = adminService.GetAllProducts();
            return Results.Ok(products);
        })
        .WithName("GetAdminProducts")
        .WithOpenApi();

        // GET /api/admin/products/{id} - single product
        admin.MapGet("/products/{id:int}", (int id, AdminService adminService) =>
        {
            var product = adminService.GetProductById(id);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        })
        .WithName("GetAdminProduct")
        .WithOpenApi();

        // POST /api/admin/products - create product
        admin.MapPost("/products", (CreateProductRequest request, AdminService adminService) =>
        {
            try
            {
                var product = adminService.CreateProduct(request);
                return Results.Created($"/api/admin/products/{product.ProductId}", product);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .WithName("CreateAdminProduct")
        .WithOpenApi();

        // PUT /api/admin/products/{id} - update product
        admin.MapPut("/products/{id:int}", (int id, UpdateProductRequest request, AdminService adminService) =>
        {
            try
            {
                var product = adminService.UpdateProduct(id, request);
                return Results.Ok(product);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .WithName("UpdateAdminProduct")
        .WithOpenApi();

        // DELETE /api/admin/products/{id} - soft-delete product
        admin.MapDelete("/products/{id:int}", (int id, AdminService adminService) =>
        {
            try
            {
                adminService.SoftDeleteProduct(id);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("DeleteAdminProduct")
        .WithOpenApi();

        // GET /api/admin/categories - list categories for product form dropdown
        admin.MapGet("/categories", (AdminService adminService) =>
        {
            var categories = adminService.GetAllCategories();
            return Results.Ok(categories);
        })
        .WithName("GetAdminCategories")
        .WithOpenApi();

        // GET /api/admin/orders - list all orders, optional status filter
        admin.MapGet("/orders", (AdminService adminService, HttpContext context) =>
        {
            var statusFilter = context.Request.Query["status"].FirstOrDefault();
            var orders = adminService.GetAllOrders(statusFilter);
            return Results.Ok(orders);
        })
        .WithName("GetAdminOrders")
        .WithOpenApi();

        // PUT /api/admin/orders/{id}/status - update order status
        admin.MapPut("/orders/{id:int}/status", (int id, UpdateOrderStatusRequest request, AdminService adminService) =>
        {
            try
            {
                var order = adminService.UpdateOrderStatus(id, request.Status);
                return Results.Ok(order);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .WithName("UpdateAdminOrderStatus")
        .WithOpenApi();
    }
}
