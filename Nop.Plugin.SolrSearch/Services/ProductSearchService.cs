using Nop.Core;
using SolrNet;
using SolrNet.Commands.Parameters;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Events;
using Nop.Plugin.SolrSearch.Infrastructure;
using Nop.Plugin.SolrSearch.Models;
using Nop.Plugin.SolrSearch.Settings;
using Nop.Plugin.SolrSearch.Tools;

namespace Nop.Plugin.SolrSearch.Services
{
    public class ProductSearchService : IProductSearchService
    {
        private readonly ISolrOperations<ProductSolrDocument> _solrOperations;
        private readonly IWorkContext _workContext;
        private readonly SolrSearchSettings _solrSearchSettings;
        private readonly IEventPublisher _eventPublisher;

        public ProductSearchService(ISolrOperations<ProductSolrDocument> solrOperations, IWorkContext workContext, SolrSearchSettings solrSearchSettings, IEventPublisher eventPublisher)
        {
            _solrOperations = solrOperations;
            _workContext = workContext;
            _solrSearchSettings = solrSearchSettings;
            _eventPublisher = eventPublisher;
        }

        public async Task<SolrQueryResults<ProductSolrDocument>> Search(string q, string languageKey = null, IEnumerable<KeyValuePair<string, List<string>>> filterFacets = null, List<string> returnFacets = null)
        {

            if (string.IsNullOrWhiteSpace(q))
            {
                if (!_solrSearchSettings.AllowEmptySearchQuery)
                {
                    return new SolrQueryResults<ProductSolrDocument>();
                }

                q = "*:*";
            }

            //facets
            filterFacets ??= new List<KeyValuePair<string, List<string>>>();
            
            var filter = new List<ISolrQuery>();
            
            foreach (var (facetName, facetOptions) in filterFacets)
            {
                var facetFilter = 
                    new LocalParams { { "tag", facetName }, { "mincount", "1" } } +
                    new SolrQueryInList(ProductSolrDocument.SOLRFIELD_MULITVALUETEXT_EXTENSION + facetName, facetOptions);
                
                filter.Add(facetFilter);
            }

            var queryOptions = new QueryOptions
            {
                Rows = _solrSearchSettings.MaxReturnedDocuments,
                FilterQueries = filter,
                Facet = returnFacets != null ? new FacetParameters
                {
                    Queries = returnFacets.Select(facet =>
                            new SolrFacetFieldQuery(new LocalParams {{"ex", facet}} + ProductSolrDocument.SOLRFIELD_MULITVALUETEXT_EXTENSION + facet))
                        .ToArray(),
                    MinCount = _solrSearchSettings.HideFacetOptionsWithNoCount ? 1 : 0
                } : null
            };

            if (_solrSearchSettings.HighlightingEnabled)
            {
                queryOptions.Highlight = new HighlightingParameters
                {
                    Fields = new[]
                    {
                        SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_NAME, languageKey, true),
                        SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_NAME, languageKey, false)
                    },
                    BeforeTerm = "<b>",
                    AfterTerm = "</b>"
                };
            }

            if (_solrSearchSettings.SpellcheckingEnabled)
            {
                queryOptions.SpellCheck = new SpellCheckingParameters
                {
                    Collate = true,
                    OnlyMorePopular = _solrSearchSettings.SpellcheckingOnlyMorePopular
                };
            }

            var languageBasedQueries = await PrepareQueries(q, languageKey, false);

            var result = await _solrOperations.QueryAsync(languageBasedQueries, queryOptions);

            if (_solrSearchSettings.EnableHeroProducts)
            {
                HandleHeroProducts(result);
            }
            
            if (result != null && result.Count > 0)
            {
                return result;
            }

            var defaultQueries = await PrepareQueries(q, languageKey, true);
            
            result = await _solrOperations.QueryAsync(defaultQueries, queryOptions);
            
            if (_solrSearchSettings.EnableHeroProducts)
            {
                HandleHeroProducts(result);
            }
            
