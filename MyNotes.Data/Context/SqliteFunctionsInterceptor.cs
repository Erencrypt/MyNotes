using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Globalization;

namespace MyNotes.Data.Context
{
    /// <summary>
    /// Registers custom SQL functions on every SQLite connection EF opens.
    /// </summary>
    internal sealed class SqliteFunctionsInterceptor : DbConnectionInterceptor
    {
        public static readonly SqliteFunctionsInterceptor Instance = new();

        /// <summary>SQL name of the function mapped to <see cref="AppDbContext.ContainsIgnoreCase"/>.</summary>
        public const string ContainsIgnoreCaseName = "contains_ci";

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
            => Register(connection);

        public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            Register(connection);
            return Task.CompletedTask;
        }

        private static void Register(DbConnection connection)
        {
            if (connection is not SqliteConnection sqlite)
            {
                return;
            }

            // SQLite's own case-insensitive matching only covers ASCII letters, so Turkish
            // characters (ç/Ç, ş/Ş, ğ/Ğ, ö/Ö, ü/Ü, ı/I, i/İ) are compared in .NET instead.
            sqlite.CreateFunction<string?, string?, bool>(
                ContainsIgnoreCaseName,
                (text, term) => text is not null && term is not null &&
                    AppDbContext.SearchCulture.CompareInfo.IndexOf(text, term, CompareOptions.IgnoreCase) >= 0,
                isDeterministic: true);
        }
    }
}
