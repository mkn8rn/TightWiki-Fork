using DAL;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TightWiki.Models;
using DalWikiExceptionEntity = DAL.Models.WikiExceptionEntity;
using ApiWikiException = TightWiki.Models.DataModels.WikiException;

namespace TightWiki.Repository
{
    public interface IExceptionRepository
    {
        void PurgeExceptions();
        void InsertException(string? text = null, string? exceptionText = null, string? stackTrace = null);
        int GetExceptionCount();
        List<ApiWikiException> GetAllExceptionsPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null);
        ApiWikiException GetExceptionById(int id);
    }

    public static class ExceptionRepository
    {
        private static IServiceProvider? _serviceProvider;
        public static void UseServiceProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        private static IExceptionRepository Repo =>
            _serviceProvider?.GetService(typeof(IExceptionRepository)) as IExceptionRepository
            ?? throw new InvalidOperationException("IExceptionRepository is not configured.");

        public static void PurgeExceptions()
            => Repo.PurgeExceptions();

        public static void InsertException(string? text = null, string? exceptionText = null, string? stackTrace = null)
            => Repo.InsertException(text, exceptionText, stackTrace);

        public static void InsertException(Exception ex)
        {
            var stackTrace = new StackTrace();
            var method = stackTrace.GetFrame(1)?.GetMethod();
            var message = string.IsNullOrEmpty(method?.Name) ? string.Empty : $"Error in {method?.Name}";

            Repo.InsertException(message, ex.Message, ex.StackTrace);
        }

        public static void InsertException(Exception ex, string? text = null)
            => Repo.InsertException(text, ex.Message, ex.StackTrace);

        public static int GetExceptionCount()
            => Repo.GetExceptionCount();

        public static List<ApiWikiException> GetAllExceptionsPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null)
            => Repo.GetAllExceptionsPaged(pageNumber, orderBy, orderByDirection);

        public static ApiWikiException GetExceptionById(int id)
            => Repo.GetExceptionById(id);
    }

    public sealed class ExceptionRepositoryEf : IExceptionRepository
    {
        public WikiDbContext Db { get; }

        public ExceptionRepositoryEf(WikiDbContext db)
        {
            Db = db;
        }

        public void PurgeExceptions()
        {
            Db.WikiExceptions.ExecuteDelete();
        }

        public void InsertException(string? text = null, string? exceptionText = null, string? stackTrace = null)
        {
            var entity = new DalWikiExceptionEntity
            {
                Text = text ?? string.Empty,
                ExceptionText = exceptionText ?? string.Empty,
                StackTrace = stackTrace ?? string.Empty,
                CreatedDate = DateTime.UtcNow
            };

            Db.WikiExceptions.Add(entity);
            Db.SaveChanges();
        }

        public int GetExceptionCount()
        {
            return Db.WikiExceptions.AsNoTracking().Count();
        }

        public List<ApiWikiException> GetAllExceptionsPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            var query = Db.WikiExceptions.AsNoTracking();

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            var isAsc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);
            query = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "id" => isAsc ? query.OrderBy(e => e.Id) : query.OrderByDescending(e => e.Id),
                "text" => isAsc ? query.OrderBy(e => e.Text) : query.OrderByDescending(e => e.Text),
                "exceptiontext" => isAsc ? query.OrderBy(e => e.ExceptionText) : query.OrderByDescending(e => e.ExceptionText),
                "createddate" => isAsc ? query.OrderBy(e => e.CreatedDate) : query.OrderByDescending(e => e.CreatedDate),
                _ => query.OrderByDescending(e => e.CreatedDate)
            };

            return query
                .Skip(skip)
                .Take(pageSize)
                .Select(e => new ApiWikiException
                {
                    Id = e.Id,
                    Text = e.Text,
                    ExceptionText = e.ExceptionText,
                    StackTrace = e.StackTrace,
                    CreatedDate = e.CreatedDate,
                    PaginationPageCount = pageCount
                })
                .ToList();
        }

        public ApiWikiException GetExceptionById(int id)
        {
            var entity = Db.WikiExceptions.AsNoTracking().Single(e => e.Id == id);
            return new ApiWikiException
            {
                Id = entity.Id,
                Text = entity.Text,
                ExceptionText = entity.ExceptionText,
                StackTrace = entity.StackTrace,
                CreatedDate = entity.CreatedDate
            };
        }
    }
}
