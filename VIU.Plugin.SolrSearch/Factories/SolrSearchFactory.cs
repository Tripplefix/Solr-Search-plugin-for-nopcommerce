using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Web.Factories;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.Media;
using VIU.Plugin.SolrSearch.Models;
using VIU.Plugin.SolrSearch.Services;
using VIU.Plugin.SolrSearch.Settings;
using VIU.Plugin.SolrSearch.Tools;

namespace VIU.Plugin.SolrSearch.Factories
{
	public sealed class SolrSearchFactory : ISolrSearchFactory
	{
		private readonly IProductSearchService _productSearchService;
		private readonly IWorkContext _workContext;
		private readonly ILocalizationService _localizationService;
		private readonly ILanguageService _languageService;
		private readonly IStoreContext _storeContext;
		private readonly ICategoryService _categoryService;
		private readonly ISpecificationAttributeService _specificationAttributeService;
		private readonly ILogger _logger;
		private readonly ViuSolrSearchSettings _viuSolrSearchSettings;
		private readonly IManufacturerService _manufacturerService;
		private readonly IProductModelFactory _productModelFactory;
		private readonly IProductService _productService;

		public SolrSearchFactory(IProductSearchService productSearchService, IWorkContext workContext, ILocalizationService localizationService, ILanguageService languageService, IStoreContext storeContext, ICategoryService categoryService, ISpecificationAttributeService specificationAttributeService, ILogger logger, ViuSolrSearchSettings viuSolrSearchSettings, IManufacturerService manufacturerService, IProductModelFactory productModelFactory, IProductService productService)
		{
			_productSearchService = productSearchService;
			_workContext = workContext;
			_localizationService = localizationService;
			_languageService = languageService;
			_storeContext = storeContext;
			_categoryService = categoryService;
			_specificationAttributeService = specificationAttributeService;
			_logger = logger;
			_viuSolrSearchSettings = viuSolrSearchSettings;
			_manufacturerService = manufacturerService;
			_productModelFactory = productModelFactory;
			_productService = productService;
		}

		public async Task<ProductSolrResultModel> PrepareSearchModel(ProductSolrResultModel model)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}

			var searchTerms = model.q ?? string.Empty;
			searchTerms = searchTerms.Trim();

			var language = string.IsNullOrEmpty(model.PresetLanguage) 
				? await _workContext.GetWorkingLanguageAsync() 
				: (await _languageService.GetAllLanguagesAsync()).FirstOrDefault(l => l.UniqueSeoCode == model.PresetLanguage);

			var availableFacetList = new List<string>();
			
			//Categories
			if (_viuSolrSearchSettings.IncludeCategoriesInFilter)
			{
				var categories = await _categoryService.GetAllCategoriesAsync(storeId: (await _storeContext.GetCurrentStoreAsync()).Id);
			
				if (categories.Any())
				{
					availableFacetList.Add(ProductSolrDocument.SOLRFIELD_ALLCATEGORIES);
				}
			}
			
			//Manufacturers
			if (_viuSolrSearchSettings.IncludeManufacturersInFilter)
			{
				var manufacturers = await _manufacturerService.GetAllManufacturersAsync(storeId: (await _storeContext.GetCurrentStoreAsync()).Id);

				if (manufacturers.Any())
				{
					availableFacetList.Add(ProductSolrDocument.SOLRFIELD_ALLMANUFACTURERS);
				}
			}

			if (_viuSolrSearchSettings.IncludeSpecificationAttributesInFilter)
			{
				//Specification Attributes
				var specificationAttributes = await (await _specificationAttributeService.GetProductSpecificationAttributesAsync(allowFiltering: true))
					.SelectAwait(async psa =>
					{
						var specAttributeOption =
							await _specificationAttributeService.GetSpecificationAttributeOptionByIdAsync(
								psa.SpecificationAttributeOptionId);
						var specAttribute =
							await _specificationAttributeService.GetSpecificationAttributeByIdAsync(specAttributeOption
								.SpecificationAttributeId);

						return specAttribute.Id;
					}).ToListAsync();


				if (!string.IsNullOrWhiteSpace(_viuSolrSearchSettings.SelectedFilterableSpecificationAttributeIds))
				{
					var selectedSaIds = _viuSolrSearchSettings.SelectedFilterableSpecificationAttributeIds.Split(',').Select(int.Parse).ToList();

					specificationAttributes = specificationAttributes.Intersect(selectedSaIds).ToList();
				}

				if (specificationAttributes.Any())
				{				
					availableFacetList.AddRange(specificationAttributes.Select(specId => ProductSolrDocument.SOLRFIELD_PREFIX_SPECIFICATION_ATTRIBUTES + specId));
				}
			}

			//Products
			IList<KeyValuePair<string, List<string>>> facets = null;
			
