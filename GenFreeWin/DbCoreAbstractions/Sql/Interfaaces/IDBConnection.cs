using System;

namespace RnzTrauer.Core.Services.Interfaces;

/// <summary>
/// Represents an abstract database connection used by the repository layer.
/// </summary>
public interface IDBConnection : IDisposable, System.Data.IDbConnection
{
    
}
