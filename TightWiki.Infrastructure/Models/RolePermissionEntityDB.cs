namespace DAL.Models
{
    /// <summary>
    /// Entity model for the RolePermission table in the database.
    /// This is the EF entity that maps to the table schema.
    /// </summary>
    public class RolePermissionEntityDB
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        public int PermissionDispositionId { get; set; }
        public string? Namespace { get; set; }
        public string? PageId { get; set; }

        // Navigation properties
        public RoleDB? Role { get; set; }
        public PermissionDB? Permission { get; set; }
        public PermissionDispositionDB? PermissionDisposition { get; set; }
    }
}
