using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Logging;
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

		public SolrSearchFactory(IProductSearchService productSearchService,
			IWorkContext workContext,
			ILocalizationService localizationService,
			ILanguageService languageService,
			IStoreContext storeContext,
			ICategoryService categoryService,
			ISpecificationAttributeService specificationAttributeService, 
			ILogger logger,
			ViuSolrSearchSettings viuSolrSearchSettings)
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

					return new
					{
						id = specAttribute.Id
					};
				}).ToListAsync();
			
			if (specificationAttributes.Any())
			{				
				availableFacetList.AddRange(specificationAttributes.Select(spec => ProductSolrDocument.SOLRFIELD_PREFIX_SPECIFICATION_ATTRIBUTES + spec.id));
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
			var products = searchResult.Select(coreProduct => PrepareProductOverviewModel(coreProduct, languageKey, prepareSpecificationAttributes: true))
				.Where(p => p.Id != 0)
				.ToList();
			
			// default sort by sku
			products = products.OrderBy(product => product.Sku).ToList();
			
			// hero products
			HandleHeroProducts(products);

			model.Products = products;

			//enrich the result: facet part
			var availableFacets = await searchResult.FacetFields
				.SelectAwait(async coreFacet =>
				{
					var (facetKey, facetValues) = coreFacet;
					var facetName = facetKey.Replace(ProductSolrDocument.SOLRFIELD_SPECIFICATION_ATTRIBUTE, string.Empty);
					
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
								facetValue.OptionName = optionName;
								facetValue.OptionProductCount = productCount;

								return facetValue;
							})
							.Where(f => f != null)
							.OrderBy(f => f.DisplayOrder)
							.ToListAsync()
					};
				})
				.ToListAsync();

			if (!string.IsNullOrWhiteSpace(_viuSolrSearchSettings.SelectedFilterableSpecificationAttributeIds))
			{
				availableFacets = availableFacets.Where(facet => _viuSolrSearchSettings.SelectedFilterableSpecificationAttributeIds
						.Split(',')
						.Select(id => $"{ProductSolrDocument.SOLRFIELD_PREFIX_SPECIFICATION_ATTRIBUTES}{id}")
						.Contains(facet.FacetName) || _viuSolrSearchSettings.IncludeCategoriesInFilter && facet.FacetName == ProductSolrDocument.SOLRFIELD_ALLCATEGORIES)
					.ToList();
			}

			model.ProductFacets = availableFacets;

			model.NoResults = !model.Products.Any();

			return model;
		}

		private async Task<ProductFacet.FacetValue> PrepareFacetValue(string facetKey, string optionName, int languageId)
		{
			int.TryParse(optionName, out var optionId);

			string facetDisplayName;
			int facetDisplayOrder;

			if (ProductSolrDocument.SOLRFIELD_SPECIFICATION_ATTRIBUTE + ProductSolrDocument.SOLRFIELD_ALLCATEGORIES == facetKey)
			{
				var category = await _categoryService.GetCategoryByIdAsync(optionId);

				if (category == null) return null;
				
				facetDisplayName = category.Name;
				facetDisplayOrder = category.DisplayOrder;
			}
			else
			{
				var specOption = await _specificationAttributeService.GetSpecificationAttributeOptionByIdAsync(optionId);
			
				if (specOption == null) return null;
			
				facetDisplayName = await _localizationService.GetLocalizedAsync(specOption, b => b.Name, languageId);
				facetDisplayOrder = specOption.DisplayOrder;
			}
			
			return new ProductFacet.FacetValue
			{
				OptionDisplayName = facetDisplayName,
				DisplayOrder = facetDisplayOrder,
			};
		}

		public ProductOverviewModel PrepareProductOverviewModel(ProductSolrDocument coreProduct, 
			string languageKey, 
			bool preparePriceModel = true, 
			bool preparePictureModel = true,
			bool prepareSpecificationAttributes = false)
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
			if (preparePriceModel)
			{
				model.ProductPrice = new ProductOverviewModel.ProductPriceModel
				{
					DisableBuyButton = coreProduct.DisableBuyButton,
					DisableWishlistButton = coreProduct.DisableWishlistButton,
					DisableAddToCompareListButton = coreProduct.DisableAddToCompareListButton
				};
			}

			//picture
			if (preparePictureModel)
			{
				model.DefaultPictureModel = new PictureModel
				{
					ImageUrl = coreProduct.ThumbImageUrl,
					//AlternateText = coreProduct.ImageAltText, TODO: implement
					//Title = coreProduct.ImageAltText TODO: implement
				};			
			}

			//specs
			if (prepareSpecificationAttributes)
			{
				model.ProductSpecificationModel = PrepareProductSpecificationModel(coreProduct.OtherFields
					.Where(f => f.Key.StartsWith(ProductSolrDocument.SOLRFIELD_SPECIFICATION_ATTRIBUTE) && 
					            f.Key.Contains(ProductSolrDocument.SOLRFIELD_SPECIFICATION_ATTRIBUTE_OPTIONS) && 
					            f.Key.EndsWith(defaultLanguage)));
			}

			return model;
		}
		
		private ProductSpecificationModel PrepareProductSpecificationModel(IEnumerable<KeyValuePair<string, object>> attributes)
		{
			var specificationModel = new ProductSpecificationModel();
			var attributeModel = new List<ProductSpecificationAttributeValueModel>();
	        
	        foreach (var (attributeKey, attributeValues) in attributes)
	        {
		        var attributeId = Regex.Match(attributeKey, @"\d+").Value;

		        var attributeList = new List<string>();
		        attributeList.AddRange(((ArrayList)attributeValues).Cast<string>());
		        
		        attributeModel.AddRange(attributeList.Select(attributeValue => new ProductSpecificationAttributeValueModel
		        {
			        ValueRaw = attributeValue
		        }).ToList());
		        
		        specificationModel.Groups.Add(new ProductSpecificationAttributeGroupModel
		        {
			        Id = int.Parse(attributeId),
			        Name = "SA" + attributeId,
			        Attributes = new List<ProductSpecificationAttributeModel>{ new() { Values = attributeModel } },
		        });
	        }
			

	        return specificationModel;

	        /*
            return _specificationAttributeService.GetProductSpecificationAttributes(productId, 0, null, true)
                .Select(psa =>
                {
                    var specAttributeOption =
                        _specificationAttributeService.GetSpecificationAttributeOptionById(
                            psa.SpecificationAttributeOptionId);
                    var specAttribute =
                        _specificationAttributeService.GetSpecificationAttributeById(specAttributeOption
                            .SpecificationAttributeId);

                    var m = new ProductSpecificationModel
                    {
                        SpecificationAttributeId = specAttribute.Id,
                        SpecificationAttributeName = _localizationService.GetLocalized(specAttribute, x => x.Name),
                        ColorSquaresRgb = specAttributeOption.ColorSquaresRgb,
                        AttributeTypeId = psa.AttributeTypeId
                    };

                    switch (psa.AttributeType)
                    {
                        case SpecificationAttributeType.Option:
                            m.ValueRaw =
                                WebUtility.HtmlEncode(
                                    _localizationService.GetLocalized(specAttributeOption, x => x.Name));
                            break;
                        case SpecificationAttributeType.CustomText:
                            m.ValueRaw =
                                WebUtility.HtmlEncode(_localizationService.GetLocalized(psa, x => x.CustomValue));
                            break;
                        case SpecificationAttributeType.CustomHtmlText:
                            m.ValueRaw = _localizationService.GetLocalized(psa, x => x.CustomValue);
                            break;
                        case SpecificationAttributeType.Hyperlink:
                            m.ValueRaw = $"<a href='{psa.CustomValue}' target='_blank'>{psa.CustomValue}</a>";
                            break;
                        default:
                            break;
                    }

                    return m;
                }).ToList();*/
		}
		
		private async Task<string> GetFacetDisplayName(string facetKey, int languageId)
		{
			if (ProductSolrDocument.SOLRFIELD_SPECIFICATION_ATTRIBUTE + ProductSolrDocument.SOLRFIELD_ALLCATEGORIES == facetKey)
				return await _localizationService.GetResourceAsync("filtering.categoryfilteredlabel", languageId);
		
			var facetIdString = facetKey.Replace(ProductSolrDocument.SOLRFIELD_SPECIFICATION_ATTRIBUTE, string.Empty).Replace("SA", string.Empty);

			int.TryParse(facetIdString, out var facetId);

			var spec = await _specificationAttributeService.GetSpecificationAttributeByIdAsync(facetId);
			
			return spec != null ? await _localizationService.GetLocalizedAsync(spec, b => b.Name, languageId) : string.Empty;
		}

		private static bool GetFilterActive(IList<KeyValuePair<string, List<string>>> facets, string facetName, string optionName)
		{
			var hasFacet = facets.Any(f => f.Key == facetName);

			if (!hasFacet) return false;
			
			var facet = facets.FirstOrDefault(f => f.Key == facetName).Value;

			return facet.Contains(optionName);
		}

		private void HandleHeroProducts(List<ProductOverviewModel> results)
		{
			var heroProducts = _viuSolrSearchSettings.HeroProducts;

			if (string.IsNullOrWhiteSpace(heroProducts))
				return;

			var heroProductList = heroProducts.Split(',').Where(m => int.TryParse(m, out _)).Select(int.Parse).Reverse().ToList();
			
			foreach (var hpId in heroProductList)
			{
				var index = results.FindIndex(item => item.Id == hpId);

				if (index <= 0) continue;
				
				var item = results[index];
				
				for (var i = index; i > 0; i--)
				{
					results[i] = results[i - 1];
				}
				
				results[0] = item;
			}
		}
	}
}