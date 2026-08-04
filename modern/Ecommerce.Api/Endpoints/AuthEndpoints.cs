using System.Security.Claims;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Ecommerce.Api.Endpoints;

/// <summary>
/// Auth endpoints: login, me.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth");

        // POST /api/auth/login - authenticate and return JWT
        auth.MapPost("/login", async (LoginRequest request, AuthService authService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest("Email and password are required");
            }

            var response = await authService.LoginAsync(request.Email, request.Password);
            if (response == null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(response);
        })
        .WithName("Login")
        .WithOpenApi();

        // POST /api/auth/register - create a new customer account
        auth.MapPost("/register", async (RegisterRequest request, AuthService authService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest("Email and password are required");
            }

            try
            {
                var response = await authService.RegisterAsync(request.Email, request.Password);
                if (response == null)
                {
                    return Results.BadRequest("Registration failed");
                }

                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .WithName("Register")
        .WithOpenApi();

        // GET /api/auth/me - get current user info (requires Bearer token)
        auth.MapGet("/me", async (ClaimsPrincipal user, AuthService authService) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var userInfo = await authService.GetUserInfoAsync(userId);
            if (userInfo == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(userInfo);
        })
        .RequireAuthorization(JwtBearerDefaults.AuthenticationScheme)
        .WithName("GetMe")
        .WithOpenApi();
    }
}
