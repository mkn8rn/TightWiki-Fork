using TightWiki.Contracts;
using TightWiki.Contracts.Interfaces;
using static TightWiki.Web.Engine.Library.Constants;

namespace TightWiki.Web.Engine.Library.Interfaces
{

    public interface ITightEngine
    {
        IScopeFunctionHandler ScopeFunctionHandler { get; }
        IStandardFunctionHandler StandardFunctionHandler { get; }
        IProcessingInstructionFunctionHandler ProcessingInstructionFunctionHandler { get; }
        IPostProcessingFunctionHandler PostProcessingFunctionHandler { get; }
        IMarkupHandler MarkupHandler { get; }
        IHeadingHandler HeadingHandler { get; }
        ICommentHandler CommentHandler { get; }
        IEmojiHandler EmojiHandler { get; }
        IExternalLinkHandler ExternalLinkHandler { get; }
        IInternalLinkHandler InternalLinkHandler { get; }
        IExceptionHandler ExceptionHandler { get; }
        ICompletionHandler CompletionHandler { get; }

        /// <summary>
        /// Transforms wiki markup to HTML.
        /// </summary>
        /// <param name="config">Engine configuration (BasePath, Emojis, etc.)</param>
        /// <param name="dataProvider">Data provider for accessing pages, users, files, and configuration</param>
        /// <param name="sessionState">User session state for localization</param>
        /// <param name="page">The page to transform</param>
        /// <param name="revision">Optional specific revision</param>
        /// <param name="omitMatches">Match types to omit from processing</param>
        ITightEngineState Transform(IEngineConfiguration config, IEngineDataProvider dataProvider, ISessionState? sessionState, IPage page, int? revision = null, WikiMatchType[]? omitMatches = null);
    }
}
