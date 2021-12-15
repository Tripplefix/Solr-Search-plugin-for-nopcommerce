using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Web.Models.Catalog;

namespace VIU.Plugin.SolrSearch.Factories
{
    public interface IExtendedProductModelFactory
    {
        Task<ProductOverviewModel.ProductPriceModel> GetPriceModel(Product product);
    }
}
