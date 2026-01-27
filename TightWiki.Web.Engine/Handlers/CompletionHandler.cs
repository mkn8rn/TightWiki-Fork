using TightWiki.Web.Engine.Library.Interfaces;

namespace TightWiki.Web.Engine.Handlers
{
    /// <summary>
    /// Handles wiki completion events.
    /// </summary>
    public class CompletionHandler : ICompletionHandler
    {
        /// <summary>
        /// Handles wiki completion events. Is called when the wiki processing completes for a given page.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        public void Complete(ITightEngineState state)
        {
            if (state.Config.RecordCompilationMetrics)
            {
                state.DataProvider.RecordCompilationStatistics(
                    state.Page.Id,
                    state.ProcessingTime.TotalMilliseconds,
                    state.MatchCount,
                    state.ErrorCount,
                    state.OutgoingLinks.Count,
                    state.Tags.Count,
                    state.HtmlResult.Length,
                    state.Page.Body.Length);
            }
        }
    }
}
