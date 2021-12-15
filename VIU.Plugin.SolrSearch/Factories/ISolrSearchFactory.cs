using System.Threading.Tasks;
using Nop.Web.Models.Catalog;
using VIU.Plugin.SolrSearch.Models;

namespace VIU.Plugin.SolrSearch.Factories
{
	public interface ISolrSearchFactory
	{
		Task<ProductSolrResultModel> PrepareSearchModel(ProductSolrResultModel model);

		ProductOverviewModel PrepareProductOverviewModel(ProductSolrDocument coreProduct,
			string languageKey,
			bool preparePriceModel = true,
			bool preparePictureModel = true,
			bool prepareSpecificationAttributes = false);
	}
}