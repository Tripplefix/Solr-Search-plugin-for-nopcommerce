using Nop.Services.Catalog;
using Nop.Services.Tasks;
using Nop.Plugin.SolrSearch.Services;
using Task = System.Threading.Tasks.Task;

namespace Nop.Plugin.SolrSearch.Tasks
{
    public class IndexTask : IScheduleTask
    {
        private readonly IProductIndexingService _productIndexingService;
        private readonly IProductService _productService;

        public IndexTask(IProductIndexingService productIndexingService, IProductService productService)
        {
	        _productIndexingService = productIndexingService;
	        _productService = productService;
        }

        public async Task ExecuteAsync()
        {
	        var products = await _productService.SearchProductsAsync(visibleIndividuallyOnly: true);
	        
            await _productIndexingService.ReindexAllProducts(products);
        }
    }
}
