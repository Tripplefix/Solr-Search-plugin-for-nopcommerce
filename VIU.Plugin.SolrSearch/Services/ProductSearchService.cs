using Nop.Core;
using SolrNet;
using SolrNet.Commands.Parameters;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VIU.Plugin.SolrSearch.Models;
using VIU.Plugin.SolrSearch.Settings;
using VIU.Plugin.SolrSearch.Tools;

namespace VIU.Plugin.SolrSearch.Services
{
    public class ProductSearchService : IProductSearchService
    {
        private readonly ISolrOperations<ProductSolrDocument> _solrOperations;
        private readonly IWorkContext _workContext;
        private readonly ViuSolrSearchSettings _viuSolrSearchSettings;

        public ProductSearchService(ISolrOperations<ProductSolrDocument> solrOperations, IWorkContext workContext, ViuSolrSearchSettings viuSolrSearchSettings)
        {
            _solrOperations = solrOperations;
            _workContext = workContext;
            _viuSolrSearchSettings = viuSolrSearchSettings;
        }

        public async Task<SolrQueryResults<ProductSolrDocument>> Search(string q, string languageKey = null,
            IEnumerable<KeyValuePair<string, List<string>>> facets = null, List<string> returnfacets = null)
        {
            var defaultLanguage = _viuSolrSearchSettings.DefaultLanguage ?? "en";

            if (string.IsNullOrWhiteSpace(q))
            {
                if (!_viuSolrSearchSettings.AllowEmptySearchQuery)
                {
                    return new SolrQueryResults<ProductSolrDocument>();
                }

                q = "*:*";
            }

            //facets
            facets ??= new List<KeyValuePair<string, List<string>>>();

            var facetQueryPartList = new List<AbstractSolrQuery>();

            foreach (var (facetName, facetOptions) in facets)
            {
                var multipleFacetValuesForOneFacet = facetOptions
                    .Select(facetValue => new SolrQueryByField(ProductSolrDocument.SOLRFIELD_SPECIFICATION_ATTRIBUTE + facetName, facetValue))
                    .Cast<AbstractSolrQuery>()
                    .ToList();

                facetQueryPartList.Add(new SolrMultipleCriteriaQuery(multipleFacetValuesForOneFacet, "OR"));
            }

            var facetQueryPart = new SolrMultipleCriteriaQuery(facetQueryPartList, "AND");

            //return facets
            var queryOptions = ConfigureQueryOptions(returnfacets);

            //search for q within "static" fields
            var skuQuery = new SolrQueryByField(SolrTools.GetStaticTextFieldName(ProductSolrDocument.SOLRFIELD_SKU), q).Boost(30);
            var gtinQuery = new SolrQueryByField(SolrTools.GetStaticTextFieldName(ProductSolrDocument.SOLRFIELD_GTIN), q).Boost(30);

            //if language not given, try to use from workContext
            if (string.IsNullOrWhiteSpace(languageKey))
            {
                languageKey = SolrTools.GetLanguageKey(await _workContext.GetWorkingLanguageAsync());
            }

            //search language based
            var languageBasedQueryPart = new SolrMultipleCriteriaQuery(new List<AbstractSolrQuery> {
                GetFuzzySolrQueryField(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_NAME, languageKey), q).Boost(20),
                new SolrQueryByField(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_SHORTDESCRIPTION, languageKey), q),
                new SolrQueryByField(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_FULLDESCRIPTION, languageKey), q),
                skuQuery,
                gtinQuery
            }, "OR");

            var languageBasedQueryList = new List<AbstractSolrQuery>
            {
                languageBasedQueryPart,
                facetQueryPart
            };

            var result = _solrOperations.Query(new SolrMultipleCriteriaQuery(languageBasedQueryList, "AND"), queryOptions);

            if (result != null && result.Count > 0)
            {
                return result;
            }

            //search in default fields: if language not given or language-based search did not find any results
            var defaultQueryPart = new SolrMultipleCriteriaQuery(new List<AbstractSolrQuery> {
                GetFuzzySolrQueryField(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_NAME, defaultLanguage, true), q).Boost(20),
                new SolrQueryByField(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_SHORTDESCRIPTION, defaultLanguage, true), q),
                new SolrQueryByField(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_FULLDESCRIPTION, defaultLanguage, true), q),
                skuQuery,
                gtinQuery
            }, "OR");

            var defaultQueryList = new List<AbstractSolrQuery>
            {
                defaultQueryPart,
                facetQueryPart
            };

            result = _solrOperations.Query(new SolrMultipleCriteriaQuery(defaultQueryList, "AND"), queryOptions);

            return result;
        }

        //TODO (if required) improve: support for SolrQueryRange, Date facets etc.
        private static QueryOptions ConfigureQueryOptions(List<string> returnfacets, int maxRows = 250)
        {
            return new QueryOptions
            {
                Rows = maxRows,
                Facet = returnfacets != null ? new FacetParameters { Queries = returnfacets.Select(facet => new SolrFacetFieldQuery(ProductSolrDocument.SOLRFIELD_SPECIFICATION_ATTRIBUTE + facet)).ToArray() } : null
            };
        }

        private static SolrQueryByField GetFuzzySolrQueryField(string field, string q)
        {
            return new SolrQueryByField(field, q + "~")
            {
                Quoted = false
            };
        }
    }
}
