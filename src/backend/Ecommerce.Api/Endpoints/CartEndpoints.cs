using System.Security.Claims;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Ecommerce.Api.Endpoints;

/// <summary>
/// Cart endpoints: anonymous cookie cart and authenticated user cart.
/// </summary>
public static class CartEndpoints
{
    public static void MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var cart = app.MapGroup("/api/cart");

        // GET /api/cart - return the current cart for guest or authenticated user
        cart.MapGet("/", (CartService cartService, HttpContext context) =>
        {
            var (ownerId, isGuest) = ResolveOwnerId(context);
            var cartDto = cartService.GetCart(ownerId, isGuest);
            return Results.Ok(cartDto);
        })
        .WithName("GetCart")
        .WithOpenApi();

        // GET /api/cart/count - return total quantity in cart
        cart.MapGet("/count", (CartService cartService, HttpContext context) =>
        {
            var (ownerId, isGuest) = ResolveOwnerId(context);
            var count = cartService.GetItemCount(ownerId, isGuest);
            return Results.Ok(new { Count = count });
        })
        .WithName("GetCartCount")
        .WithOpenApi();

        // POST /api/cart/items - add item, create guest cookie if needed
        cart.MapPost("/items", (AddCartItemRequest request, CartService cartService, HttpContext context) =>
        {
            var (ownerId, isGuest) = ResolveOrCreateOwnerId(context, out var createdCookie);

            try
            {
                cartService.AddItem(ownerId, isGuest, request.ProductId, request.VariantId, request.Quantity);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }

            var cartDto = cartService.GetCart(ownerId, isGuest);
            var result = Results.Created($"/api/cart", cartDto);
            if (createdCookie)
            {
                AppendCartOwnerCookie(context, ownerId);
            }
            return result;
        })
        .WithName("AddCartItem")
        .WithOpenApi();

        // PUT /api/cart/items/{cartItemId} - update quantity
        cart.MapPut("/items/{cartItemId:int}", (int cartItemId, UpdateCartQuantityRequest request, CartService cartService, HttpContext context) =>
        {
            var (ownerId, isGuest) = ResolveOwnerId(context);
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return Results.NotFound();
            }

            try
            {
                cartService.UpdateQuantity(ownerId, isGuest, cartItemId, request.Quantity);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }

            return Results.Ok(cartService.GetCart(ownerId, isGuest));
        })
        .WithName("UpdateCartItem")
        .WithOpenApi();

        // DELETE /api/cart/items/{cartItemId} - remove item
        cart.MapDelete("/items/{cartItemId:int}", (int cartItemId, CartService cartService, HttpContext context) =>
        {
            var (ownerId, isGuest) = ResolveOwnerId(context);
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return Results.NotFound();
            }

            try
            {
                cartService.RemoveItem(ownerId, isGuest, cartItemId);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }

            return Results.Ok(cartService.GetCart(ownerId, isGuest));
        })
        .WithName("RemoveCartItem")
        .WithOpenApi();

        // POST /api/cart/merge - merge guest cart into authenticated user cart
        cart.MapPost("/merge", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] (MergeCartRequest? request, CartService cartService, HttpContext context) =>
        {
            var authUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(authUserId))
            {
                return Results.Unauthorized();
            }

            var guestOwnerId = request?.GuestOwnerId ?? context.Request.Cookies["ecommerce.cart_owner"];
            if (string.IsNullOrWhiteSpace(guestOwnerId))
            {
                // Nothing to merge; just return current user cart.
                return Results.Ok(cartService.GetCart(authUserId, isGuest: false));
            }

            try
            {
                cartService.MergeGuestCart(guestOwnerId, authUserId);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }

            // Clear the guest cookie after merge.
            context.Response.Cookies.Delete("ecommerce.cart_owner");

            return Results.Ok(cartService.GetCart(authUserId, isGuest: false));
        })
        .WithName("MergeCart")
        .WithOpenApi();
    }

    /// <summary>
    /// Resolve owner from JWT (authenticated) or cookie (guest).
    /// Returns (ownerId, isGuest). Empty ownerId when neither is present.
    /// </summary>
    private static (string ownerId, bool isGuest) ResolveOwnerId(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return (context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty, false);
        }

        return (context.Request.Cookies["ecommerce.cart_owner"] ?? string.Empty, true);
    }

    /// <summary>
    /// Resolve or create owner id. Generates a guest GUID + cookie when no
    /// auth and no cookie exist. createdCookie signals the endpoint to set it.
    /// </summary>
    private static (string ownerId, bool isGuest) ResolveOrCreateOwnerId(HttpContext context, out bool createdCookie)
    {
        createdCookie = false;

        if (context.User.Identity?.IsAuthenticated == true)
        {
            return (context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.NewGuid().ToString(), false);
        }

        var cookieValue = context.Request.Cookies["ecommerce.cart_owner"];
        if (!string.IsNullOrWhiteSpace(cookieValue))
        {
            return (cookieValue, true);
        }

        createdCookie = true;
        return (Guid.NewGuid().ToString("D"), true);
    }

    private static void AppendCartOwnerCookie(HttpContext context, string ownerId)
    {
        context.Response.Cookies.Append("ecommerce.cart_owner", ownerId, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            Path = "/"
        });
    }
}