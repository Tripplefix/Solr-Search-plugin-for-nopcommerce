using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Services.Catalog;
using Nop.Core.Caching;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Plugin.SolrSearch.Areas.Admin.Models;
using Nop.Plugin.SolrSearch.Infrastructure;
using Nop.Plugin.SolrSearch.Settings;

namespace Nop.Plugin.SolrSearch.Areas.Admin.Controllers
{
    [Area(AreaNames.Admin)]
    public class SolrSearchSettingsController : BasePluginController
    {
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;
        private readonly INotificationService _notificationService;
        private readonly IProductService _productService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly IStaticCacheManager _staticCacheManager;

        public SolrSearchSettingsController(ISettingService settingService, ILocalizationService localizationService, IPermissionService permissionService, INotificationService notificationService, IProductService productService, IUrlRecordService urlRecordService, ISpecificationAttributeService specificationAttributeService, IStaticCacheManager staticCacheManager)
        {
	        _settingService = settingService;
	        _localizationService = localizationService;
	        _permissionService = permissionService;
	        _notificationService = notificationService;
	        _productService = productService;
	        _urlRecordService = urlRecordService;
	        _specificationAttributeService = specificationAttributeService;
	        _staticCacheManager = staticCacheManager;
        }

        [AuthorizeAdmin]
        public async Task<IActionResult> Configure()
        {
	        if (!await _permissionService.AuthorizeAsync(SolrPermissionProvider.ManageSearch))
	        {
		        return AccessDeniedView();
	        }
            
            var solrSearchSettings = await _settingService.LoadSettingAsync<SolrSearchSettings>();
            var model = solrSearchSettings.ToSettingsModel<SolrSearchSettingsModel>();

            model.AvailableFilterableSpecificationAttributes = (await _specificationAttributeService.GetSpecificationAttributesAsync()).Select(sa => new SelectListItem
            {
                Text = sa.Name,
                Value = sa.Id.ToString()
            }).ToList();

            if (!string.IsNullOrWhiteSpace(solrSearchSettings.SelectedFilterableSpecificationAttributeIds))
            {
                model.SelectedFilterableSpecificationAttributeIds = solrSearchSettings.SelectedFilterableSpecificationAttributeIds
                    .Split(',')
                    .Where(m => int.TryParse(m, out _))
                    .Select(int.Parse).ToList();
            }

            return View(model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        public async Task<IActionResult> Configure(SolrSearchSettingsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(SolrPermissionProvider.ManageSearch))
            {
                return AccessDeniedView();
            }

            var solrSearchSettings = await _settingService.LoadSettingAsync<SolrSearchSettings>();

            var heroProducts = solrSearchSettings.HeroProducts ?? string.Empty;

            model.ToSettings(solrSearchSettings);

            solrSearchSettings.HeroProducts = heroProducts;
            
            if (model.SelectedFilterableSpecificationAttributeIds != null)
            {
                solrSearchSettings.SelectedFilterableSpecificationAttributeIds = string.Join(',', model.SelectedFilterableSpecificationAttributeIds);
            }

            await _settingService.SaveSettingAsync(solrSearchSettings);
            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        #region Hero Products

        [HttpPost]
        public virtual async Task<IActionResult> ProductList(ProductSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(SolrPermissionProvider.ManageSearch))
                return await AccessDeniedDataTablesJson();
            
            var heroProductIds = (await GetHeroProductIds()).ToArray();

            var heroProducts = (await _productService.GetProductsByIdsAsync(heroProductIds)).ToPagedList(searchModel);
            
            var model = new HeroProductListModel().PrepareToGrid(searchModel, heroProducts, () =>
            {
                return heroProducts.Select((product, index) => new HeroProductModel
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    DisplayOrder = index
                });
            });

            return Json(model);
        }

        public virtual async Task<IActionResult> ProductDelete(int productId)
        {
            if (!await _permissionService.AuthorizeAsync(SolrPermissionProvider.ManageSearch))
                return AccessDeniedView();
            
            var heroProductIds = await GetHeroProductIds();
            heroProductIds.Remove(productId);
            await SaveHeroProductIds(heroProductIds);
            
            return new NullJsonResult();
        }

