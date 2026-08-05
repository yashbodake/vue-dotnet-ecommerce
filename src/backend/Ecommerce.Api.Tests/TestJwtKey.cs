namespace Ecommerce.Api.Tests;

/// <summary>
/// Test-only JWT signing key used by unit tests that construct AuthService directly.
/// This value is intentionally NOT a production secret and is only compiled into the test assembly.
/// </summary>
internal static class TestJwtKey
{
    public const string Value = "TestOnlySigningKey-32BytesOrMoreForUnitTests!";
}
