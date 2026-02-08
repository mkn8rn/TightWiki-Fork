using Microsoft.AspNetCore.Http;

namespace TightWiki.Web.Bff.ViewModels.File
{
    /// <summary>Parameters for image/binary file retrieval.</summary>
    public class FileImageRequest
    {
        public required string GivenPageNavigation { get; init; }
        public required string GivenFileNavigation { get; init; }
        public int? FileRevision { get; init; }
        public int? Scale { get; init; }
        public int? MaxWidth { get; init; }
    }

    /// <summary>Parameters for binary file download.</summary>
    public class FileBinaryRequest
    {
        public required string GivenPageNavigation { get; init; }
        public required string GivenFileNavigation { get; init; }
        public int? FileRevision { get; init; }
    }

    /// <summary>Parameters for file revision listing.</summary>
    public class FileRevisionsRequest
    {
        public required string GivenPageNavigation { get; init; }
        public required string GivenFileNavigation { get; init; }
        public int Page { get; init; } = 1;
    }

    /// <summary>Parameters for drag-drop file upload.</summary>
    public class UploadDragDropRequest
    {
        public required string GivenPageNavigation { get; init; }
        public required List<IFormFile> PostedFiles { get; init; }
    }

    /// <summary>Parameters for manual single-file upload.</summary>
    public class ManualUploadRequest
    {
        public required string GivenPageNavigation { get; init; }
        public IFormFile? FileData { get; init; }
    }

    /// <summary>Parameters for detaching a file from a page revision.</summary>
    public class DetachFileRequest
    {
        public required string GivenPageNavigation { get; init; }
        public required string GivenFileNavigation { get; init; }
        public int PageRevision { get; init; }
    }

    /// <summary>Parameters for emoji image retrieval.</summary>
    public class EmojiImageRequest
    {
        public required string GivenEmojiNavigation { get; init; }
        public int Scale { get; init; } = 100;
    }
}
