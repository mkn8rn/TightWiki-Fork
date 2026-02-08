using BLL.Services.PageFile;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TightWiki.API.Models;
using TightWiki.API.Services;

namespace TightWiki.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("fixed")]
    public class FilesController : ControllerBase
    {
        private readonly BackgroundJobService _jobs;

        public FilesController(BackgroundJobService jobs) => _jobs = jobs;

        [HttpGet("by-page/{pageNavigation}")]
        public AcceptedResult GetFilesByPage(string pageNavigation,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int? pageRevision = null)
            => _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IPageFileService>();
                var files = svc.GetPageFilesInfoByPageNavigationAndPageRevisionPaged(
                    pageNavigation, pageNumber, pageRevision: pageRevision);
                return new PaginatedResult<FileAttachmentDto>
                {
                    PageNumber = pageNumber,
                    PageCount = files.FirstOrDefault()?.PaginationPageCount ?? 0,
                    Items = files.Select(f => new FileAttachmentDto
                    {
                        Id = f.Id, PageId = f.PageId, Name = f.Name,
                        ContentType = f.ContentType, Size = f.Size,
                        FileNavigation = f.FileNavigation, PageNavigation = f.PageNavigation,
                        FileRevision = f.FileRevision, CreatedDate = f.CreatedDate,
                        CreatedByUserName = f.CreatedByUserName
                    }).ToList()
                };
            });

        [HttpGet("by-page/{pageNavigation}/{fileNavigation}/download")]
        public AcceptedResult DownloadFile(string pageNavigation, string fileNavigation,
            [FromQuery] int? fileRevision = null)
            => _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IPageFileService>();
                var attachment = svc.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(
                    pageNavigation, fileNavigation, fileRevision);
                if (attachment == null) return null;
                return new
                {
                    attachment.Name,
                    attachment.ContentType,
                    Data = Convert.ToBase64String(attachment.Data)
                };
            });
    }
}
