using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using System.Runtime.Caching;
using TightWiki.Utils.Caching;
using TightWiki.Utils;
using TightWiki.Utils;

namespace BLL.Services.Emojis
{
    /// <summary>
    /// Business logic service for Emoji operations.
    /// Orchestrates data access, caching, and business rules.
    /// </summary>
    public sealed class EmojiService : IEmojiService
    {
        private readonly WikiDbContext _db;

        public EmojiService(WikiDbContext db)
        {
            _db = db;
        }

        public List<Emoji> GetAllEmojis()
        {
            return _db.Emojis.AsNoTracking()
                .OrderBy(e => e.Name)
                .Select(e => MapToDto(e))
                .ToList();
        }

        public List<Emoji> GetAllEmojisPaged(
            int pageNumber,
            string? orderBy = null,
            string? orderByDirection = null,
            List<string>? categories = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = Math.Max(0, pageNumber - 1) * pageSize;

            IQueryable<EmojiDB> query = _db.Emojis.AsNoTracking();

            // Filter by categories if specified
            if (categories is { Count: > 0 })
            {
                var emojiIds = SearchEmojiCategoryIds(categories);
                query = query.Where(e => emojiIds.Contains(e.Id));
            }

            // Get total count for pagination
            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Apply ordering
            query = ApplyOrderBy(query, orderBy, orderByDirection);

            // Execute query with pagination
            var emojis = query
                .Skip(skip)
                .Take(pageSize)
                .Select(e => MapToDto(e))
                .ToList();

            // Set pagination info on results
            foreach (var Emoji in emojis)
            {
                Emoji.PaginationPageCount = pageCount;
            }

            return emojis;
        }

        public Emoji? GetEmojiByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return _db.Emojis.AsNoTracking()
                .Where(e => e.Name == name)
                .Select(e => MapToDto(e))
                .SingleOrDefault();
        }

        public List<EmojiCategory> GetEmojiCategoriesByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return [];

