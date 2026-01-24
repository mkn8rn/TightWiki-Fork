using DAL;
using Microsoft.EntityFrameworkCore;

namespace TightWiki.Repository
{
    public interface ISpannedRepository
    {
        string VacuumDatabase();
        string OptimizeDatabase();
        string IntegrityCheckDatabase();
        string GetDatabaseVersion();
        long GetDatabaseSize();
    }

    public static class SpannedRepository
    {
        private static IServiceProvider? _serviceProvider;
        public static void UseServiceProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        private static ISpannedRepository Repo =>
            _serviceProvider?.GetService(typeof(ISpannedRepository)) as ISpannedRepository
            ?? throw new InvalidOperationException("ISpannedRepository is not configured.");

        public static string VacuumDatabase(string databaseName)
            => Repo.VacuumDatabase();

        public static string OptimizeDatabase(string databaseName)
            => Repo.OptimizeDatabase();

        public static string IntegrityCheckDatabase(string databaseName)
            => Repo.IntegrityCheckDatabase();

        public static string ForeignKeyCheck(string databaseName)
            => "Foreign key validation not applicable for PostgreSQL - constraints are enforced automatically.";

        public static List<(string Name, string Version)> GetDatabaseVersions()
            => [("PostgreSQL", Repo.GetDatabaseVersion())];

        public static List<(string Name, int PageCount)> GetDatabasePageCounts()
            // PostgreSQL doesn't use the same page concept as SQLite
            => [("PostgreSQL", 0)];

        public static List<(string Name, int PageSize)> GetDatabasePageSizes()
            // PostgreSQL uses 8KB blocks by default
            => [("PostgreSQL", 8192)];

        public static long GetDatabaseSize()
            => Repo.GetDatabaseSize();
    }

    public sealed class SpannedRepositoryEf : ISpannedRepository
    {
        private readonly WikiDbContext _wikiDb;

        public SpannedRepositoryEf(WikiDbContext wikiDb)
        {
            _wikiDb = wikiDb;
        }

        public string VacuumDatabase()
        {
            try
            {
                // PostgreSQL VACUUM reclaims storage and updates statistics
                // Note: VACUUM FULL requires exclusive lock, regular VACUUM is usually sufficient
                _wikiDb.Database.ExecuteSqlRaw("VACUUM");
                return "VACUUM completed successfully.";
            }
            catch (Exception ex)
            {
                return $"VACUUM failed: {ex.Message}";
            }
        }

        public string OptimizeDatabase()
        {
            try
            {
                // PostgreSQL ANALYZE updates statistics for the query planner
                _wikiDb.Database.ExecuteSqlRaw("ANALYZE");
                return "ANALYZE completed successfully.";
            }
            catch (Exception ex)
            {
                return $"ANALYZE failed: {ex.Message}";
            }
        }

        public string IntegrityCheckDatabase()
        {
            try
            {
                // PostgreSQL doesn't have a simple integrity check like SQLite's PRAGMA integrity_check
                // We can check for invalid indexes using pg_catalog queries
                var result = new List<string> { "Database integrity check:" };

                // Check for invalid indexes
                var invalidIndexes = _wikiDb.Database
                    .SqlQueryRaw<string>(
                        @"SELECT indexrelid::regclass::text 
                          FROM pg_index 
                          WHERE NOT indisvalid")
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

                // Check connection is healthy
                result.Add("- Database connection: OK");

                return string.Join("\r\n", result);
            }
            catch (Exception ex)
            {
                return $"Integrity check failed: {ex.Message}";
            }
        }

        public string GetDatabaseVersion()
        {
            try
            {
                var version = _wikiDb.Database
                    .SqlQueryRaw<string>("SELECT version()")
                    .FirstOrDefault();

                return version ?? "Unknown";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public long GetDatabaseSize()
        {
            try
            {
                // Get the current database size in bytes
                var size = _wikiDb.Database
                    .SqlQueryRaw<long>("SELECT pg_database_size(current_database())")
                    .FirstOrDefault();

                return size;
            }
            catch
            {
                return 0;
            }
        }
    }
}

