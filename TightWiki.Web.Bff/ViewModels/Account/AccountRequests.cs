namespace TightWiki.Web.Bff.ViewModels.Account
{
    public class ExternalLoginRequest
    {
        public required string Provider { get; init; }
        public string? ReturnUrl { get; init; }
    }

    public class ExternalLoginCallbackRequest
    {
        public string? ReturnUrl { get; init; }
        public string? RemoteError { get; init; }
    }
}
