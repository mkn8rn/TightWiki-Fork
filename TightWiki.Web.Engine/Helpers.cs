using DuoVia.FuzzyStrings;
using NTDLS.Helpers;
using TightWiki.Utils.Caching;
using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;
using TightWiki.Contracts.Interfaces;
using TightWiki.Web.Engine.Library.Interfaces;
using TightWiki.Utils;
using static TightWiki.Web.Engine.Library.Constants;

namespace TightWiki.Web.Engine
{
    public class Helpers
    {
        /// <summary>
        /// Inserts a new page if Page.Id == 0, other wise updates the page. All metadata is written to the database.
        /// </summary>
        public static int UpsertPage(ITightEngine wikifier, IEngineConfiguration config, IEngineDataProvider dataProvider, EnginePage page, ISessionState? sessionState = null)
        {
            bool isNewlyCreated = page.Id == 0;

            page.Id = dataProvider.SavePage(page);

            RefreshPageMetadata(wikifier, config, dataProvider, page, sessionState);

            if (isNewlyCreated)
            {
                //This will update the PageId of references that have been saved to the navigation link.
                dataProvider.UpdateSinglePageReference(page.Navigation, page.Id);
            }

            return page.Id;
        }

        /// <summary>
        /// Rebuilds the page and writes all aspects to the database.
        /// </summary>
        /// <param name="wikifier">The wiki engine</param>
        /// <param name="config">Engine configuration</param>
        /// <param name="dataProvider">Data provider for data access</param>
        /// <param name="page">The page to process</param>
        /// <param name="sessionState">Optional session state</param>
        public static void RefreshPageMetadata(ITightEngine wikifier, IEngineConfiguration config, IEngineDataProvider dataProvider, EnginePage page, ISessionState? sessionState = null)
        {
            //We omit function calls from the tokenization process because they are too dynamic for static searching.
            var state = wikifier.Transform(config, dataProvider, sessionState, page, null,
                [WikiMatchType.StandardFunction]);

            dataProvider.UpdatePageTags(page.Id, state.Tags);
            dataProvider.UpdatePageProcessingInstructions(page.Id, state.ProcessingInstructions);

            var pageTokens = ParsePageTokens(state, dataProvider).Select(o =>
                      new EnginePageToken
                      {
                          PageId = page.Id,
                          Token = o.Token,
                          DoubleMetaphone = o.DoubleMetaphone,
                          Weight = o.Weight
                      }).ToList();

            dataProvider.SavePageSearchTokens(pageTokens);

            dataProvider.UpdatePageReferences(page.Id, state.OutgoingLinks);

            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Id]));
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Navigation]));
        }

        public static List<AggregatedSearchToken> ParsePageTokens(ITightEngineState state, IEngineDataProvider dataProvider)
        {
            var parsedTokens = new List<WeightedSearchToken>();

            parsedTokens.AddRange(ComputeParsedPageTokens(state.HtmlResult, 1, dataProvider));
            parsedTokens.AddRange(ComputeParsedPageTokens(state.Page.Description, 1.2, dataProvider));
            parsedTokens.AddRange(ComputeParsedPageTokens(string.Join(" ", state.Tags), 1.4, dataProvider));
            parsedTokens.AddRange(ComputeParsedPageTokens(state.Page.Name, 1.6, dataProvider));

            var aggregatedTokens = parsedTokens.GroupBy(o => o.Token).Select(o => new AggregatedSearchToken
            {
                Token = o.Key,
                DoubleMetaphone = o.Key.ToDoubleMetaphone(),
                Weight = o.Sum(g => g.Weight)
            }).ToList();

            return aggregatedTokens;
        }

        internal static List<WeightedSearchToken> ComputeParsedPageTokens(string content, double weightMultiplier, IEngineDataProvider dataProvider)
        {
            var searchConfig = dataProvider.GetConfigurationEntriesByGroupName(TightWiki.Contracts.Constants.ConfigurationGroup.Membership);

            var exclusionWords = searchConfig?.Value<string>("Word Exclusions")?
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries).Distinct() ?? new List<string>();
            var strippedContent = Html.StripHtml(content);

            var tokens = strippedContent.Split([' ', '\n', '\t', '-', '_']).ToList();

            if (searchConfig?.Value<bool>("Split Camel Case") == true)
            {
                var allSplitTokens = new List<string>();

                foreach (var token in tokens)
                {
                    var splitTokens = Text.SplitCamelCase(token);
                    if (splitTokens.Count > 1)
                    {
                        splitTokens.ForEach(t => allSplitTokens.Add(t));
                    }
                }

                tokens.AddRange(allSplitTokens);
            }

            tokens = tokens.ConvertAll(d => d.ToLowerInvariant());

            tokens.RemoveAll(o => exclusionWords.Contains(o));

            var searchTokens = (from w in tokens
                                group w by w into g
                                select new WeightedSearchToken
                                {
                                    Token = g.Key,
                                    Weight = g.Count() * weightMultiplier
                                }).ToList();

            return searchTokens.Where(o => string.IsNullOrWhiteSpace(o.Token) == false).ToList();
        }
    }
}
