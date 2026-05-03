using System.Data.Common;

namespace Db.Core.Abstractions.Sql.Interfaaces;

/// <summary>
/// Creates provider-specific database services for the repository layer.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates a database connection for the given settings.
    /// </summary>
    DbConnection CreateConnection(IDBSettings xSettings);

    /// <summary>
    /// Creates the SQL statement renderer for the current provider.
    /// </summary>
    IDbStatementRenderer CreateStatementRenderer();

    IDBSettings CreateSettingsStub();
}
