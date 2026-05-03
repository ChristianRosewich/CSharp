using Db.Core.Abstractions.Sql.Interfaaces;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime;

namespace Db.Provider.MySql
{
    /// <summary>
    /// Creates MySQL-specific ADO.NET objects for the neutral DB framework.
    /// </summary>
    public sealed class MySqlDbConnectionFactory : IDbConnectionFactory
    {
        public DbConnection CreateConnection(IDBSettings xSettings)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(xSettings);
#else
            if (xSettings == null) throw new ArgumentNullException(nameof(xSettings));
#endif

            var sConnectionString = new MySqlConnectionStringBuilder
            {
                Server = xSettings[nameof(MySqlConnectionStringBuilder.Server)].ToString(),
                Port = (uint?)xSettings[nameof(MySqlConnectionStringBuilder.Port)] ?? 3306,
                UserID = xSettings[nameof(MySqlConnectionStringBuilder.UserID)].ToString(),
                Password = xSettings[nameof(MySqlConnectionStringBuilder.Password)].ToString(),
                Database = xSettings[nameof(MySqlConnectionStringBuilder.Database)].ToString(),
                AllowUserVariables = true,
                ConvertZeroDateTime = true
            }.ConnectionString;

            return new MySqlConnection(sConnectionString);
        }

        public IDBSettings CreateSettingsStub()
        {
            return
                 (IDBSettings)(new Dictionary<string, object>()
                 {
                     [nameof(MySqlConnectionStringBuilder.Server)] = "",
                     [nameof(MySqlConnectionStringBuilder.Port)] = 3306,
                     [nameof(MySqlConnectionStringBuilder.UserID)] = "",
                     [nameof(MySqlConnectionStringBuilder.Password)] = "",
                     [nameof(MySqlConnectionStringBuilder.Database)] = ""
                 });
        }

        /// <inheritdoc />
        public IDbStatementRenderer CreateStatementRenderer()
        {
            return new MySqlStatementRenderer();
        }

        
    }
}