			try
			{
				if (!string.IsNullOrWhiteSpace(model.Facets))
				{
					facets = HttpUtility.UrlDecode(model.Facets).Split(',').Select(e =>
							new KeyValuePair<string, List<string>>(e.Split(":")[0], e.Split(":")[1].Split("|").ToList())
						).ToList();
				}
			}
			catch (Exception ex)
			{
				await _logger.ErrorAsync($"Invalid search query: \"{model.Facets}\"", ex);
			}

			var languageKey = SolrTools.GetLanguageKey(language);

			//do the search
			var searchResult = await _productSearchService.Search(searchTerms, languageKey, facets, availableFacetList);

			//enrich the result: product part
			model.Products = await searchResult.SelectAwait(async coreProduct => await PrepareProductOverviewModel(coreProduct, languageKey))
				.Where(p => p.Id != 0)
				.ToListAsync();

			//enrich the result: facet part
			model.ProductFacets = await searchResult.FacetFields
				.SelectAwait(async coreFacet =>
				{
					var (facetKey, facetValues) = coreFacet;
					var facetName = facetKey.Replace(ProductSolrDocument.SOLRFIELD_MULITVALUETEXT_EXTENSION, string.Empty);
					
					return new ProductFacet
					{
						FacetName = facetName,
						FacetDisplayName = await GetFacetDisplayName(facetKey, language.Id),
						FacetValues = await facetValues
							.SelectAwait(async coreValue =>
							{
								var (optionName, productCount) = coreValue;

								var facetValue = await PrepareFacetValue(facetKey, optionName, language.Id);

								if (facetValue == null) return null;

								facetValue.FilterActive = facets != null && GetFilterActive(facets, facetName, optionName: optionName);
								
								// move selected facets to the top
								facetValue.DisplayOrder = facetValue.FilterActive
									? int.MinValue
									: facetValue.DisplayOrder;
								facetValue.OptionName = optionName;
								facetValue.OptionProductCount = productCount;

								return facetValue;
							})
							.Where(f => f != null)
							.OrderBy(f => f.DisplayOrder)
							.ToListAsync()
					};
				})
				.Where(productFacet => productFacet.FacetValues.Any(facetValue => facetValue.OptionProductCount > 0))
				.ToListAsync();

			if (_viuSolrSearchSettings.HighlightingEnabled)
			{
				foreach (var product in model.Products)
				{
					var highlights = searchResult.Highlights.FirstOrDefault(h => h.Key == product.Id.ToString()).Value;

					var highlightedProductName = highlights.FirstOrDefault(h => h.Key == SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_NAME, languageKey, false));

					if (highlightedProductName.Equals(default(KeyValuePair<string, ICollection<string>>)) || string.IsNullOrWhiteSpace(highlightedProductName.Value.FirstOrDefault()))
					{
						highlightedProductName = highlights.FirstOrDefault(h => h.Key == SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_NAME, languageKey, true));
					}

