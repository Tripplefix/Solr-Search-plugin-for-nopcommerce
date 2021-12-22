using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Domain.Tasks;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Services.Tasks;
using Nop.Web.Framework.Menu;
using Nop.Plugin.SolrSearch.Infrastructure;
using Nop.Plugin.SolrSearch.Settings;
using Task = System.Threading.Tasks.Task;

namespace Nop.Plugin.SolrSearch
{
    public class SolrSearchPlugin : BasePlugin, IAdminMenuPlugin
    {
        private readonly ILocalizationService _localizationService;
        private readonly ICustomerService _customerService;
        private readonly IWorkContext _workContext;
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly ISettingService _settingService;
        private readonly IPermissionService _permissionService;

        public SolrSearchPlugin(ILocalizationService localizationService, ICustomerService customerService, IWorkContext workContext, IScheduleTaskService scheduleTaskService, ISettingService settingService, IPermissionService permissionService)
        {
	        _localizationService = localizationService;
	        _customerService = customerService;
	        _workContext = workContext;
	        _scheduleTaskService = scheduleTaskService;
	        _settingService = settingService;
	        _permissionService = permissionService;
        }

        public async Task ManageSiteMapAsync(SiteMapNode rootNode)
        {
            var menuItem = new SiteMapNode
            {
                SystemName = "Nop.Plugin.SolrSearch",
                Title = await _localizationService.GetResourceAsync("Nop.Plugin.SolrSearch.Menu.Top"),
                Visible = await _permissionService.AuthorizeAsync(SolrPermissionProvider.ManageSearch),
                IconClass = "fas fa-search"
            };
            
            var coreMenuItem = new SiteMapNode
            {
                SystemName = "Nop.Plugin.SolrSearch.GeneralSettings",
                Title = await _localizationService.GetResourceAsync("Nop.Plugin.SolrSearch.Menu.GeneralSettings"),
                ControllerName = "SolrSearchSettings",
                ActionName = "Configure",
                Visible = await _permissionService.AuthorizeAsync(SolrPermissionProvider.ManageSearch),
                RouteValues = new RouteValueDictionary
                {
                    {
                        "Area", "Admin"
                    }
                },
                IconClass = "far fa-dot-circle"
            };
            menuItem.ChildNodes.Add(coreMenuItem);
            
            rootNode.ChildNodes.Add(menuItem);
        }

        public override async Task InstallAsync()
        {
	        //permissions
	        var provider = (IPermissionProvider)Activator.CreateInstance(typeof(SolrPermissionProvider));
	        await _permissionService.InstallPermissionsAsync(provider);
	        
	        //locales
	        await _localizationService.AddLocaleResourceAsync(AdminLocales);
	        await _localizationService.AddLocaleResourceAsync(StoreFrontLocales);
	        
	        //schedule task
	        if (await _scheduleTaskService.GetTaskByTypeAsync(SolrSearchPluginDefaults.BuildIndexTaskName) == null)
	        {
		        await _scheduleTaskService.InsertTaskAsync(new ScheduleTask
		        {
			        Enabled = true,
			        Seconds = SolrSearchPluginDefaults.DefaultReindexPeriod * 60 * 60,
			        Name = SolrSearchPluginDefaults.BuildIndexTaskName,
			        Type = SolrSearchPluginDefaults.BuildIndexTask,
		        });
	        }
	        
	        //settings
	        await _settingService.SaveSettingAsync(SolrSettings);
	        
	        await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
	        //locales
	        await _localizationService.DeleteLocaleResourcesAsync("Nop.Plugin.SolrSearch");
	        
	        //schedule task
	        var task = await _scheduleTaskService.GetTaskByTypeAsync(SolrSearchPluginDefaults.BuildIndexTask);
	        if (task != null)
		        await _scheduleTaskService.DeleteTaskAsync(task);
	        
	        //settings
	        await _settingService.DeleteSettingAsync<SolrSearchSettings>();
	        
	        await base.UninstallAsync();
        }

