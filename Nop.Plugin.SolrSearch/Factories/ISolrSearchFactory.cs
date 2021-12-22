using System.Threading.Tasks;
using Nop.Plugin.SolrSearch.Models;

namespace Nop.Plugin.SolrSearch.Factories
{
	public interface ISolrSearchFactory
	{
		Task<ProductSolrResultModel> PrepareSearchModel(ProductSolrResultModel model, bool highlightResults = false);
	}
}