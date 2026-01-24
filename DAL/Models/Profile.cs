namespace DAL.Models
{
    /// <summary>
    /// Entity model for the Profile table in the database.
    /// </summary>
    public class Profile
    {
        public Guid UserId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string? Biography { get; set; }
        public byte[]? Avatar { get; set; }
        public string? AvatarContentType { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
