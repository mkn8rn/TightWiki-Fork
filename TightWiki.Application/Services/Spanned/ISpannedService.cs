namespace BLL.Services.Spanned
{
    /// <summary>
    /// Service interface for cross-cutting database operations.
    /// </summary>
    public interface ISpannedService
    {
        /// <summary>
        /// Vacuum the database to reclaim storage.
        /// </summary>
        string VacuumDatabase(string databaseName);

        /// <summary>
        /// Optimize the database (ANALYZE in PostgreSQL).
        /// </summary>
        string OptimizeDatabase(string databaseName);

        /// <summary>
        /// Check database integrity.
        /// </summary>
        string IntegrityCheckDatabase(string databaseName);

        /// <summary>
        /// Check foreign key constraints (returns info message for PostgreSQL).
        /// </summary>
        string ForeignKeyCheck(string databaseName);

        /// <summary>
        /// Get database version information.
        /// </summary>
        List<(string Name, string Version)> GetDatabaseVersions();

        /// <summary>
        /// Get database page counts (PostgreSQL approximation).
        /// </summary>
        List<(string Name, int PageCount)> GetDatabasePageCounts();

        /// <summary>
        /// Get database page sizes.
        /// </summary>
        List<(string Name, int PageSize)> GetDatabasePageSizes();

        /// <summary>
        /// Get total database size in bytes.
        /// </summary>
        long GetDatabaseSize();
    }
}
