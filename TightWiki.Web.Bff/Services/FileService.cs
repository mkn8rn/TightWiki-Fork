using BLL.Services.Emojis;
using BLL.Services.Pages;
using BLL.Services.PageFile;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NTDLS.Helpers;
using SixLabors.ImageSharp;
using System.Web;
using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;
using TightWiki.Contracts.Interfaces;
using TightWiki.Utils;
using TightWiki.Utils.Caching;
using TightWiki.Web.Bff.Extensions;
using TightWiki.Web.Bff.Interfaces;
using TightWiki.Web.Bff.ViewModels.File;
using static TightWiki.Contracts.Constants;
using static TightWiki.Utils.Images;

namespace TightWiki.Web.Bff.Services
{
    public class FileService(
        IEmojiService emojiService,
        IPageService pageService,
        IPageFileService pageFileService,
        ISessionState session,
        IStringLocalizer<FileService> localizer)
        : IFileBffService
    {
        public IActionResult GetImage(FileImageRequest request)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page,
                [request.GivenPageNavigation, request.GivenFileNavigation, request.FileRevision, request.Scale, request.MaxWidth]);

            if (WikiCache.TryGet<ImageCacheItem>(cacheKey, out var cached))
                return new FileContentResult(cached.Bytes, cached.ContentType);

            var pageNavigation = new NamespaceNavigation(request.GivenPageNavigation);
            var fileNavigation = new NamespaceNavigation(request.GivenFileNavigation);

            var file = pageFileService.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(
                pageNavigation.Canonical, fileNavigation.Canonical, request.FileRevision)
                ?? throw new FileNotFoundException(
                    localizer.Localize("[{0}] was not found on the page [{1}].", fileNavigation, pageNavigation));

