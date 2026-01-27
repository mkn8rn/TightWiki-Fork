namespace DAL.Models
{
    /// <summary>
    /// Entity model for the AccountRole junction table (user-role membership).
    /// </summary>
    public class AccountRoleDB
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int RoleId { get; set; }

        // Navigation properties
        public RoleDB? Role { get; set; }
    }
}
