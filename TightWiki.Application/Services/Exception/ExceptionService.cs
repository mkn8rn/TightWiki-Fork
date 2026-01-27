using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BLL.Services.Exception
{
    /// <summary>
    /// Business logic service for exception/error logging operations.
    /// </summary>
    public sealed class ExceptionService : IExceptionService
    {
        private readonly WikiDbContext _db;

        public ExceptionService(WikiDbContext db)
        {
            _db = db;
        }

        public void LogException(string? text = null, string? exceptionText = null, string? stackTrace = null)
        {
            var entity = new WikiExceptionEntityDB
            {
                Text = text ?? string.Empty,
                ExceptionText = exceptionText ?? string.Empty,
                StackTrace = stackTrace ?? string.Empty,
                CreatedDate = DateTime.UtcNow
            };

            _db.WikiExceptions.Add(entity);
            _db.SaveChanges();
        }

        public void LogException(System.Exception ex)
        {
            var stackTrace = new StackTrace();
            var method = stackTrace.GetFrame(1)?.GetMethod();
            var message = string.IsNullOrEmpty(method?.Name) ? string.Empty : $"Error in {method?.Name}";

            LogException(message, ex.Message, ex.StackTrace);
        }

        public void LogException(System.Exception ex, string? text)
        {
            LogException(text, ex.Message, ex.StackTrace);
        }

        public int GetExceptionCount()
        {
            return _db.WikiExceptions.AsNoTracking().Count();
        }

        public List<WikiException> GetAllExceptionsPaged(
            int pageNumber,
            string? orderBy = null,
            string? orderByDirection = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            var query = _db.WikiExceptions.AsNoTracking();

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
                .Select(e => MapToDto(e, pageCount))
                .ToList();
        }

        public WikiException GetExceptionById(int id)
        {
            var entity = _db.WikiExceptions.AsNoTracking().Single(e => e.Id == id);
            return MapToDto(entity);
        }

        public void PurgeExceptions()
        {
            _db.WikiExceptions.ExecuteDelete();
        }

        #region Private Methods

        private static WikiException MapToDto(WikiExceptionEntityDB e, int pageCount = 0) => new()
        {
            Id = e.Id,
            Text = e.Text,
            ExceptionText = e.ExceptionText,
            StackTrace = e.StackTrace,
            CreatedDate = e.CreatedDate,
            PaginationPageCount = pageCount
        };

        #endregion
    }
}

