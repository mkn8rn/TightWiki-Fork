using Microsoft.AspNetCore.Mvc;
using TightWiki.Web.Bff.ViewModels.Profile;

namespace TightWiki.Web.Bff.Interfaces
{
    public interface IProfileBffService
    {
        IActionResult GetAvatar(GetAvatarRequest request);
        PublicViewModel GetPublicViewModel(string userAccountName);
        AccountProfileViewModel GetMyProfileViewModel();
        IActionResult SaveMyProfile(SaveMyProfileRequest request);
        IActionResult DeleteMyAccount(DeleteMyAccountRequest request);
    }
}
