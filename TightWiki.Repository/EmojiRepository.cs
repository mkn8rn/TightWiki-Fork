using DAL;
using DalEmoji = DAL.Models.Emoji;
using DalEmojiCategory = DAL.Models.EmojiCategory;
using Microsoft.EntityFrameworkCore;
using TightWiki.Library;
using TightWiki.Models;
using ApiEmoji = TightWiki.Models.DataModels.Emoji;
using ApiEmojiCategory = TightWiki.Models.DataModels.EmojiCategory;
using ApiUpsertEmoji = TightWiki.Models.DataModels.UpsertEmoji;

namespace TightWiki.Repository
{
    public interface IEmojiRepository
    {
        List<ApiEmoji> GetAllEmojis();
        IEnumerable<string> AutoCompleteEmoji(string term);
        IEnumerable<ApiEmoji> GetEmojisByCategory(string category);
        IEnumerable<ApiEmojiCategory> GetEmojiCategoriesGrouped();
        IEnumerable<int> SearchEmojiCategoryIds(List<string> categories);
        List<ApiEmojiCategory> GetEmojiCategoriesByName(string name);
        void DeleteById(int id);
        ApiEmoji? GetEmojiByName(string name);
        int UpsertEmoji(ApiUpsertEmoji emoji);
        List<ApiEmoji> GetAllEmojisPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? categories = null);
    }

    public static partial class EmojiRepository
    {
        private static IServiceProvider? _serviceProvider;
        public static void UseServiceProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        private static IEmojiRepository Repo =>
            _serviceProvider?.GetService(typeof(IEmojiRepository)) as IEmojiRepository
            ?? throw new InvalidOperationException("IEmojiRepository is not configured.");

        public static List<ApiEmoji> GetAllEmojis() => Repo.GetAllEmojis();
        public static IEnumerable<string> AutoCompleteEmoji(string term) => Repo.AutoCompleteEmoji(term);
        public static IEnumerable<ApiEmoji> GetEmojisByCategory(string category) => Repo.GetEmojisByCategory(category);
        public static IEnumerable<ApiEmojiCategory> GetEmojiCategoriesGrouped() => Repo.GetEmojiCategoriesGrouped();
        public static IEnumerable<int> SearchEmojiCategoryIds(List<string> categories) => Repo.SearchEmojiCategoryIds(categories);
        public static List<ApiEmojiCategory> GetEmojiCategoriesByName(string name) => Repo.GetEmojiCategoriesByName(name);
        public static void DeleteById(int id) => Repo.DeleteById(id);
        public static ApiEmoji? GetEmojiByName(string name) => Repo.GetEmojiByName(name);
        public static int UpsertEmoji(ApiUpsertEmoji emoji) => Repo.UpsertEmoji(emoji);
        public static List<ApiEmoji> GetAllEmojisPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? categories = null)
            => Repo.GetAllEmojisPaged(pageNumber, orderBy, orderByDirection, categories);
    }

    public sealed class EmojiRepositoryEf : IEmojiRepository
    {
        public WikiDbContext Db { get; }

        public EmojiRepositoryEf(WikiDbContext db)
        {
            Db = db;
        }

        public List<ApiEmoji> GetAllEmojis()
            => Db.Emojis.AsNoTracking()
                .OrderBy(e => e.Name)
                .Select(MapEmoji)
                .ToList();

        public IEnumerable<string> AutoCompleteEmoji(string term)
        {
            term ??= string.Empty;

            return Db.Emojis.AsNoTracking()
                .Where(e => e.Name.Contains(term))
                .OrderBy(e => e.Name)
                .Select(e => e.Shortcut)
                .Take(25)
                .ToList();
        }

        public IEnumerable<ApiEmoji> GetEmojisByCategory(string category)
        {
            category ??= string.Empty;

            return Db.Emojis.AsNoTracking()
                .Where(e => Db.EmojiCategories.Any(c => c.EmojiId == e.Id && c.Category == category))
                .OrderBy(e => e.Name)
                .Select(MapEmoji)
                .ToList();
        }

        public IEnumerable<ApiEmojiCategory> GetEmojiCategoriesGrouped()
        {
            return Db.EmojiCategories.AsNoTracking()
                .GroupBy(c => c.Category)
                .Select(g => new ApiEmojiCategory
                {
                    EmojiId = 0,
                    Category = g.Key,
                    EmojiCount = g.Count().ToString()
                })
                .OrderByDescending(x => x.EmojiCount)
                .ThenBy(x => x.Category)
                .ToList();
        }

        public IEnumerable<int> SearchEmojiCategoryIds(List<string> categories)
        {
            if (categories == null || categories.Count == 0)
            {
                return [];
            }

            var normalized = categories
                .Where(c => string.IsNullOrWhiteSpace(c) == false)
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Db.EmojiCategories.AsNoTracking()
                .Where(ec => normalized.Contains(ec.Category))
                .Select(ec => ec.EmojiId)
                .Distinct()
                .ToList();
        }

        public List<ApiEmojiCategory> GetEmojiCategoriesByName(string name)
        {
            name ??= string.Empty;

            return Db.EmojiCategories.AsNoTracking()
                .Where(c => c.Category == name)
                .Select(c => new ApiEmojiCategory { EmojiId = c.EmojiId, Category = c.Category })
                .ToList();
        }

        public void DeleteById(int id)
        {
            var entity = Db.Emojis.SingleOrDefault(e => e.Id == id);
            if (entity == null)
            {
                return;
            }

            Db.Emojis.Remove(entity);
            Db.SaveChanges();

            ConfigurationRepository.ReloadEmojis();
        }

        public ApiEmoji? GetEmojiByName(string name)
        {
            name ??= string.Empty;
            return Db.Emojis.AsNoTracking()
                .Where(e => e.Name == name)
                .Select(MapEmoji)
                .SingleOrDefault();
        }

        public int UpsertEmoji(ApiUpsertEmoji emoji)
        {
            if (emoji == null)
            {
                throw new ArgumentNullException(nameof(emoji));
            }

            using var tx = Db.Database.BeginTransaction();

            try
            {
                var categories = (emoji.Categories ?? [])
                    .Where(c => string.IsNullOrWhiteSpace(c) == false)
                    .Select(c => c.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var imageData = emoji.ImageData == null ? null : Utility.Compress(emoji.ImageData);

                DalEmoji entity;

                if (emoji.Id is null or 0)
                {
                    entity = new DalEmoji
                    {
                        Name = emoji.Name,
                        Shortcut = emoji.Name, // preserved as best-effort until we verify legacy shortcut logic
                        ImageData = imageData,
                        MimeType = emoji.MimeType,
                        Categories = string.Join(';', categories),
                        PaginationPageCount = 0
                    };

                    Db.Emojis.Add(entity);
                    Db.SaveChanges();
                }
                else
                {
                    entity = Db.Emojis.Single(e => e.Id == emoji.Id);
                    entity.Name = emoji.Name;
                    entity.ImageData = imageData;
                    entity.MimeType = emoji.MimeType;
                    entity.Categories = string.Join(';', categories);
                    Db.SaveChanges();
                }

                var existingCats = Db.EmojiCategories.Where(c => c.EmojiId == entity.Id);
                Db.EmojiCategories.RemoveRange(existingCats);
                Db.SaveChanges();

                foreach (var cat in categories)
                {
                    Db.EmojiCategories.Add(new DalEmojiCategory { EmojiId = entity.Id, Category = cat });
                }

                Db.SaveChanges();
                tx.Commit();

                ConfigurationRepository.ReloadEmojis();

                return entity.Id;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public List<ApiEmoji> GetAllEmojisPaged(int pageNumber, string? orderBy, string? orderByDirection, List<string>? categories)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = Math.Max(0, pageNumber - 1) * pageSize;

            IQueryable<DalEmoji> query = Db.Emojis.AsNoTracking();

            if (categories is { Count: > 0 })
            {
                var emojiIds = this.SearchEmojiCategoryIds(categories);
                query = query.Where(e => emojiIds.Contains(e.Id));
            }

            query = ApplyOrderBy(query, orderBy, orderByDirection);

            return query
                .Skip(skip)
                .Take(pageSize)
                .Select(MapEmoji)
                .ToList();
        }

        private static IQueryable<DalEmoji> ApplyOrderBy(IQueryable<DalEmoji> query, string? orderBy, string? orderByDirection)
        {
            var asc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "name" => asc ? query.OrderBy(e => e.Name) : query.OrderByDescending(e => e.Name),
                "id" => asc ? query.OrderBy(e => e.Id) : query.OrderByDescending(e => e.Id),
                _ => query.OrderBy(e => e.Name)
            };
        }

        private static ApiEmoji MapEmoji(DalEmoji e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            Shortcut = e.Shortcut,
            PaginationPageCount = e.PaginationPageCount,
            Categories = e.Categories,
            ImageData = e.ImageData,
            MimeType = e.MimeType,
        };
    }
}
