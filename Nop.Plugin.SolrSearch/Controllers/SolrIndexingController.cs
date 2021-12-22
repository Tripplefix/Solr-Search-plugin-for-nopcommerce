using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Catalog;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Plugin.SolrSearch.Services;

namespace Nop.Plugin.SolrSearch.Controllers
{
    public class SolrIndexingController : Controller
    {
        private readonly IProductIndexingService _productIndexingService;
        private readonly IProductService _productService;

        public SolrIndexingController(IProductIndexingService productIndexingService, IProductService productService)
        {
	        _productIndexingService = productIndexingService;
	        _productService = productService;
        }

        [AuthorizeAdmin]
        [HttpPost]
        [Route("/solr/index/products")]
        public async Task<ActionResult> ReindexProducts()
        {
	        var products = await _productService.SearchProductsAsync(visibleIndividuallyOnly: true);
	        
	        var result = await _productIndexingService.ReindexAllProducts(products);
	        
	        return Ok(result);
        }

        [AuthorizeAdmin]
        [HttpPut,HttpPost]
        [Route("/solr/update/product/{id}")]
        public async Task<ActionResult> AddOrUpdateProduct(int id)
        {
	        var product = await _productService.GetProductByIdAsync(id);
	        
	        var result = await _productIndexingService.AddOrUpdateProduct(product);
	        
	        return Ok(result);
        }

        [AuthorizeAdmin]
        [HttpDelete]
        [Route("/solr/delete/product/{id}")]
        public async Task<ActionResult> DeleteProduct(int id)
        {
	        var product = await _productService.GetProductByIdAsync(id);
	        
	        var result = await _productIndexingService.DeleteProduct(product);
	        
	        return Ok(result);
        }

        [AuthorizeAdmin]
        [HttpPost]
        [Route("/solr/clear/products")]
        public ActionResult ClearProducts()
        {
            _productIndexingService.ClearIndex();
            return Ok("Solr: product index cleared");
        }
    }
}