            return _db.EmojiCategories.AsNoTracking()
                .Where(c => c.Category == name)
                .Select(c => new EmojiCategory { EmojiId = c.EmojiId, Category = c.Category })
                .ToList();
        }

        public int UpsertEmoji(UpsertEmoji Emoji)
        {
            if (Emoji == null)
                throw new ArgumentNullException(nameof(Emoji));

            using var transaction = _db.Database.BeginTransaction();

            try
            {
                var categories = (Emoji.Categories ?? [])
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => c.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var imageData = Emoji.ImageData == null ? null : Utility.Compress(Emoji.ImageData);

                EmojiDB entity;

                if (Emoji.Id is null or 0)
                {
                    // Create new Emoji
                    entity = new EmojiDB
                    {
                        Name = Emoji.Name,
                        Shortcut = Emoji.Name,
                        ImageData = imageData,
                        MimeType = Emoji.MimeType,
                        Categories = string.Join(';', categories),
                        PaginationPageCount = 0
                    };

                    _db.Emojis.Add(entity);
                    _db.SaveChanges();
                }
                else
                {
                    // Update existing Emoji
                    entity = _db.Emojis.Single(e => e.Id == Emoji.Id);
                    entity.Name = Emoji.Name;
                    entity.ImageData = imageData;
                    entity.MimeType = Emoji.MimeType;
                    entity.Categories = string.Join(';', categories);
                    _db.SaveChanges();
                }

                // Replace categories
                var existingCats = _db.EmojiCategories.Where(c => c.EmojiId == entity.Id);
                _db.EmojiCategories.RemoveRange(existingCats);
                _db.SaveChanges();

                foreach (var cat in categories)
                {
                    _db.EmojiCategories.Add(new EmojiCategoryDB { EmojiId = entity.Id, Category = cat });
                }

                _db.SaveChanges();
                transaction.Commit();

                // Reload Emoji cache after modification
                ReloadEmojisCache();

                return entity.Id;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        public void DeleteById(int id)
        {
            var entity = _db.Emojis.SingleOrDefault(e => e.Id == id);
            if (entity == null)
                return;

            _db.Emojis.Remove(entity);
            _db.SaveChanges();

            // Reload Emoji cache after deletion
            ReloadEmojisCache();
        }

        public bool EmojiNameExists(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return _db.Emojis.AsNoTracking()
                .Any(e => e.Name == name.ToLowerInvariant());
        }

        public IEnumerable<string> AutoCompleteEmoji(string term)
        {
            term ??= string.Empty;

            return _db.Emojis.AsNoTracking()
                .Where(e => e.Name.Contains(term))
                .OrderBy(e => e.Name)
                .Select(e => e.Shortcut)
                .Take(25)
                .ToList();
        }

        public IEnumerable<Emoji> GetEmojisByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return [];

            return _db.Emojis.AsNoTracking()
                .Where(e => _db.EmojiCategories.Any(c => c.EmojiId == e.Id && c.Category == category))
                .OrderBy(e => e.Name)
                .Select(e => MapToDto(e))
                .ToList();
        }

        public IEnumerable<EmojiCategory> GetEmojiCategoriesGrouped()
        {
            return _db.EmojiCategories.AsNoTracking()
                .GroupBy(c => c.Category)
                .Select(g => new EmojiCategory
                {
                    EmojiId = 0,
                    Category = g.Key,
                    EmojiCount = g.Count().ToString()
                })
                .OrderByDescending(x => x.EmojiCount)
                .ThenBy(x => x.Category)
                .ToList();
        }

        #region Private Methods

        private IEnumerable<int> SearchEmojiCategoryIds(List<string> categories)
        {
            if (categories == null || categories.Count == 0)
                return [];

            var normalized = categories
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return _db.EmojiCategories.AsNoTracking()
                .Where(ec => normalized.Contains(ec.Category))
                .Select(ec => ec.EmojiId)
                .Distinct()
                .ToList();
        }

        private static IQueryable<EmojiDB> ApplyOrderBy(IQueryable<EmojiDB> query, string? orderBy, string? orderByDirection)
        {
            var asc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "name" => asc ? query.OrderBy(e => e.Name) : query.OrderByDescending(e => e.Name),
                "id" => asc ? query.OrderBy(e => e.Id) : query.OrderByDescending(e => e.Id),
                _ => query.OrderBy(e => e.Name)
            };
        }

        private static Emoji MapToDto(EmojiDB e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            Shortcut = e.Shortcut,
            PaginationPageCount = e.PaginationPageCount,
            Categories = e.Categories,
            ImageData = e.ImageData,
            MimeType = e.MimeType,
        };

        #endregion

        #region Cache Management

        /// <inheritdoc />
        public void ReloadEmojisCache()
        {
            WikiCache.ClearCategory(WikiCache.Category.Emoji);
            GlobalConfiguration.Emojis = GetAllEmojis();

            if (GlobalConfiguration.PreLoadAnimatedEmojis)
            {
                // Pre-load animated emojis in background thread
                new Thread(() =>
                {
                    var parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount / 2 < 2 ? 2 : Environment.ProcessorCount / 2
                    };

                    Parallel.ForEach(GlobalConfiguration.Emojis, parallelOptions, Emoji =>
                    {
                        if (Emoji.MimeType.Equals("image/gif", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var imageCacheKey = WikiCacheKey.Build(WikiCache.Category.Emoji, [Emoji.Shortcut]);
                            Emoji.ImageData = GetEmojiByName(Emoji.Name)?.ImageData;

                            if (Emoji.ImageData != null)
                            {
                                var scaledImageCacheKey = WikiCacheKey.Build(WikiCache.Category.Emoji, [Emoji.Shortcut, "100"]);
                                var decompressedImageBytes = Utility.Decompress(Emoji.ImageData);
                                var img = Image.Load(new MemoryStream(decompressedImageBytes));

                                int customScalePercent = 100;

                                var (Width, Height) = Utility.ScaleToMaxOf(img.Width, img.Height, GlobalConfiguration.DefaultEmojiHeight);

                                Height = (int)(Height * (customScalePercent / 100.0));
                                Width = (int)(Width * (customScalePercent / 100.0));

                                if (Height < 16)
                                {
                                    Height += 16 - Height;
                                    Width += 16 - Height;
                                }
                                if (Width < 16)
                                {
                                    Height += 16 - Width;
                                    Width += 16 - Width;
                                }

                                var resized = Images.ResizeGifImage(decompressedImageBytes, Width, Height);
                                var itemCache = new ImageCacheItem(resized, "image/gif");
                                WikiCache.Put(scaledImageCacheKey, itemCache, new CacheItemPolicy());
                            }
                        }
                    });
                }).Start();
            }
        }

        #endregion
    }
}

