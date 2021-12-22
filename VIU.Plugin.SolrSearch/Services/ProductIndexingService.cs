using System.Collections.Generic;
using System.Linq;
using SolrNet;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Logging;
using VIU.Plugin.SolrSearch.Models;
using VIU.Plugin.SolrSearch.Tools;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Core.Domain.Media;
using Nop.Services.Security;
using System.Threading.Tasks;
using Nop.Core.Events;
using VIU.Plugin.SolrSearch.Infrastructure;
using VIU.Plugin.SolrSearch.Settings;

namespace VIU.Plugin.SolrSearch.Services
{
    public class ProductIndexingService : IProductIndexingService
    {
        private readonly ISolrOperations<ProductSolrDocument> _solrOperations;
        private readonly ViuSolrSearchSettings _viuSolrSearchSettings;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IPermissionService _permissionService;
        private readonly CatalogSettings _catalogSettings;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly ICategoryService _categoryService;
        private readonly IPictureService _pictureService;
        private readonly MediaSettings _mediaSettings;
        private readonly ILanguageService _languageService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly ILogger _logger;
        private readonly IManufacturerService _manufacturerService;
        private readonly IEventPublisher _eventPublisher;

        public ProductIndexingService(ISolrOperations<ProductSolrDocument> solrOperations, ViuSolrSearchSettings viuSolrSearchSettings, IUrlRecordService urlRecordService, IPermissionService permissionService, CatalogSettings catalogSettings, ISpecificationAttributeService specificationAttributeService, ICategoryService categoryService, IPictureService pictureService, MediaSettings mediaSettings, ILanguageService languageService, ILocalizedEntityService localizedEntityService, ILogger logger, IProductService productService, IManufacturerService manufacturerService, IEventPublisher eventPublisher)
        {
	        _solrOperations = solrOperations;
	        _viuSolrSearchSettings = viuSolrSearchSettings;
	        _urlRecordService = urlRecordService;
	        _permissionService = permissionService;
	        _catalogSettings = catalogSettings;
	        _specificationAttributeService = specificationAttributeService;
	        _categoryService = categoryService;
	        _pictureService = pictureService;
	        _mediaSettings = mediaSettings;
	        _languageService = languageService;
	        _localizedEntityService = localizedEntityService;
	        _logger = logger;
	        _manufacturerService = manufacturerService;
	        _eventPublisher = eventPublisher;
        }

        public async Task<string> ReindexAllProducts(IList<Product> products)
        {

            var message = "Solr Product Indexing Service: found " + products.Count + " Products";
            await _logger.InformationAsync(message);

            ClearIndex();
            
            foreach (var product in products)
            {
                var psd = await ConvertProductToSolrDocument(product);

                await _solrOperations.AddAsync(psd);
                await _solrOperations.CommitAsync();
            }
            
            return message;
        }

        public async Task<string> AddOrUpdateProduct(Product product)
        {
            var psd = await ConvertProductToSolrDocument(product);

            await _solrOperations.AddAsync(psd);
            await _solrOperations.CommitAsync();
            
            var message = $"Solr Product Indexing Service: updated product \"{product.Name}\"";
            await _logger.InformationAsync(message);
            
            return message;
        }

        public async Task<string> DeleteProduct(Product product)
        {
            await _solrOperations.DeleteAsync(product.Id.ToString());
            await _solrOperations.CommitAsync();
            
            var message = $"Solr Product Indexing Service: removed product \"{product.Name}\"";
            await _logger.InformationAsync(message);
            
            return message;
        }

        public void ClearIndex()
        {
            _solrOperations.Delete(new SolrQuery("*:*"));
            _solrOperations.Commit();
        }

