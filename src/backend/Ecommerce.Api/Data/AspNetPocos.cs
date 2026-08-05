namespace Ecommerce.Api.Data;

/// <summary>
/// POCO for AspNetUsers table - matches Identity schema exactly.
/// No EF attributes, just column mappings for SQL queries.
/// </summary>
public sealed class AspNetUser
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PasswordHash { get; set; }
    public string? SecurityStamp { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTime? LockoutEndDateUtc { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }
    public string UserName { get; set; } = string.Empty;
}
