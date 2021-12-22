using Microsoft.AspNetCore.Mvc;
using Nop.Services.Catalog;
using Nop.Services.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Plugin.SolrSearch.Factories;
using Nop.Plugin.SolrSearch.Models;

namespace Nop.Plugin.SolrSearch.Controllers
{
    public class SolrSearchController : Controller
    {
        private readonly ISolrSearchFactory _solrSearchFactory;
        private readonly IExtendedProductModelFactory _extendedProductModelFactory;
        private readonly IProductService _productService;
        private readonly ILogger _logger;

        public SolrSearchController(ISolrSearchFactory solrSearchFactory, IExtendedProductModelFactory extendedProductModelFactory, IProductService productService, ILogger logger)
        {
            _solrSearchFactory = solrSearchFactory;
            _extendedProductModelFactory = extendedProductModelFactory;
            _productService = productService;
            _logger = logger;
        }

        [HttpGet]
        [Route("/solr/search/products/json")]
        public ActionResult SearchProductsJson(string q, string language = null, string facetString = null)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest();
    
            try 
            {
                var searchModel = new ProductSolrResultModel
                {
                    q = q,
                    Facets = facetString,
                    PresetLanguage = language
                };
                return Ok(_solrSearchFactory.PrepareSearchModel(searchModel));
            }
            catch (Exception e)
            {
                _logger.ErrorAsync("Error occurred during search", e);
            }
            
            return BadRequest();
        }

        [HttpGet]
        public async Task<ActionResult> SearchProducts(ProductSolrResultModel model)
        {
            model ??= new ProductSolrResultModel();
            model = await _solrSearchFactory.PrepareSearchModel(model);
            
            return View(model);
        }

        [HttpGet]
        [CheckLanguageSeoCode(true)]
        public async Task<ActionResult> SearchProductsUpdate(ProductSolrResultModel model)
        {
            model ??= new ProductSolrResultModel();
            model = await _solrSearchFactory.PrepareSearchModel(model);
            
        	return PartialView("_ProductsInGridOrLines", model);
        }
        
        [HttpGet]
        [CheckLanguageSeoCode(true)]
        public async Task<ActionResult> SearchFiltersUpdate(ProductSolrResultModel model)
        {
            model ??= new ProductSolrResultModel();
            model = await _solrSearchFactory.PrepareSearchModel(model);
    
            return PartialView("_FacetFilter", model);
        }

        [HttpGet]
        [Route("/prices/json")]
        public async Task<ActionResult> PricesJson(int[] productIds)
        {
            if (productIds == null || !productIds.Any())
            {
                await _logger.ErrorAsync("Error during price calculation. \"productIds\" was null or did not contain any values.");
                
                return BadRequest();
            }
            
            var products = await _productService.GetProductsByIdsAsync(productIds);

            var priceModels = await products.SelectAwait(async product => new
            {
                id = product.Id,
                priceModel = await _extendedProductModelFactory.GetPriceModel(product)
            }).ToListAsync();

            return Ok(priceModels);
        }
    }
}
