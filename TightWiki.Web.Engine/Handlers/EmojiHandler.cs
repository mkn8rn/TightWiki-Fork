using TightWiki.Web.Engine.Library;
using TightWiki.Web.Engine.Library.Interfaces;
using static TightWiki.Web.Engine.Library.Constants;

namespace TightWiki.Web.Engine.Handlers
{
    /// <summary>
    /// Handles wiki emojis.
    /// </summary>
    public class EmojiHandler : IEmojiHandler
    {
        /// <summary>
        /// Handles an emoji instruction.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="key">The lookup key for the given emoji.</param>
        /// <param name="scale">The desired 1-100 scale factor for the emoji.</param>
        public HandlerResult Handle(ITightEngineState state, string key, int scale)
        {
            var emoji = state.Config.Emojis.FirstOrDefault(o => o.Shortcut == key);

            if (state.Config.Emojis.Any(o => o.Shortcut == key))
            {
                if (scale != 100 && scale > 0 && scale <= 500)
                {
                    var emojiImage = $"<img src=\"{state.Config.BasePath}/file/Emoji/{key.Trim('%')}?Scale={scale}\" alt=\"{emoji?.Name}\" />";

                    return new HandlerResult(emojiImage);
                }
                else
                {
                    var emojiImage = $"<img src=\"{state.Config.BasePath}/file/Emoji/{key.Trim('%')}\" alt=\"{emoji?.Name}\" />";

                    return new HandlerResult(emojiImage);
                }
            }
            else
            {
                return new HandlerResult(key) { Instructions = [HandlerResultInstruction.DisallowNestedProcessing] };
            }
        }
    }
}