					if (!highlightedProductName.Equals(default(KeyValuePair<string, ICollection<string>>)) && !string.IsNullOrWhiteSpace(highlightedProductName.Value.FirstOrDefault()))
					{
						product.Name = highlightedProductName.Value.FirstOrDefault();
					}
				}
			}

			model.NoResults = !model.Products.Any();

			return model;
		}

		private async Task<ProductFacet.FacetValue> PrepareFacetValue(string facetKey, string optionName, int languageId)
		{
			int.TryParse(optionName, out var optionId);

			string facetDisplayName;
			int facetDisplayOrder;

			switch (facetKey)
			{
				case ProductSolrDocument.SOLRFIELD_MULITVALUETEXT_EXTENSION + ProductSolrDocument.SOLRFIELD_ALLCATEGORIES:
				{
					var category = await _categoryService.GetCategoryByIdAsync(optionId);

					if (category == null) return null;
				
					facetDisplayName = category.Name;
					facetDisplayOrder = category.DisplayOrder;
					break;
				}
				case ProductSolrDocument.SOLRFIELD_MULITVALUETEXT_EXTENSION + ProductSolrDocument.SOLRFIELD_ALLMANUFACTURERS:
				{
					var manufacturer = await _manufacturerService.GetManufacturerByIdAsync(optionId);

					if (manufacturer == null) return null;
				
					facetDisplayName = manufacturer.Name;
					facetDisplayOrder = manufacturer.DisplayOrder;
					break;
				}
				default:
				{
					var specOption = await _specificationAttributeService.GetSpecificationAttributeOptionByIdAsync(optionId);
			
					if (specOption == null) return null;
			
					facetDisplayName = await _localizationService.GetLocalizedAsync(specOption, b => b.Name, languageId);
					facetDisplayOrder = specOption.DisplayOrder;
					break;
				}
			}
			
			return new ProductFacet.FacetValue
			{
				OptionDisplayName = facetDisplayName,
				DisplayOrder = facetDisplayOrder,
			};
		}

		private async Task<ProductOverviewModel> PrepareProductOverviewModel(ProductSolrDocument coreProduct, string languageKey)
		{
			var defaultLanguage = _viuSolrSearchSettings.DefaultLanguage ?? "en";

			var model = new ProductOverviewModel
			{
				Id = coreProduct.Id,

				Name = coreProduct.OtherFields.TryGetValue(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_NAME, languageKey), out var localizedName)
				? localizedName.ToString()
				: (coreProduct.OtherFields.TryGetValue(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_NAME, defaultLanguage, true), out var defaultName) ? defaultName.ToString() : "Product name"),

				ShortDescription = coreProduct.OtherFields.TryGetValue(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_SHORTDESCRIPTION, languageKey), out var localizedSD)
				? localizedSD.ToString()
				: (coreProduct.OtherFields.TryGetValue(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_SHORTDESCRIPTION, defaultLanguage, true), out var defaultSD) ? defaultSD.ToString() : "Short description"),

				FullDescription = coreProduct.OtherFields.TryGetValue(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_FULLDESCRIPTION, languageKey), out var localizedFD) 
				? localizedFD.ToString() 
				: (coreProduct.OtherFields.TryGetValue(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_FULLDESCRIPTION, defaultLanguage, true), out var defaultFD) ? defaultFD.ToString() : "Full description"),

				SeName = coreProduct.OtherFields.TryGetValue(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_SE_NAME, languageKey), out var localizedSN) 
				? localizedSN.ToString() 
				: (coreProduct.OtherFields.TryGetValue(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_SE_NAME, defaultLanguage, true), out var defaultSN) ? defaultSN.ToString() : "Full description"),

				Sku = coreProduct.Sku
				//MarkAsNew = coreProduct.MarkAsNew, TODO: implement
			};

			//price
			if (_viuSolrSearchSettings.PreparePriceModel)
			{
				model.ProductPrice = new ProductOverviewModel.ProductPriceModel
				{
					DisableBuyButton = coreProduct.DisableBuyButton,
					DisableWishlistButton = coreProduct.DisableWishlistButton,
					DisableAddToCompareListButton = coreProduct.DisableAddToCompareListButton
				};
			}

			//picture
			if (_viuSolrSearchSettings.PreparePictureModel)
			{
				model.DefaultPictureModel = new PictureModel
				{
					ThumbImageUrl = coreProduct.ThumbImageUrl,
					ImageUrl = coreProduct.DefaultImageUrl,
					FullSizeImageUrl = coreProduct.FullSizeImageUrl
					//AlternateText = coreProduct.ImageAltText, TODO: implement
					//Title = coreProduct.ImageAltText TODO: implement
				};			
			}

			//specs 
			if (_viuSolrSearchSettings.PrepareSpecificationAttributes)
			{
				var product = await _productService.GetProductByIdAsync(coreProduct.Id);
				model.ProductSpecificationModel = await _productModelFactory.PrepareProductSpecificationModelAsync(product);
			}

			return model;
		}
		
		private async Task<string> GetFacetDisplayName(string facetKey, int languageId)
		{
			switch (facetKey)
			{
				case ProductSolrDocument.SOLRFIELD_MULITVALUETEXT_EXTENSION + ProductSolrDocument.SOLRFIELD_ALLCATEGORIES:
					return await _localizationService.GetResourceAsync("filtering.categoryfilteredlabel", languageId);
				
				case ProductSolrDocument.SOLRFIELD_MULITVALUETEXT_EXTENSION + ProductSolrDocument.SOLRFIELD_ALLMANUFACTURERS:
					return await _localizationService.GetResourceAsync("filtering.manufacturersfilteredlabel", languageId);
			}

			var facetIdString = facetKey.Replace(ProductSolrDocument.SOLRFIELD_MULITVALUETEXT_EXTENSION, string.Empty).Replace("SA", string.Empty);

			int.TryParse(facetIdString, out var facetId);

			var spec = await _specificationAttributeService.GetSpecificationAttributeByIdAsync(facetId);
			
			return spec != null ? await _localizationService.GetLocalizedAsync(spec, b => b.Name, languageId) : string.Empty;
		}

		private static bool GetFilterActive(IList<KeyValuePair<string, List<string>>> facets, string facetName, string optionName)
		{
			var hasFacet = facets.Any(f => f.Key == facetName);

			if (hasFacet)
			{
				var facet = facets.FirstOrDefault(f => f.Key == facetName).Value;

				return facet.Contains(optionName);
			}

			return false;
		}
	}
}