            return result;
        }

        private async Task<SolrMultipleCriteriaQuery> PrepareQueries(string q, string language, bool isDefault)
        {
            language = isDefault ? _solrSearchSettings.DefaultLanguage : language;
            
            //if language not given, try to use from workContext
            if (string.IsNullOrWhiteSpace(language))
            {
                language = SolrTools.GetLanguageKey(await _workContext.GetWorkingLanguageAsync());
            }
            
            var queries = new List<ISolrQuery> {
                new SolrQueryByField(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_NAME, language, isDefault), q).Boost(_solrSearchSettings.ProductNameQueryBoost ?? 0),
                new SolrQueryByField(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_SHORTDESCRIPTION, language, isDefault), q),
                new SolrQueryByField(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_FULLDESCRIPTION, language, isDefault), q),
                new SolrQueryByField(SolrTools.GetStaticTextFieldName(ProductSolrDocument.SOLRFIELD_SKU), q).Boost(_solrSearchSettings.ProductSkuQueryBoost ?? 0),
                new SolrQueryByField(SolrTools.GetStaticTextFieldName(ProductSolrDocument.SOLRFIELD_GTIN), q).Boost(_solrSearchSettings.ProductGtinQueryBoost ?? 0)
                //todo: specification attributes? categories? manufacturers?
            };

            //wildcard search
            if (_solrSearchSettings.WildcardQueryEnabled && q.Length >= _solrSearchSettings.WildcardQueryMinLength && !q.EndsWith("*"))
            {
                var fieldValue = q;
                switch (_solrSearchSettings.WildcardQuerySelectedType)
                {
                    case SolrSearchSettings.WildcardQueryType.Prefix:
                        fieldValue = "*" + fieldValue;
                        break;
                    case SolrSearchSettings.WildcardQueryType.PrefixAndPostfix:
                        fieldValue = "*" + fieldValue + "*";
                        break;
                    case SolrSearchSettings.WildcardQueryType.Postfix:
                    default:
                        fieldValue += "*";
                        break;
                }
                
                queries.Add(new SolrQueryByField(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_NAME, language, isDefault), fieldValue)
                {
                    Quoted = false
                }.Boost(_solrSearchSettings.WildcardQueryBoost ?? 0));
            }

            //fuzzy search
            if (_solrSearchSettings.FuzzyQueryEnabled && q.Length >= _solrSearchSettings.FuzzyQueryMinLength && !q.EndsWith("~"))
            {
                var fieldValue = q + "~";

                if (_solrSearchSettings.FuzzyQueryFuzziness != null)
                {
                    fieldValue += _solrSearchSettings.FuzzyQueryFuzziness;
                }
                
                queries.Add(new SolrQueryByField(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_NAME, language, isDefault), fieldValue)
                {
                    Quoted = false
                }.Boost(_solrSearchSettings.FuzzyQueryBoost ?? 0));
            }

            //phrase search
            if (_solrSearchSettings.PhraseQueryEnabled)
            {
                var fieldValue = $@"""{q}""";

                if (_solrSearchSettings.PhraseQueryProximity != null)
                {
                    fieldValue += "~" + _solrSearchSettings.PhraseQueryProximity;
                }
                
                queries.Add(new SolrQueryByField(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_NAME, language, isDefault), fieldValue)
                {
                    Quoted = false
                }.Boost(_solrSearchSettings.PhraseQueryBoost ?? 0));
            }
            
            //raise event       
            await _eventPublisher.PublishAsync(new ProductSearchedEvent(queries, q, language, false));

            return new SolrMultipleCriteriaQuery(queries, "OR");
        }

        private void HandleHeroProducts(List<ProductSolrDocument> results)
        {
            var heroProducts = _solrSearchSettings.HeroProducts;
            
            if (string.IsNullOrWhiteSpace(heroProducts)) return;
            
            var heroProductList = heroProducts.Split(',').Where(m => int.TryParse(m, out _)).Select(int.Parse).Reverse().ToList();
            
            foreach (var hpId in heroProductList)
            {
                var index = results.FindIndex(item => item.Id == hpId);
                if (index <= 0) continue;
                    
                var item = results[index];
                for (var i = index; i > 0; i--)
                {
                    results[i] = results[i - 1];
                }
                results[0] = item;
            }
        }

    }
}