        private static SolrSearchSettings SolrSettings => new()
        {
	        AllowEmptySearchQuery = false,
	        SelectedFilterableSpecificationAttributeIds = string.Empty,
	        EnableHeroProducts = false,
	        HeroProducts = string.Empty,
	        DefaultLanguage = "en",
	        IncludeSpecificationAttributesInFilter = true,
	        IncludeCategoriesInFilter = true,
	        IncludeManufacturersInFilter = true,
	        HideFacetOptionsWithNoCount = false,
	        MaxReturnedDocuments = 500,
	        HighlightingEnabled = false,
	        SpellcheckingEnabled = false,
	        SpellcheckingOnlyMorePopular = false,
	        PreparePriceModel = true,
	        PreparePictureModel = true,
	        PrepareSpecificationAttributes = false,
	        ProductNameQueryBoost = 10,
	        ProductSkuQueryBoost = 10,
	        ProductGtinQueryBoost = 10,
	        WildcardQueryEnabled = false,
	        WildcardQueryBoost = 8,
	        WildcardQueryMinLength = 4,
	        WildcardQuerySelectedType = SolrSearchSettings.WildcardQueryType.PrefixAndPostfix,
	        FuzzyQueryEnabled = false,
	        FuzzyQueryBoost = 6,
	        FuzzyQueryFuzziness = 2,
	        FuzzyQueryMinLength = 3,
	        PhraseQueryEnabled = false,
	        PhraseQueryBoost = 8,
	        PhraseQueryProximity = 2,
        };
        
        private static Dictionary<string, string> StoreFrontLocales => new()
        {
	        ["Nop.Plugin.SolrSearch.ResultPage.FilterTitle"] = "Filter",
	        ["Nop.Plugin.SolrSearch.ResultPage.FilterReset"] = "Zurücksetzen",
	        ["Nop.Plugin.SolrSearch.ResultPage.FilterClose"] = "Filter schliessen",
	        ["Nop.Plugin.SolrSearch.ResultPage.Title"] = "Dermaplast Sortiment",
	        ["Nop.Plugin.SolrSearch.ResultPage.ShowMore"] = "Mehr anzeigen",
	        ["Nop.Plugin.SolrSearch.ResultPage.ResultsFor"] = "Suchergebnisse für “{0}“",
	        ["Nop.Plugin.SolrSearch.ResultPage.ProductsWithCount"] = "{0}",
        };
        
