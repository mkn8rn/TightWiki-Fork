using Microsoft.AspNetCore.Mvc;
using TightWiki.API.Services;

namespace TightWiki.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResultsController : ControllerBase
    {
        private readonly BackgroundJobService _jobService;

        public ResultsController(BackgroundJobService jobService)
        {
            _jobService = jobService;
        }

        /// <summary>
        /// Poll for the result of an async job.
        /// Returns 202 if still processing, 200 with payload when done, or 404/500 on error.
        /// </summary>
        [HttpGet("{jobId}")]
        public ActionResult Get(string jobId)
        {
            var entry = _jobService.TryGet(jobId);
            if (entry is null)
                return NotFound(new { error = "Job not found." });

            if (entry.Status == JobStatus.Pending)
                return Accepted(new { jobId, status = "pending" });

            if (entry.Status == JobStatus.Failed)
                return StatusCode(500, new { jobId, status = "failed", error = entry.Error });

            if (entry.StatusCode == 404)
                return NotFound(new { jobId, status = "completed", result = (object?)null });

            return Ok(new { jobId, status = "completed", result = entry.Result });
        }
    }
}
