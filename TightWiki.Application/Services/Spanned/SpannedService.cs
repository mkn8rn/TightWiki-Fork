using DAL;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Spanned
{
    /// <summary>
    /// Business logic service for cross-cutting database operations.
    /// Uses raw SQL for PostgreSQL-specific maintenance operations that cannot be expressed in LINQ.
    /// </summary>
    public sealed class SpannedService : ISpannedService
    {
        private readonly WikiDbContext _wikiDb;

        // PostgreSQL-specific SQL commands (no EF Core LINQ equivalent exists for these)
        private static class PostgresSql
        {
            // Maintenance commands
            public const string Vacuum = "VACUUM";
            public const string Analyze = "ANALYZE";

            // System functions (must alias as "Value" for EF Core scalar mapping)
            public const string GetVersion = """SELECT version() AS "Value" """;
            public const string GetDatabaseSize = """SELECT pg_database_size(current_database()) AS "Value" """;

            // System catalog queries
            public const string GetInvalidIndexes = """
                SELECT indexrelid::regclass::text AS "Value"
                FROM pg_index 
                WHERE NOT indisvalid
                """;
        }

        public SpannedService(WikiDbContext wikiDb)
        {
            _wikiDb = wikiDb;
        }

        public string VacuumDatabase(string databaseName)
        {
            try
            {
                _wikiDb.Database.ExecuteSqlRaw(PostgresSql.Vacuum);
                return "VACUUM completed successfully.";
            }
            catch (System.Exception ex)
            {
                return $"VACUUM failed: {ex.Message}";
            }
        }

        public string OptimizeDatabase(string databaseName)
        {
            try
            {
                _wikiDb.Database.ExecuteSqlRaw(PostgresSql.Analyze);
                return "ANALYZE completed successfully.";
            }
            catch (System.Exception ex)
            {
                return $"ANALYZE failed: {ex.Message}";
            }
        }

        public string IntegrityCheckDatabase(string databaseName)
        {
            try
            {
                var result = new List<string> { "Database integrity check:" };

                var invalidIndexes = _wikiDb.Database
                    .SqlQueryRaw<string>(PostgresSql.GetInvalidIndexes)
                    .ToList();

                if (invalidIndexes.Count == 0)
                {
                    result.Add("- All indexes are valid.");
                }
                else
                {
                    result.Add($"- Found {invalidIndexes.Count} invalid indexes:");
                    result.AddRange(invalidIndexes.Select(i => $"  - {i}"));
                }

                result.Add("- Database connection: OK");

                return string.Join("\r\n", result);
            }
            catch (System.Exception ex)
            {
                return $"Integrity check failed: {ex.Message}";
            }
        }

        public string ForeignKeyCheck(string databaseName)
            => "Foreign key validation not applicable for PostgreSQL - constraints are enforced automatically.";

        public List<(string Name, string Version)> GetDatabaseVersions()
        {
            try
            {
                var version = _wikiDb.Database
                    .SqlQueryRaw<string>(PostgresSql.GetVersion)
                    .FirstOrDefault() ?? "Unknown";

                return [("PostgreSQL", version)];
            }
            catch (System.Exception ex)
            {
                return [("PostgreSQL", $"Error: {ex.Message}")];
            }
        }

        public List<(string Name, int PageCount)> GetDatabasePageCounts()
            => [("PostgreSQL", 0)]; // PostgreSQL doesn't expose page counts like SQLite

        public List<(string Name, int PageSize)> GetDatabasePageSizes()
            => [("PostgreSQL", 8192)]; // PostgreSQL uses 8KB blocks by default

        public long GetDatabaseSize()
        {
            try
            {
                return _wikiDb.Database
                    .SqlQueryRaw<long>(PostgresSql.GetDatabaseSize)
                    .FirstOrDefault();
            }
            catch
            {
                return 0;
            }
        }
    }
}
