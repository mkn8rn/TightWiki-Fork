using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace TightWiki.API.Services
{
    public enum JobStatus
    {
        Pending,
        Completed,
        Failed
    }

    public class JobEntry
    {
        public required string JobId { get; init; }
        public JobStatus Status { get; set; } = JobStatus.Pending;
        public object? Result { get; set; }
        public int? StatusCode { get; set; }
        public string? Error { get; set; }
        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    }

    public class BackgroundJobService
    {
        private readonly ConcurrentDictionary<string, JobEntry> _jobs = new();
        private readonly IServiceScopeFactory _scopeFactory;

        public BackgroundJobService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Queues work on a background thread and returns a 202 Accepted result
        /// with the job ID and a polling location.
        /// </summary>
        public AcceptedResult Enqueue(IUrlHelper url, Func<IServiceProvider, object?> work)
        {
            var jobId = Guid.NewGuid().ToString("N");
            var entry = new JobEntry { JobId = jobId };
            _jobs[jobId] = entry;

            _ = Task.Run(() =>
            {
                using var scope = _scopeFactory.CreateScope();
                try
                {
                    var result = work(scope.ServiceProvider);
                    entry.Result = result;
                    entry.StatusCode = result is null ? 404 : 200;
                    entry.Status = JobStatus.Completed;
                }
                catch (Exception ex)
                {
                    entry.Error = ex.Message;
                    entry.StatusCode = 500;
                    entry.Status = JobStatus.Failed;
                }
            });

            var location = url.Action("Get", "Results", new { jobId })!;
            return new AcceptedResult(location, new { jobId, status = "pending", poll = location });
        }

        public JobEntry? TryGet(string jobId) =>
            _jobs.TryGetValue(jobId, out var entry) ? entry : null;
    }
}
