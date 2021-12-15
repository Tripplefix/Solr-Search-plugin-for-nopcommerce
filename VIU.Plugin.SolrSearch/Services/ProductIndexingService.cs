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
        private readonly IProductService _productService;
        private readonly ILocalizationService _localizationService;

        public ProductIndexingService(ISolrOperations<ProductSolrDocument> solrOperations, ViuSolrSearchSettings viuSolrSearchSettings, IUrlRecordService urlRecordService, IPermissionService permissionService, CatalogSettings catalogSettings, ISpecificationAttributeService specificationAttributeService, ICategoryService categoryService, IPictureService pictureService, MediaSettings mediaSettings, ILanguageService languageService, ILocalizedEntityService localizedEntityService, ILogger logger, IProductService productService, ILocalizationService localizationService)
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
            _productService = productService;
            _localizationService = localizationService;
        }

        public async Task<string> ReindexAllProducts()
        {
            var selectedProducts = await _productService.SearchProductsAsync(visibleIndividuallyOnly: true);

            var message = "Solr Product Indexing Service: found " + selectedProducts.Count + " Products";
            await _logger.InformationAsync(message);

            foreach (var product in selectedProducts)
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

        public void Clear()
        {
            _solrOperations.Delete(new SolrQuery("*:*"));
            _solrOperations.Commit();
        }

        public async Task<Task> ReindexAllProductsTask()
        {
            Clear();
            var info = await ReindexAllProducts();
            
            await _logger.InformationAsync("Solr search: automatic reindexing of products: " + info);
            
            return Task.CompletedTask;
        }

        private async Task<ProductSolrDocument> ConvertProductToSolrDocument(Product product)
        {
            var defaultLanguage = _viuSolrSearchSettings.DefaultLanguage ?? "en";

            //get categories
            var pcs = await _categoryService.GetProductCategoriesByProductIdAsync(product.Id);

            var categories = await pcs
                .SelectAwait(async pc => await _categoryService.GetCategoryByIdAsync(pc.CategoryId))
                .Select(c => c.Id.ToString()).ToListAsync();

            //get product picture
            var productPicture = (await _pictureService.GetPicturesByProductIdAsync(product.Id, 1)).FirstOrDefault();


            var fullSizeImageUrl = string.Empty;
            var imageUrl = string.Empty;
            
            if (productPicture != null)
            {
                var picture = await _pictureService.GetPictureByIdAsync(productPicture.Id);

                (fullSizeImageUrl, picture) = await _pictureService.GetPictureUrlAsync(picture);
                (imageUrl, _) = await _pictureService.GetPictureUrlAsync(picture, _mediaSettings.ProductThumbPictureSize);
            }
            
            //new Solr Document
            var psd = new ProductSolrDocument
            {
                //default fields
                Id = product.Id,
                Sku = product.Sku,
                Gtin = product.Gtin,
                ProductType = product.ProductType.ToString(),
                Manufacturer = product.ManufacturerPartNumber,
                DefaultImageUrl = imageUrl,
                ThumbImageUrl = imageUrl,
                FullSizeImageUrl = fullSizeImageUrl,
                DisableBuyButton = product.DisableBuyButton ||
                                      !await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableShoppingCart) ||
                                      !await _permissionService.AuthorizeAsync(StandardPermissionProvider.DisplayPrices),
                DisableWishlistButton = product.DisableWishlistButton ||
                                           !await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableWishlist) ||
                                           !await _permissionService.AuthorizeAsync(StandardPermissionProvider.DisplayPrices),
                DisableAddToCompareListButton = !_catalogSettings.CompareProductsEnabled,
                AllCategories = categories
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

                if (sa == null) continue;
                
                foreach (var language in languages)
                {
                    var saNameLocalized = await _localizationService.GetLocalizedAsync(sa, entity => entity.Name, language.Id);
                        
                    //add specification attributes "readable"
                    var attributeNameField = ProductSolrDocument.SOLRFIELD_SPECIFICATION_ATTRIBUTE + sa.Id + "_title_" + SolrTools.GetLanguageKey(language);

                    if (otherFields.All(f => f.Key != attributeNameField))
                    {
                        otherFields.Add(attributeNameField, saNameLocalized);
                    }

                    var saoNameLocalized = await _localizationService.GetLocalizedAsync(sao, entity => entity.Name, language.Id);
                        
                    var attributeOptionsField = ProductSolrDocument.SOLRFIELD_SPECIFICATION_ATTRIBUTE + sa.Id + "_options_" + SolrTools.GetLanguageKey(language);
                        
                    if (otherFields.TryGetValue(attributeOptionsField, out var nameResult)) {
                        ((List<string>) nameResult).Add(saoNameLocalized);
                    }
                    else
                    {
                        otherFields.Add(attributeOptionsField, new List<string>
                        {
                            saoNameLocalized
                        });
                    }
                }
                        
                //add specification attributes based on IDs
                var attributeIdField = ProductSolrDocument.SOLRFIELD_SPECIFICATION_ATTRIBUTE + "SA" + sa.Id;

                if (otherFields.TryGetValue(attributeIdField, out object idResult))
                {
                    ((List<int>) idResult).Add(sao.Id);
                }
                else
                {
                    otherFields.Add(attributeIdField, new List<int> { sao.Id });
                }
            }

            psd.OtherFields = otherFields.Count > 0 ? otherFields : null;

            return psd;
        }
    }
}
