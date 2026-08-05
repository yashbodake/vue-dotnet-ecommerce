using System.Text;
using Ecommerce.Api.Data;
using Ecommerce.Api.Endpoints;
using Ecommerce.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Register SQL connection factory for native SQL access (no EF)
builder.Services.AddSingleton<ISqlConnectionFactory>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("DefaultConnection");
    return new SqlConnectionFactory(connectionString!);
});

// Register catalog service
builder.Services.AddSingleton<ProductCatalogService>();

// Register cart service
builder.Services.AddSingleton<CartService>();

// Register checkout service
builder.Services.AddSingleton<CheckoutService>();

// Register account service
builder.Services.AddSingleton<AccountService>();

// Register admin service
builder.Services.AddSingleton<AdminService>();

// Register auth service with JWT settings
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var jwtConfig = config.GetSection("Jwt");
    return new AuthService(
        sp.GetRequiredService<ISqlConnectionFactory>(),
        jwtConfig["SigningKey"]!,
        jwtConfig["Issuer"]!,
        jwtConfig["Audience"]!,
        int.Parse(jwtConfig["ExpirationMinutes"] ?? "60")
    );
});

// Register admin user seeder
builder.Services.AddSingleton<AdminUserSeeder>();

// Configure CORS to allow gateway and dev origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowGateway", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5000",
                "http://localhost:5173",
                "http://127.0.0.1:5000",
                "http://127.0.0.1:5173")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Configure JWT authentication
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
const string JwtSigningKeyPlaceholder = "REPLACE_VIA_JWT_SIGNINGKEY_ENV_OR_SECRETS";
if (builder.Environment.IsProduction()
    && (string.IsNullOrWhiteSpace(jwtSigningKey) || jwtSigningKey == JwtSigningKeyPlaceholder))
{
    throw new InvalidOperationException(
        "JWT signing key is not configured. Set a strong secret via the Jwt:SigningKey configuration " +
        "(e.g. ASPNETCORE_Jwt__SigningKey environment variable) and remove the placeholder value.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey ?? JwtSigningKeyPlaceholder)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Seed admin user on startup (skip in Testing env per S03 CONTEXT pitfalls)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<AdminUserSeeder>();
    seeder.EnsureAdminUser();

    // Warm the SQL connection pool so the first shop request is not a cold login.
    var connectionFactory = scope.ServiceProvider.GetRequiredService<ISqlConnectionFactory>();
    using var warm = connectionFactory.CreateConnection();
    CatalogHealthQuery.Execute(warm);
}

app.UseCors("AllowGateway");

app.UseAuthentication();
app.UseAuthorization();

// Health endpoint - smoke test for database connectivity
app.MapGet("/api/health", (ISqlConnectionFactory connectionFactory, ILogger<Program> logger) =>
{
    try
    {
        using var connection = connectionFactory.CreateConnection();
        var count = CatalogHealthQuery.Execute(connection);
        return Results.Ok(new { Status = "healthy", ProductCount = count });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Health check failed: database connectivity error");
        return Results.Problem("Database connection failed.", statusCode: 503);
    }
});

// Map catalog endpoints
app.MapCatalogEndpoints();

// Map cart endpoints
app.MapCartEndpoints();

// Map checkout endpoints
app.MapCheckoutEndpoints();

// Map auth endpoints
app.MapAuthEndpoints();

// Map account endpoints
app.MapAccountEndpoints();

// Map admin endpoints
app.MapAdminEndpoints();

app.MapGet("/", () => "Ecommerce Modern API");

app.Run();
