using TightWiki.Contracts.DataModels;

namespace BLL.Services.Exception
{
    /// <summary>
    /// Service interface for exception/error logging operations.
    /// </summary>
    public interface IExceptionService
    {
        /// <summary>
        /// Logs an exception with optional context.
        /// </summary>
        void LogException(string? text = null, string? exceptionText = null, string? stackTrace = null);

        /// <summary>
        /// Logs an Exception object with automatic stack trace capture.
        /// </summary>
        void LogException(System.Exception ex);

        /// <summary>
        /// Logs an Exception object with additional context text.
        /// </summary>
        void LogException(System.Exception ex, string? text);

        /// <summary>
        /// Gets the total count of logged exceptions.
        /// </summary>
        int GetExceptionCount();

        /// <summary>
        /// Gets exceptions with pagination and sorting.
        /// </summary>
        List<WikiException> GetAllExceptionsPaged(
            int pageNumber,
            string? orderBy = null,
            string? orderByDirection = null);

        /// <summary>
        /// Gets a single exception by ID.
        /// </summary>
        WikiException GetExceptionById(int id);

        /// <summary>
        /// Purges all logged exceptions.
        /// </summary>
        void PurgeExceptions();
    }
}