            return ServeImage(file.Data, file.ContentType, request.Scale, request.MaxWidth, cacheKey, convertToPng: false);
        }

        public IActionResult GetPng(FileImageRequest request)
        {
            session.RequirePermission(request.GivenPageNavigation, WikiPermission.Read);

            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page,
                [request.GivenPageNavigation, request.GivenFileNavigation, request.FileRevision, request.Scale, request.MaxWidth]);

            if (WikiCache.TryGet<ImageCacheItem>(cacheKey, out var cached))
                return new FileContentResult(cached.Bytes, cached.ContentType);

            var pageNavigation = new NamespaceNavigation(request.GivenPageNavigation);
            var fileNavigation = new NamespaceNavigation(request.GivenFileNavigation);

            var file = pageFileService.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(
                pageNavigation.Canonical, fileNavigation.Canonical, request.FileRevision)
                ?? throw new FileNotFoundException(
                    localizer.Localize("[{0}] was not found on the page [{1}].", fileNavigation, pageNavigation));

            return ServeImage(Utility.Decompress(file.Data), file.ContentType, request.Scale, request.MaxWidth, cacheKey, convertToPng: true);
        }

        public IActionResult GetBinary(FileBinaryRequest request)
        {
            session.RequirePermission(request.GivenPageNavigation, WikiPermission.Read);

            var pageNavigation = new NamespaceNavigation(request.GivenPageNavigation);
            var fileNavigation = new NamespaceNavigation(request.GivenFileNavigation);

            var file = pageFileService.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(
                pageNavigation.Canonical, fileNavigation.Canonical, request.FileRevision)
                ?? throw new FileNotFoundException(
                    localizer.Localize("[{0}] was not found on the page [{1}].", fileNavigation, pageNavigation));

            return new FileContentResult(file.Data.ToArray(), file.ContentType);
        }

        public PageFileRevisionsViewModel GetFileRevisionsViewModel(FileRevisionsRequest request)
        {
            session.RequirePermission(request.GivenPageNavigation, WikiPermission.Read);

            var pageNavigation = new NamespaceNavigation(request.GivenPageNavigation).Canonical;
            var fileNavigation = new NamespaceNavigation(request.GivenFileNavigation).Canonical;

            var revisions = pageFileService.GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(
                pageNavigation, fileNavigation, request.Page);

            return new PageFileRevisionsViewModel
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                Revisions = revisions,
                PaginationPageCount = revisions.FirstOrDefault()?.PaginationPageCount ?? 0
            };
        }

        public FileAttachmentViewModel GetPageAttachmentsViewModel(string givenPageNavigation)
        {
            session.RequirePermission(givenPageNavigation, WikiPermission.Read);

            var page = pageService.GetPageRevisionByNavigation(new NamespaceNavigation(givenPageNavigation));
            if (page != null)
            {
                return new FileAttachmentViewModel
                {
                    PageNavigation = page.Navigation,
                    PageRevision = page.Revision,
                    Files = pageFileService.GetPageFilesInfoByPageId(page.Id)
                };
            }

            return new FileAttachmentViewModel { Files = new() };
        }

        public IActionResult UploadDragDrop(UploadDragDropRequest request)
        {
            session.RequirePermission(request.GivenPageNavigation, [WikiPermission.Create, WikiPermission.Edit]);

            var userId = (session.Profile?.UserId).EnsureNotNullOrEmpty();

            foreach (var file in request.PostedFiles)
            {
                if (file != null && file.Length > 0)
                {
                    if (file.Length > GlobalConfiguration.MaxAttachmentFileSize)
                        return new JsonResult(new { message = localizer.Localize("Could not attach file: [{0}], too large.", file.FileName) });

                    var fileName = HttpUtility.UrlDecode(file.FileName);
                    UpsertPageFileInternal(request.GivenPageNavigation, fileName, ConvertHttpFileToBytes(file), file.Length, userId);
                }
            }

            return new JsonResult(new { success = true, message = localizer.Localize("files: {0:n0}", request.PostedFiles.Count) });
        }

        public IActionResult ManualUpload(ManualUploadRequest request)
        {
            session.RequirePermission(request.GivenPageNavigation, [WikiPermission.Create, WikiPermission.Edit]);

            if (request.FileData != null && request.FileData.Length > 0)
            {
                if (request.FileData.Length > GlobalConfiguration.MaxAttachmentFileSize)
                    return new ContentResult { Content = localizer.Localize("Could not save the attached file, too large") };

                var fileName = HttpUtility.UrlDecode(request.FileData.FileName);
                UpsertPageFileInternal(request.GivenPageNavigation, fileName,
                    ConvertHttpFileToBytes(request.FileData), request.FileData.Length,
                    (session.Profile?.UserId).EnsureNotNullOrEmpty());

                return new ContentResult { Content = localizer.Localize("Success") };
            }

            return new ContentResult { Content = localizer.Localize("Failure") };
        }

        public IActionResult Detach(DetachFileRequest request)
        {
            session.RequirePermission(request.GivenPageNavigation, WikiPermission.Delete);

            pageFileService.DetachPageRevisionAttachment(
                new NamespaceNavigation(request.GivenPageNavigation).Canonical,
                new NamespaceNavigation(request.GivenFileNavigation).Canonical,
                request.PageRevision);

            return new ContentResult { Content = localizer.Localize("Success") };
        }

        public IActionResult AutoCompleteEmoji(string query)
        {
            var results = emojiService.AutoCompleteEmoji(query)
                .Select(o => new { text = o }).ToList();
            return new JsonResult(results);
        }

        public IActionResult GetEmoji(EmojiImageRequest request)
        {
            var emojiNavigation = Navigation.Clean(request.GivenEmojiNavigation);
            if (string.IsNullOrEmpty(emojiNavigation))
                throw new FileNotFoundException(localizer.Localize("Emoji {0} was not found", request.GivenEmojiNavigation));

            string shortcut = $"%%{emojiNavigation.ToLowerInvariant()}%%";

            var emoji = GlobalConfiguration.Emojis.FirstOrDefault(o => o.Shortcut == shortcut)
                ?? throw new FileNotFoundException(localizer.Localize("Emoji {0} was not found", emojiNavigation));

            var givenScale = request.Scale;

            var scaledImageCacheKey = WikiCacheKey.Build(WikiCache.Category.Emoji, [shortcut, givenScale]);
            if (WikiCache.TryGet<ImageCacheItem>(scaledImageCacheKey, out var cachedEmoji))
                return new FileContentResult(cachedEmoji.Bytes, cachedEmoji.ContentType);

            var imageCacheKey = WikiCacheKey.Build(WikiCache.Category.Emoji, [shortcut]);
            emoji.ImageData = WikiCache.Get<byte[]>(imageCacheKey);
            if (emoji.ImageData == null)
            {
                emoji.ImageData = emojiService.GetEmojiByName(emoji.Name)?.ImageData
                    ?? throw new FileNotFoundException(localizer.Localize("Emoji {0} was not found", emojiNavigation));

                WikiCache.Put(imageCacheKey, emoji.ImageData);
            }

            var decompressedImageBytes = Utility.Decompress(emoji.ImageData);
            var img = SixLabors.ImageSharp.Image.Load(new MemoryStream(decompressedImageBytes));

            if (givenScale > 500) givenScale = 500;

            var (Width, Height) = Utility.ScaleToMaxOf(img.Width, img.Height, GlobalConfiguration.DefaultEmojiHeight);
            Height = (int)(Height * (givenScale / 100.0));
            Width = (int)(Width * (givenScale / 100.0));
            EnsureMinDimensions(ref Width, ref Height);

            if (emoji.MimeType?.ToLowerInvariant() == "image/gif")
            {
                var resized = ResizeGifImage(decompressedImageBytes, Width, Height);
                var itemCache = new ImageCacheItem(resized, "image/gif");
                WikiCache.Put(scaledImageCacheKey, itemCache);
                return new FileContentResult(itemCache.Bytes, itemCache.ContentType);
            }
            else
            {
                using var image = Images.ResizeImage(img, Width, Height);
                using var ms = new MemoryStream();
                image.SaveAsPng(ms);

                var itemCache = new ImageCacheItem(ms.ToArray(), "image/png");
                WikiCache.Put(scaledImageCacheKey, itemCache);
                return new FileContentResult(itemCache.Bytes, itemCache.ContentType);
            }
        }

        #region Private Helpers

        private void UpsertPageFileInternal(string pageNavigation, string fileName, byte[] data, long fileSize, Guid userId)
        {
            var page = pageService.GetPageInfoByNavigation(new NamespaceNavigation(pageNavigation).Canonical)
                ?? throw new InvalidOperationException($"Page not found: {pageNavigation}");

            pageFileService.UpsertPageFile(new PageFileAttachment
            {
                Data = data,
                CreatedDate = DateTime.UtcNow,
                PageId = page.Id,
                Name = fileName,
                FileNavigation = Navigation.Clean(fileName),
                Size = fileSize,
                ContentType = Utility.GetMimeType(fileName)
            }, userId);
        }

        private static IActionResult ServeImage(byte[] data, string contentType, int? scale, int? maxWidth, WikiCacheKeyFunction cacheKey, bool convertToPng)
        {
            if (contentType == "image/x-icon" && !convertToPng)
                return new FileContentResult(data, contentType);

            var img = SixLabors.ImageSharp.Image.Load(new MemoryStream(data));

            if (scale > 500) scale = 500;

            if (scale != null && scale != 100)
            {
                int width = (int)(img.Width * (scale / 100.0));
                int height = (int)(img.Height * (scale / 100.0));
                EnsureMinDimensions(ref width, ref height);

                if (!convertToPng && contentType.Equals("image/gif", StringComparison.InvariantCultureIgnoreCase))
                    return new FileContentResult(ResizeGifImage(data, width, height), "image/gif");

                return SaveAndCache(img, width, height, contentType, cacheKey, convertToPng);
            }

            if (maxWidth > 0 && img.Width > maxWidth)
            {
                double widthScale = (double)maxWidth / img.Width;
                int width = Math.Max(1, (int)Math.Round(img.Width * widthScale));
                int height = Math.Max(1, (int)Math.Round(img.Height * widthScale));

                return SaveAndCache(img, width, height, contentType, cacheKey, convertToPng);
            }

            if (convertToPng)
            {
                using var ms = new MemoryStream();
                img.SaveAsPng(ms);
                var cacheItem = new ImageCacheItem(ms.ToArray(), "image/png");
                WikiCache.Put(cacheKey, cacheItem);
                return new FileContentResult(cacheItem.Bytes, cacheItem.ContentType);
            }

            return new FileContentResult(data, contentType);
        }

        private static IActionResult SaveAndCache(SixLabors.ImageSharp.Image img, int width, int height, string contentType, WikiCacheKeyFunction cacheKey, bool convertToPng)
        {
            using var image = ResizeImage(img, width, height);
            using var ms = new MemoryStream();

            if (convertToPng)
            {
                image.SaveAsPng(ms);
                contentType = "image/png";
            }
            else
            {
                contentType = BestEffortConvertImage(image, ms, contentType);
            }

            var cacheItem = new ImageCacheItem(ms.ToArray(), contentType);
            WikiCache.Put(cacheKey, cacheItem);
            return new FileContentResult(cacheItem.Bytes, cacheItem.ContentType);
        }

        private static void EnsureMinDimensions(ref int width, ref int height)
        {
            if (height < 16) { int d = 16 - height; height += d; width += d; }
            if (width < 16) { int d = 16 - width; height += d; width += d; }
        }

        private static byte[] ConvertHttpFileToBytes(Microsoft.AspNetCore.Http.IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var reader = new BinaryReader(stream);
            return reader.ReadBytes((int)file.Length);
        }

        #endregion
    }
}
