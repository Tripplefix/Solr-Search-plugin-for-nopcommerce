using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Logging;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Models.Catalog;
using Nop.Plugin.SolrSearch.Factories;
using Nop.Plugin.SolrSearch.Models;

namespace Nop.Plugin.SolrSearch.Controllers
{
	public class CatalogExtendedController : Controller
	{
		private readonly ISolrSearchFactory _solrSearchFactory;
		private readonly ILogger _logger;
		private readonly CatalogSettings _catalogSettings;
		private readonly IWebHelper _webHelper;
		private readonly HttpClient _httpClient;
		private readonly IWorkContext _workContext;

		public CatalogExtendedController(ISolrSearchFactory solrSearchFactory, ILogger logger, CatalogSettings catalogSettings, IWebHelper webHelper, IWorkContext workContext)
		{
			_solrSearchFactory = solrSearchFactory;
			_logger = logger;
			_catalogSettings = catalogSettings;
			_webHelper = webHelper;
			_httpClient = new HttpClient();
			_workContext = workContext;
		}

		[CheckLanguageSeoCode(true)]
		public async Task<IActionResult> SearchTermAutoComplete(string term)
		{
			if (string.IsNullOrWhiteSpace(term))
				return Content("");

			term = term.Trim();

			if (string.IsNullOrWhiteSpace(term) || term.Length < _catalogSettings.ProductSearchTermMinimumLength)
				return Content("");
    
			try 
			{
				var searchModel = new ProductSolrResultModel
				{
					q = term
				};
				
				var showLinkToResultSearch = _catalogSettings.ShowLinkToAllResultInSearchAutoComplete;

				//products
				var productNumber = _catalogSettings.ProductSearchAutoCompleteNumberOfProducts > 0 ?
					_catalogSettings.ProductSearchAutoCompleteNumberOfProducts : 10;  

				var productResult = (await _solrSearchFactory.PrepareSearchModel(searchModel, true)).Products;
				
				var productResultJson = (productResult.Take(productNumber)
						.Select(p => new
						{
							type = "product",
							label = p.Name,
							producturl = Url.RouteUrl("Product", new { SeName = p.SeName }),
							productpictureurl = p.DefaultPictureModel.ThumbImageUrl,
							totalcountblogs = 0,
							totalcountproducts = productResult.Count,
							showlinktoresultsearch = showLinkToResultSearch
						}))
					.ToList();
				
				return Json(productResultJson);
			}
			catch (Exception e)
			{
				await _logger.ErrorAsync("Error occurred during search", e);
			}
            
			return BadRequest();
		}
	}
}