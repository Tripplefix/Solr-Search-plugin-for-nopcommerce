using System.Collections.Generic;
using System.Threading.Tasks;
using SolrNet;
using VIU.Plugin.SolrSearch.Models;

namespace VIU.Plugin.SolrSearch.Services
{
    public interface IProductSearchService
    {
        Task<SolrQueryResults<ProductSolrDocument>> Search(string q, string languageKey = null,
            IEnumerable<KeyValuePair<string, List<string>>> facets = null, List<string> returnfacets = null);
    }
}
