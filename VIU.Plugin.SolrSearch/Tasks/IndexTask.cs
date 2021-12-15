using Nop.Services.Tasks;
using VIU.Plugin.SolrSearch.Services;
using Task = System.Threading.Tasks.Task;

namespace VIU.Plugin.SolrSearch.Tasks
{
    public class IndexTask : IScheduleTask
    {
        private readonly IProductIndexingService _productIndexingService;

        public IndexTask(IProductIndexingService productIndexingService)
        {
            _productIndexingService = productIndexingService;
        }

        public Task ExecuteAsync()
        {
            return _productIndexingService.ReindexAllProductsTask();
        }
    }
}
