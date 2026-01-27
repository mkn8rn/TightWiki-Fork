using TightWiki.Contracts.Interfaces;

namespace TightWiki.Contracts.DataModels
{
    /// <summary>
    /// User profile data for public profile listings.
    /// </summary>
    public class EngineAccountProfile : IAccountProfile
    {
        public Guid UserId { get; set; }
        public string EmailAddress { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string TimeZone { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string? Theme { get; set; }
        public string? Biography { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int PaginationPageCount { get; set; }
        public int PaginationPageSize { get; set; }
    }
}