        public virtual async Task<IActionResult> ProductAddPopup()
        {
            if (!await _permissionService.AuthorizeAsync(SolrPermissionProvider.ManageSearch))
                return AccessDeniedView();

            var model = new AddProductsToHeroProductsSearchModel();

            model.SetPopupGridPageSize();

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> ProductAddPopupList(AddProductsToHeroProductsSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(SolrPermissionProvider.ManageSearch))
                return await AccessDeniedDataTablesJson();

            //prepare model
            //get products
            var products = await _productService.SearchProductsAsync(
                showHidden: false,
                keywords: searchModel.SearchProductName,
                pageIndex: searchModel.Page - 1, 
                pageSize: searchModel.PageSize);

            //prepare grid model
            var model = await new AddProductToHeroProductListModel().PrepareToGridAsync(searchModel, products, () =>
            {
                return products.SelectAwait(async product =>
                {
                    var productModel = product.ToModel<ProductModel>();
                    productModel.SeName = await _urlRecordService.GetSeNameAsync(product, 0, true, false);

                    return productModel;
                });
            });

            return Json(model);
        }

        [HttpPost]
        [FormValueRequired("save")]
        public virtual async Task<IActionResult> ProductAddPopup(AddProductToHeroProductsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(SolrPermissionProvider.ManageSearch))
                return AccessDeniedView();
            
            var heroProductIds = await GetHeroProductIds();
            heroProductIds.AddRange(model.SelectedProductIds);
            await SaveHeroProductIds(heroProductIds);
            
            ViewBag.RefreshPage = true;
            
            return View(new AddProductsToHeroProductsSearchModel());
        }        
        
        [HttpPost]
        public virtual async Task<IActionResult> MoveHeroProductUp(int id)
        {
            if (!await _permissionService.AuthorizeAsync(SolrPermissionProvider.ManageSearch))
                return AccessDeniedView();
            
            var heroProductIds = await GetHeroProductIds();

            if (heroProductIds.Contains(id))
            {
                var oldIndex = heroProductIds.IndexOf(id);

                var newIndex = oldIndex + 1;

                if (newIndex >= 0 && newIndex < heroProductIds.Count)
                {
                    heroProductIds.Remove(id);
                    heroProductIds.Insert(newIndex, id);

                    await SaveHeroProductIds(heroProductIds);
                }
            }
            
            // todo: there is probably a better solution, but I haven't found it, yet
            await _staticCacheManager.ClearAsync();
            
            return Json(new { result = true });
        }

        [HttpPost]
        public virtual async Task<IActionResult> MoveHeroProductDown(int id)
        {
            if (!await _permissionService.AuthorizeAsync(SolrPermissionProvider.ManageSearch))
                return AccessDeniedView();
            
            var heroProductIds = await GetHeroProductIds();

            if (heroProductIds.Contains(id))
            {
                var oldIndex = heroProductIds.IndexOf(id);

                var newIndex = oldIndex - 1;

                if (newIndex >= 0 && newIndex < heroProductIds.Count)
                {
                    heroProductIds.Remove(id);
                    heroProductIds.Insert(newIndex, id);

                    await SaveHeroProductIds(heroProductIds);
                }
            }
            
            // todo: there is probably a better solution, but I haven't found it, yet
            await _staticCacheManager.ClearAsync();
            
            return Json(new { result = true });
        }

        private async Task<List<int>> GetHeroProductIds()
        {
            var solrSearchSettings = await _settingService.LoadSettingAsync<SolrSearchSettings>();

            if (string.IsNullOrWhiteSpace(solrSearchSettings.HeroProducts))
                return new List<int>();
            
            return solrSearchSettings.HeroProducts
                .Split(',')
                .Where(m => int.TryParse(m, out _))
                .Select(int.Parse).ToList();
        }

        private async Task SaveHeroProductIds(IEnumerable<int> heroProductIds)
        {
            var solrSearchSettings = await _settingService.LoadSettingAsync<SolrSearchSettings>();
            
            solrSearchSettings.HeroProducts = string.Join(",", heroProductIds.Distinct());
            
            await _settingService.SaveSettingAsync(solrSearchSettings);
        }

        #endregion
    }
}
