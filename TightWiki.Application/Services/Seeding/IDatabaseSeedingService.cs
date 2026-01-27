namespace BLL.Services.Seeding
{
    /// <summary>
    /// Service interface for database seeding operations.
    /// </summary>
    public interface IDatabaseSeedingService
    {
        /// <summary>
        /// Seeds the database with required initial data if tables are empty.
        /// </summary>
        void SeedDatabase();

        /// <summary>
        /// Checks if the database has been seeded with initial data.
        /// </summary>
        bool IsDatabaseSeeded();
    }
}
