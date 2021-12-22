using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.SolrSearch.Factories
{
    public interface IExtendedProductModelFactory
    {
        Task<ProductOverviewModel.ProductPriceModel> GetPriceModel(Product product);
    }
}
