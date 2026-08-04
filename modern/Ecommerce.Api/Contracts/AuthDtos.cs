namespace Ecommerce.Api.Contracts;

/// <summary>
/// Login request DTO.
/// </summary>
public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Login response DTO with JWT token and user info.
/// </summary>
public sealed class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = [];
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// User info DTO for /api/auth/me endpoint.
/// </summary>
public sealed class UserInfo
{
    public string Email { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = [];
}
