namespace Db.Core.Abstractions.Sql.Interfaaces;

/// <summary>
/// Renders abstract database statements to provider-specific SQL text.
/// </summary>
public interface IDbStatementRenderer
{
    /// <summary>
    /// Renders a select statement.
    /// </summary>
    string RenderSelect(IDbSelectStatement xStatement);

    /// <summary>
    /// Renders an insert statement.
    /// </summary>
    string RenderInsert(IDbInsertStatement xStatement);

    /// <summary>
    /// Renders an update statement.
    /// </summary>
    string RenderUpdate(IDbUpdateStatement xStatement);
}
