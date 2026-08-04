using System.Security.Claims;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Ecommerce.Api.Endpoints;

/// <summary>
/// Account endpoints: authenticated user order history and order detail.
/// </summary>
public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var account = app.MapGroup("/api/account");

        // GET /api/account/orders - current user's order history
        account.MapGet("/orders",
            [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
            (AccountService accountService, HttpContext context) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var orders = accountService.GetOrderHistory(userId);
            return Results.Ok(orders);
        })
        .WithName("GetOrderHistory")
        .WithOpenApi();

        // GET /api/account/orders/{orderId} - order detail for current user
        account.MapGet("/orders/{orderId:int}",
            [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
            (int orderId, AccountService accountService, HttpContext context) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            try
            {
                var detail = accountService.GetOrderDetail(orderId, userId);
                return Results.Ok(detail);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("GetAccountOrderDetail")
        .WithOpenApi();
    }
}
