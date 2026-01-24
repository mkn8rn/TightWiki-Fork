namespace DAL.Models
{
    /// <summary>
    /// Entity model for the AccountPermission table in the database.
    /// This is the EF entity that maps to the table schema.
    /// </summary>
    public class AccountPermissionEntity
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int PermissionId { get; set; }
        public int PermissionDispositionId { get; set; }
        public string? Namespace { get; set; }
        public string? PageId { get; set; }

        // Navigation properties
        public Permission? Permission { get; set; }
        public PermissionDisposition? PermissionDisposition { get; set; }
    }
}
