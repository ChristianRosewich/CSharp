using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Db.Core.Abstractions.Sql;
using Db.Core.Abstractions.Sql.Interfaaces;

namespace Db.Provider.MySql
{
    /// <summary>
    /// Renders abstract statements to MySQL SQL syntax.
    /// </summary>
    public sealed class MySqlStatementRenderer : IDbStatementRenderer
    {
        /// <inheritdoc />
        public string RenderSelect(IDbSelectStatement xStatement)
        {
            if (xStatement is null)
            {
                throw new ArgumentNullException(nameof(xStatement));
            }

            var sFields = xStatement.Fields.Count == 0 || (xStatement.Fields.Count == 1 && xStatement.Fields[0] == "*")  ? "*" : string.Join(",", xStatement.Fields.Select(QuoteIdentifier));
            var xBuilder = new StringBuilder($"SELECT {sFields} FROM {QuoteIdentifier(xStatement.Table)}");
            AppendFilters(xBuilder, xStatement.Filters);
            if (xStatement.Limit.HasValue)
            {
                xBuilder.Append($" limit {xStatement.Limit.Value}");
            }

            return xBuilder.ToString();
        }

        /// <inheritdoc />
        public string RenderInsert(IDbInsertStatement xStatement)
        {
            if (xStatement is null)
            {
                throw new ArgumentNullException(nameof(xStatement));
            }

            var sFields = string.Join(", ", xStatement.Fields.Select(xField => QuoteIdentifier(xField.Key)));
            var sValues = string.Join(", ", xStatement.Fields.Select(xField => xField.Value));
            return $"INSERT INTO {QuoteIdentifier(xStatement.Table)} ({sFields}) VALUES ({sValues});";
        }

        /// <inheritdoc />
        public string RenderUpdate(IDbUpdateStatement xStatement)
        {
            if (xStatement is null)
            {
                throw new ArgumentNullException(nameof(xStatement));
            }

            var sSet = string.Join(", ", xStatement.Fields.Select(xField => $"{QuoteIdentifier(xField.Key)}={xField.Value}"));
            var xBuilder = new StringBuilder($"UPDATE {QuoteIdentifier(xStatement.Table)} SET {sSet}");
            AppendFilters(xBuilder, xStatement.Filters);
            return xBuilder.ToString();
        }

        private static void AppendFilters(StringBuilder xBuilder, IReadOnlyList<IDbFilterClause> arrFilters)
        {
            if (arrFilters.Count == 0)
            {
                return;
            }

            xBuilder.Append(" WHERE ");
            xBuilder.Append(string.Join(" AND ", arrFilters.Select(RenderFilter)));
        }

        private static string RenderFilter(IDbFilterClause xClause)
        {
            return xClause.Operator switch
            {
                DbFilterOperator.Equal => $"{QuoteIdentifier(xClause.Field)}={xClause.ParameterName}",
                DbFilterOperator.IsNull => $"{QuoteIdentifier(xClause.Field)} is null",
                _ => throw new NotSupportedException($"Unsupported filter operator {xClause.Operator}.")
            };
        }

        private static string QuoteIdentifier(string sIdentifier)
        {
            var arrParts = sIdentifier.Split(new[] { "`.`", "." }, StringSplitOptions.None);
            return string.Join(".", arrParts.Select(sPart => $"`{sPart.Trim('`')}`"));
        }
    }
}
