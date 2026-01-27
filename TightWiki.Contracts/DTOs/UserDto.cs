namespace TightWiki.Contracts.DTOs
{
    /// <summary>
    /// User profile data.
    /// </summary>
    public class UserDto
    {
        public Guid UserId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Biography { get; set; }
        public string TimeZone { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string? Theme { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
