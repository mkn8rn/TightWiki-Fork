namespace TightWiki.Contracts.DataModels
{
    public class RolePermission
    {
        /// <summary>
        /// Id of the RolePermission table.
        /// </summary>
        public int Id { get; set; }
        public string Permission { get; set; } = string.Empty;
        public string PermissionDisposition { get; set; } = string.Empty;
        public string? ResourceName { get; set; }
        public string? Namespace { get; set; }
        public string? PageId { get; set; }
        public int PaginationPageSize { get; set; }
        public int PaginationPageCount { get; set; }
    }
}
