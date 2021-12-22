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
using VIU.Plugin.SolrSearch.Factories;
using VIU.Plugin.SolrSearch.Models;

namespace VIU.Plugin.SolrSearch.Controllers
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
				
				//cms 
				
				/*
				JArray kontentResult = null;
				var totalBlogsCount = 0;
				var storeLocation = _webHelper.GetStoreLocation();
				var url = $"{storeLocation}{(!storeLocation.EndsWith("/") ? "/" : string.Empty)}kontentsearch/blogs?" +
				          $"query={term}&" +
				          $"pageSize={_catalogSettings.ProductSearchAutoCompleteNumberOfProducts}&" +
				          $"language={(await _workContext.GetWorkingLanguageAsync()).UniqueSeoCode}";
				var response = await _httpClient.GetAsync(url);
				
				if (response.IsSuccessStatusCode)
				{
					var responseString = await response.Content.ReadAsStringAsync();
					
					dynamic kontentResultRaw = JsonConvert.DeserializeObject(responseString);

					totalBlogsCount = kontentResultRaw?.TotalResults;
					kontentResult = kontentResultRaw?.Results as JArray;
				}
				else
				{
					await _logger.ErrorAsync($"SearchTermAutoComplete (CMS): There is a problem with the kontent search plugin. Is it installed and running? - response code was {response.StatusCode}");
				}
				
				if(kontentResult == null) return Json(productResultJson);
				
				var kontentResultJson = (kontentResult.Children()
						.Take(productNumber)
						.Select(k =>
						{
							var label = (string) k.Children<JProperty>().FirstOrDefault(j => j.Name == "Title") ?? string.Empty;

							var indexOfTerm = label.IndexOf(term, StringComparison.InvariantCultureIgnoreCase);
							var termToHighlight = indexOfTerm >= 0 ? label.Substring(indexOfTerm, term.Length) : string.Empty;
							
							return new
							{
								type = "blog",
								label = label.Replace(term, $@"<b>{termToHighlight}</b>", StringComparison.InvariantCultureIgnoreCase),
								producturl = Url.RouteUrl("KontentBlog", new { SeName = (string)k.Children<JProperty>().FirstOrDefault(j => j.Name == "SeName") }), //$"{storeLocation}{(!storeLocation.EndsWith("/") ? "/" : string.Empty)}{(string) k.Children<JProperty>().FirstOrDefault(j => j.Name == "Url")}",
								productpictureurl = (string) k.Children<JProperty>().FirstOrDefault(j => j.Name == "PreviewImage"),
								totalcountblogs = totalBlogsCount,
								totalcountproducts = 0,
								showlinktoresultsearch = showLinkToResultSearch
							};
						}))
					.ToList();

				var resultJson = productResultJson.Concat(kontentResultJson).ToList();
				*/
				
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