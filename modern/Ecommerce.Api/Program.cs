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

// Configure JWT authentication
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]!;
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

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
}

app.UseAuthentication();
app.UseAuthorization();

// Health endpoint - smoke test for database connectivity
app.MapGet("/api/health", (ISqlConnectionFactory connectionFactory) =>
{
    try
    {
        using var connection = connectionFactory.CreateConnection();
        var count = CatalogHealthQuery.Execute(connection);
        return Results.Ok(new { Status = "healthy", ProductCount = count });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database connection failed: {ex.Message}");
    }
});

// Map catalog endpoints
app.MapCatalogEndpoints();

// Map auth endpoints
app.MapAuthEndpoints();

app.MapGet("/", () => "Ecommerce Modern API");

app.Run();
