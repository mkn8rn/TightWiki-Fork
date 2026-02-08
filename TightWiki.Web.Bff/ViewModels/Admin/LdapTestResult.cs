namespace TightWiki.Web.Bff.ViewModels.Admin
{
    public class LdapTestResult
    {
        public bool Ok { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
        public string? DistinguishedName { get; set; }
    }
}
