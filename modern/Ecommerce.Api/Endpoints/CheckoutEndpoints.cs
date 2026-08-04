using System.Security.Claims;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Ecommerce.Api.Endpoints;

/// <summary>
/// Checkout endpoints: shipping options (public) and authenticated place-order / order detail.
/// </summary>
public static class CheckoutEndpoints
{
    public static void MapCheckoutEndpoints(this IEndpointRouteBuilder app)
    {
        var checkout = app.MapGroup("/api/checkout");

        // GET /api/checkout/shipping-options - public, no auth required
        checkout.MapGet("/shipping-options", (CheckoutService checkoutService) =>
        {
            var options = checkoutService.GetShippingOptions();
            return Results.Ok(options);
        })
        .WithName("GetShippingOptions")
        .WithOpenApi();

        // POST /api/checkout/place-order - requires JWT
        checkout.MapPost("/place-order",
            [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
            (PlaceOrderRequest request, CheckoutService checkoutService, HttpContext context) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            try
            {
                var confirmation = checkoutService.PlaceOrder(userId, request);
                return Results.Created($"/api/checkout/orders/{confirmation.OrderId}", confirmation);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .WithName("PlaceOrder")
        .WithOpenApi();

        // GET /api/checkout/orders/{orderId} - requires JWT, IDOR protected
        checkout.MapGet("/orders/{orderId:int}",
            [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
            (int orderId, CheckoutService checkoutService, HttpContext context) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            try
            {
                var confirmation = checkoutService.GetOrderDetail(orderId, userId);
                return Results.Ok(confirmation);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("GetOrderDetail")
        .WithOpenApi();
    }
}