        private async Task<ProductSolrDocument> ConvertProductToSolrDocument(Product product)
        {
            var defaultLanguage = _viuSolrSearchSettings.DefaultLanguage ?? "en";

            //get categories
            var categories = (await _categoryService.GetProductCategoriesByProductIdAsync(product.Id)).Select(pc => pc.CategoryId.ToString()).ToList();

            //get product picture
            var productPicture = (await _pictureService.GetPicturesByProductIdAsync(product.Id, 1)).FirstOrDefault();
            
            //get manufacturers
            var manufacturers = (await _manufacturerService.GetProductManufacturersByProductIdAsync(product.Id)).Select(mf => mf.ManufacturerId.ToString()).ToList();

            //new Solr Document
            var psd = new ProductSolrDocument
            {
                //default fields
                Id = product.Id,
                Sku = product.Sku,
                Gtin = product.Gtin,
                ProductType = product.ProductType.ToString(),
                Manufacturer = product.ManufacturerPartNumber,
                DefaultImageUrl = (await _pictureService.GetPictureUrlAsync(productPicture, _mediaSettings.ProductDetailsPictureSize)).Url,
                ThumbImageUrl = (await _pictureService.GetPictureUrlAsync(productPicture, _mediaSettings.AutoCompleteSearchThumbPictureSize)).Url,
                FullSizeImageUrl = (await _pictureService.GetPictureUrlAsync(productPicture)).Url,
                DisableBuyButton = product.DisableBuyButton ||
                                      !await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableShoppingCart) ||
                                      !await _permissionService.AuthorizeAsync(StandardPermissionProvider.DisplayPrices),
                DisableWishlistButton = product.DisableWishlistButton ||
                                           !await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableWishlist) ||
                                           !await _permissionService.AuthorizeAsync(StandardPermissionProvider.DisplayPrices),
                DisableAddToCompareListButton = !_catalogSettings.CompareProductsEnabled,
                AllCategories = categories,
                AllManufacturers = manufacturers
            };

            //all other fields
            var otherFields = new Dictionary<string, object>
            {
                //default fields
                [SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_SE_NAME, defaultLanguage, true)] = await _urlRecordService.GetSeNameAsync(product, 0),
                [SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_NAME, defaultLanguage, true)] = product.Name,
                [SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_SHORTDESCRIPTION, defaultLanguage, true)] = product.ShortDescription,
                [SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_FULLDESCRIPTION, defaultLanguage, true)] = product.FullDescription
            };

            //all localized fields
            var languages = await _languageService.GetAllLanguagesAsync();

            foreach (var language in languages)
            {
                var languageKey = SolrTools.GetLanguageKey(language);

                var localizedSeName = await _urlRecordService.GetSeNameAsync(product, language.Id);
                otherFields.Add(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_SE_NAME, languageKey), localizedSeName);

                var localizedProductName = await _localizedEntityService.GetLocalizedValueAsync(language.Id, product.Id, nameof(Product), nameof(Product.Name));
                otherFields.Add(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_NAME, languageKey), localizedProductName);

                var localizedShortDescription = await _localizedEntityService.GetLocalizedValueAsync(language.Id, product.Id, nameof(Product), nameof(Product.ShortDescription));
                otherFields.Add(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_SHORTDESCRIPTION, languageKey), localizedShortDescription);

                //TODO remove html tags, configure HTMLStripCharFilter in Solr
                var localizedFullDescription = await _localizedEntityService.GetLocalizedValueAsync(language.Id, product.Id, nameof(Product), nameof(Product.FullDescription));
                otherFields.Add(SolrTools.GetLocalizedTextFieldName(ProductSolrDocument.SOLRFIELD_FULLDESCRIPTION, languageKey), localizedFullDescription);
            }

            //all product specification attributes 
            var psas = await _specificationAttributeService.GetProductSpecificationAttributesAsync(product.Id, 0, true);
            
            foreach (var psa in psas)
            {
                var sao = await _specificationAttributeService.GetSpecificationAttributeOptionByIdAsync(psa.SpecificationAttributeOptionId);
                var sa = await _specificationAttributeService.GetSpecificationAttributeByIdAsync(sao.SpecificationAttributeId);
                
                if (sa != null)
                {
                    //add specification attributes "readable"
                    var attributeNameField = ProductSolrDocument.SOLRFIELD_MULITVALUETEXT_EXTENSION + sa.Name;

                    if (otherFields.TryGetValue(attributeNameField, out object nameResult)) {
                        ((List<string>) nameResult).Add(sao.Name);
                    }
                    else
                    {
                        otherFields.Add(attributeNameField, new List<string> { sao.Name });
                    }

                    //add specification attributes based on IDs
                    attributeNameField = ProductSolrDocument.SOLRFIELD_MULITVALUETEXT_EXTENSION + "SA" + sa.Id;

                    if (otherFields.TryGetValue(attributeNameField, out object idResult))
                    {
                        ((List<int>) idResult).Add(sao.Id);
                    }
                    else
                    {
                        otherFields.Add(attributeNameField, new List<int> { sao.Id });
                    }
                }
            }

            psd.OtherFields = otherFields.Count > 0 ? otherFields : null;
            
            //raise event       
            await _eventPublisher.PublishAsync(new ProductIndexedEvent(psd, defaultLanguage));

            return psd;
        }
    }
}
