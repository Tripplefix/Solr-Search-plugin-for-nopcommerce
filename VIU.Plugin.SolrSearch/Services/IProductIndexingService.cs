using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;

namespace VIU.Plugin.SolrSearch.Services
{
    public interface IProductIndexingService
    {
        Task<string> ReindexAllProducts(IList<Product> products);
        Task<string> AddOrUpdateProduct(Product product);
        Task<string> DeleteProduct(Product product);
        void ClearIndex();
    }
}