        private static Dictionary<string, string> AdminLocales => new()
        {
	        ["Nop.Plugin.SolrSearch.Admin.Setting.FilterableSpecificationAttributes"] = "Specification Attributes displayed in filter",
            ["Nop.Plugin.SolrSearch.Admin.Setting.FilterableSpecificationAttributes.Hint"] = "Specification Attributes displayed in filter",
            ["Nop.Plugin.SolrSearch.Admin.Setting.AllowEmptySearchQuery"] = "Allow empty search queries",
            ["Nop.Plugin.SolrSearch.Admin.Setting.AllowEmptySearchQuery.Hint"] = "Allow empty search queries",
            ["Nop.Plugin.SolrSearch.Admin.Setting.DefaultLanguage"] = "Default language",
            ["Nop.Plugin.SolrSearch.Admin.Setting.DefaultLanguage.Hint"] = "Default Language",
            ["Nop.Plugin.SolrSearch.Admin.Setting.PreparePriceModel"] = "Prepare Price Model",
            ["Nop.Plugin.SolrSearch.Admin.Setting.PreparePriceModel.Hint"] = "Prepare Price Model",
            ["Nop.Plugin.SolrSearch.Admin.Setting.PreparePictureModel"] = "Prepare Picture Model",
            ["Nop.Plugin.SolrSearch.Admin.Setting.PreparePictureModel.Hint"] = "Prepare Picture Model",
            ["Nop.Plugin.SolrSearch.Admin.Setting.PrepareSpecificationAttributes"] = "Prepare Specification Attributes",
            ["Nop.Plugin.SolrSearch.Admin.Setting.PrepareSpecificationAttributes.Hint"] = "Prepare Specification Attributes",
            ["Nop.Plugin.SolrSearch.Admin.Setting.IncludeCategoriesInFilter"] = "Include categories in filter",
            ["Nop.Plugin.SolrSearch.Admin.Setting.IncludeCategoriesInFilter.Hint"] = "Includes categories in the filter on the search result page",
            ["Nop.Plugin.SolrSearch.Admin.Setting.IncludeSpecificationAttributesInFilter"] = "Include Specification Attributes In Filter",
            ["Nop.Plugin.SolrSearch.Admin.Setting.IncludeSpecificationAttributesInFilter.Hint"] = "Include Specification Attributes In Filter",
            ["Nop.Plugin.SolrSearch.Admin.Settings.SpecificationAttributes.NoAttributes"] = "No attributes available.",
            ["Nop.Plugin.SolrSearch.Admin.Setting.EnableHeroProducts"] = "Enable Hero Products",
            ["Nop.Plugin.SolrSearch.Admin.Setting.EnableHeroProducts.Hint"] = "Enable Hero Products",
            ["Nop.Plugin.SolrSearch.Admin.Setting.HeroProducts"] = "Hero Products comma-separated",
            ["Nop.Plugin.SolrSearch.Admin.Setting.HeroProducts.Table.Product"] = "Product Name",
            ["Nop.Plugin.SolrSearch.Admin.Setting.HeroProducts.Table.DisplayOrder"] = "Display Order",
            ["Nop.Plugin.SolrSearch.Admin.Setting.HeroProducts.Table.MoveUp"] = "Down", //👇
            ["Nop.Plugin.SolrSearch.Admin.Setting.HeroProducts.Table.MoveDown"] = "Up", //👆
            ["Nop.Plugin.SolrSearch.Admin.Setting.HeroProducts.Table.AddNew"] = "Add new product",
            ["Nop.Plugin.SolrSearch.Admin.Setting.IncludeManufacturersInFilter"] = "Include manufacturers in filter",
            ["Nop.Plugin.SolrSearch.Admin.Setting.IncludeManufacturersInFilter.Hint"] = "Include manufacturers in filter",
            ["Nop.Plugin.SolrSearch.Admin.Setting.HideFacetOptionsWithNoCount"] = "Hide facet options with no count",
            ["Nop.Plugin.SolrSearch.Admin.Setting.HideFacetOptionsWithNoCount.Hint"] = "Hide facet options with no count",
            ["Nop.Plugin.SolrSearch.Admin.Setting.MaxReturnedDocuments"] = "Max number of documents returned",
            ["Nop.Plugin.SolrSearch.Admin.Setting.MaxReturnedDocuments.Hint"] = "Max number of documents returned",
            ["Nop.Plugin.SolrSearch.Admin.Setting.HighlightingEnabled"] = "Highlighting enabled",
            ["Nop.Plugin.SolrSearch.Admin.Setting.HighlightingEnabled.Hint"] = "Highlighting enabled",
            ["Nop.Plugin.SolrSearch.Admin.Setting.SpellcheckingEnabled"] = "Spellchecking Enabled",
            ["Nop.Plugin.SolrSearch.Admin.Setting.SpellcheckingEnabled.Hint"] = "Spellchecking Enabled",
            ["Nop.Plugin.SolrSearch.Admin.Setting.SpellcheckingOnlyMorePopular"] = "Spellchecking Only More Popular",
            ["Nop.Plugin.SolrSearch.Admin.Setting.SpellcheckingOnlyMorePopular.Hint"] = "Spellchecking Only More Popular",
            ["Nop.Plugin.SolrSearch.Admin.Setting.ProductNameQueryBoost"] = "Product Name Query Boost",
            ["Nop.Plugin.SolrSearch.Admin.Setting.ProductNameQueryBoost.Hint"] = "Product Name Query Boost",
            ["Nop.Plugin.SolrSearch.Admin.Setting.ProductShortDescriptionQueryBoost"] = "Product Short Description Query Boost",
            ["Nop.Plugin.SolrSearch.Admin.Setting.ProductShortDescriptionQueryBoost.Hint"] = "Product Short Description Query Boost",
            ["Nop.Plugin.SolrSearch.Admin.Setting.ProductFullDescriptionQueryBoost"] = "Product Full Description Query Boost",
            ["Nop.Plugin.SolrSearch.Admin.Setting.ProductFullDescriptionQueryBoost.Hint"] = "Product Full Description Query Boost",
            ["Nop.Plugin.SolrSearch.Admin.Setting.ProductSkuQueryBoost"] = "Product Sku Query Boost",
            ["Nop.Plugin.SolrSearch.Admin.Setting.ProductSkuQueryBoost.Hint"] = "Product Sku Query Boost",
            ["Nop.Plugin.SolrSearch.Admin.Setting.ProductGtinQueryBoost"] = "Product Gtin Query Boost",
            ["Nop.Plugin.SolrSearch.Admin.Setting.ProductGtinQueryBoost.Hint"] = "Product Gtin Query Boost",
            ["Nop.Plugin.SolrSearch.Admin.Setting.WildcardQueryEnabled"] = "Wildcard Query Enabled",
            ["Nop.Plugin.SolrSearch.Admin.Setting.WildcardQueryEnabled.Hint"] = "Wildcard Query Enabled",
            ["Nop.Plugin.SolrSearch.Admin.Setting.WildcardQueryBoost"] = "Wildcard Query Boost",
            ["Nop.Plugin.SolrSearch.Admin.Setting.WildcardQueryBoost.Hint"] = "Wildcard Query Boost",
            ["Nop.Plugin.SolrSearch.Admin.Setting.WildcardQueryMinLength"] = "Wildcard Query Min Length",
            ["Nop.Plugin.SolrSearch.Admin.Setting.WildcardQueryMinLength.Hint"] = "Wildcard Query Min Length",
            ["Nop.Plugin.SolrSearch.Admin.Setting.WildcardQuerySelectedType"] = "Wildcard Query Selected Type",
            ["Nop.Plugin.SolrSearch.Admin.Setting.WildcardQuerySelectedType.Hint"] = "Wildcard Query Selected Type",
            ["Nop.Plugin.SolrSearch.Admin.Setting.FuzzyQueryEnabled"] = "Fuzzy Query Enabled",
            ["Nop.Plugin.SolrSearch.Admin.Setting.FuzzyQueryEnabled.Hint"] = "Fuzzy Query Enabled",
            ["Nop.Plugin.SolrSearch.Admin.Setting.FuzzyQueryBoost"] = "Fuzzy Query Boost",
            ["Nop.Plugin.SolrSearch.Admin.Setting.FuzzyQueryBoost.Hint"] = "Fuzzy Query Boost",
            ["Nop.Plugin.SolrSearch.Admin.Setting.FuzzyQueryFuzziness"] = "Fuzzy Query Fuzziness",
            ["Nop.Plugin.SolrSearch.Admin.Setting.FuzzyQueryFuzziness.Hint"] = "Fuzzy Query Fuzziness",
            ["Nop.Plugin.SolrSearch.Admin.Setting.FuzzyQueryMinLength"] = "Fuzzy Query Min Length",
            ["Nop.Plugin.SolrSearch.Admin.Setting.FuzzyQueryMinLength.Hint"] = "Fuzzy Query Min Length",
            ["Nop.Plugin.SolrSearch.Admin.Setting.PhraseQueryEnabled"] = "Phrase Query Enabled",
            ["Nop.Plugin.SolrSearch.Admin.Setting.PhraseQueryEnabled.Hint"] = "Phrase Query Enabled",
            ["Nop.Plugin.SolrSearch.Admin.Setting.PhraseQueryBoost"] = "Phrase Query Boost",
            ["Nop.Plugin.SolrSearch.Admin.Setting.PhraseQueryBoost.Hint"] = "Phrase Query Boost",
            ["Nop.Plugin.SolrSearch.Admin.Setting.PhraseQueryProximity"] = "Phrase Query Proximity",
            ["Nop.Plugin.SolrSearch.Admin.Setting.PhraseQueryProximity.Hint"] = "Phrase Query Proximity",
            
            ["Nop.Plugin.SolrSearch.Menu.Top"] = "Solr Search",
            ["Nop.Plugin.SolrSearch.Menu.GeneralSettings"] = "SolrSearch General Settings",
            ["Nop.Plugin.SolrSearch.Admin.Settings.Title"] = "SolrSearch General Settings",
            ["Nop.Plugin.SolrSearch.Admin.TabTitle.SolrSettings"] = "Solr Settings",
            ["Nop.Plugin.SolrSearch.Admin.TabTitle.Boosting"] = "Boosting",
            ["Nop.Plugin.SolrSearch.Admin.TabTitle.HeroProducts"] = "Hero Products",
            ["Nop.Plugin.SolrSearch.Admin.BoostingNameFieldInfo"] = "The product-name fields can be configured with the following settings:",
        };
    }
}