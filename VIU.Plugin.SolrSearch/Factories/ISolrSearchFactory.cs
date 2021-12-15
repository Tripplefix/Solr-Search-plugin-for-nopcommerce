using System.Threading.Tasks;
using VIU.Plugin.SolrSearch.Models;

namespace VIU.Plugin.SolrSearch.Factories
{
	public interface ISolrSearchFactory
	{
		Task<ProductSolrResultModel> PrepareSearchModel(ProductSolrResultModel model);
	}
}