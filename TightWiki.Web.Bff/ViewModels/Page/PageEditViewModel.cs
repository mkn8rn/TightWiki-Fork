using System.ComponentModel.DataAnnotations;
using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.Page
{
    public class PageEditViewModel : ViewModelBase
    {
        public int Id { get; set; }

        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(TightWiki.Localisation.Resources.ValTexts))]
        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public string? ChangeSummary { get; set; } = string.Empty;
        public string? Body { get; set; } = string.Empty;
        public List<TightWiki.Contracts.DataModels.Page> Templates { get; set; } = new();
        public List<FeatureTemplate> FeatureTemplates { get; set; } = new();
    }
}
