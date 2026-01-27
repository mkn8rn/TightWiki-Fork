using TightWiki.Web.Engine.Library;
using TightWiki.Web.Engine.Library.Interfaces;
using static TightWiki.Web.Engine.Library.Constants;

namespace TightWiki.Web.Engine.Handlers
{
    /// <summary>
    /// Handles basic markup/style instructions like bole, italic, underline, etc.
    /// </summary>
    public class MarkupHandler : IMarkupHandler
    {
        /// <summary>
        /// Handles basic markup instructions like bole, italic, underline, etc.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="sequence">The sequence of symbols that were found to denotate this markup instruction,</param>
        /// <param name="scopeBody">The body of text to apply the style to.</param>
        public HandlerResult Handle(ITightEngineState state, char sequence, string scopeBody)
        {
            switch (sequence)
            {
                case '~': return new HandlerResult($"<strike>{scopeBody}</strike>");
                case '*': return new HandlerResult($"<strong>{scopeBody}</strong>");
                case '_': return new HandlerResult($"<u>{scopeBody}</u>");
                case '/': return new HandlerResult($"<i>{scopeBody}</i>");
                case '!': return new HandlerResult($"<mark>{scopeBody}</mark>");
                default:
                    break;
            }

            return new HandlerResult() { Instructions = [HandlerResultInstruction.Skip] };
        }
    }
}
