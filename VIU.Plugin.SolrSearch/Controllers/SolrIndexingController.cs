using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using VIU.Plugin.SolrSearch.Services;

namespace VIU.Plugin.SolrSearch.Controllers
{
    public class SolrIndexingController : Controller
    {
        private readonly IProductIndexingService _productIndexingService;

        public SolrIndexingController(IProductIndexingService productIndexingService)
        {
            _productIndexingService = productIndexingService;
        }

        [AuthorizeAdmin]
        [HttpGet]
        [Route("/solr/index/products")]
        public async Task<ActionResult> ReindexProducts()
        {
            var result = await _productIndexingService.ReindexAllProducts();
            return Ok(result);
        }

        [AuthorizeAdmin]
        [HttpGet]
        [Route("/solr/clear/products")]
        public ActionResult ClearProducts()
        {
            _productIndexingService.Clear();
            return Ok("Solr: product index cleared");
        }
    }
}
