using System.Data;

namespace Ecommerce.Api.Data;

/// <summary>
/// Simple smoke test query to verify database connectivity.
/// </summary>
public static class CatalogHealthQuery
{
    public const string Sql = "SELECT COUNT(*) FROM Products";

    public static int Execute(IDbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = Sql;
        command.CommandType = CommandType.Text;
        return (int)command.ExecuteScalar()!;
    }
}
