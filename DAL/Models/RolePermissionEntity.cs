namespace DAL.Models
{
    /// <summary>
    /// Entity model for the RolePermission table in the database.
    /// This is the EF entity that maps to the table schema.
    /// </summary>
    public class RolePermissionEntity
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        public int PermissionDispositionId { get; set; }
        public string? Namespace { get; set; }
        public string? PageId { get; set; }

        // Navigation properties
        public Role? Role { get; set; }
        public Permission? Permission { get; set; }
        public PermissionDisposition? PermissionDisposition { get; set; }
    }
}
