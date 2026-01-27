using TightWiki.Web.Engine.Library;
using TightWiki.Web.Engine.Library.Interfaces;
using static TightWiki.Web.Engine.Library.Constants;

namespace TightWiki.Web.Engine.Handlers
{
    /// <summary>
    /// Handles wiki comments. These are generally removed from the result.
    /// </summary>
    public class CommentHandler : ICommentHandler
    {
        /// <summary>
        /// Handles a wiki comment.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="text">The comment text</param>
        public HandlerResult Handle(ITightEngineState state, string text)
        {
            return new HandlerResult() { Instructions = [HandlerResultInstruction.TruncateTrailingLine] };
        }
    }
}
