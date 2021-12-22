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
using VIU.Plugin.SolrSearch.Infrastructure;
using VIU.Plugin.SolrSearch.Settings;
using Task = System.Threading.Tasks.Task;

namespace VIU.Plugin.SolrSearch
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
                SystemName = "VIU.Plugin.SolrSearch",
                Title = await _localizationService.GetResourceAsync("VIU.Plugin.SolrSearch.Menu.Top"),
                Visible = await _permissionService.AuthorizeAsync(SolrPermissionProvider.ManageSearch),
                IconClass = "fas fa-search"
            };
            
            var coreMenuItem = new SiteMapNode
            {
                SystemName = "VIU.Plugin.SolrSearch.GeneralSettings",
                Title = await _localizationService.GetResourceAsync("VIU.Plugin.SolrSearch.Menu.GeneralSettings"),
                ControllerName = "ViuSolrSearchSettings",
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
	        await _localizationService.DeleteLocaleResourcesAsync("VIU.Plugin.SolrSearch");
	        
	        //schedule task
	        var task = await _scheduleTaskService.GetTaskByTypeAsync(SolrSearchPluginDefaults.BuildIndexTask);
	        if (task != null)
		        await _scheduleTaskService.DeleteTaskAsync(task);
	        
	        //settings
	        await _settingService.DeleteSettingAsync<ViuSolrSearchSettings>();
	        
	        await base.UninstallAsync();
        }

        private static ViuSolrSearchSettings SolrSettings => new()
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
	        WildcardQuerySelectedType = ViuSolrSearchSettings.WildcardQueryType.PrefixAndPostfix,
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
	        ["VIU.Plugin.SolrSearch.ResultPage.FilterTitle"] = "Filter",
	        ["VIU.Plugin.SolrSearch.ResultPage.FilterReset"] = "Zurücksetzen",
	        ["VIU.Plugin.SolrSearch.ResultPage.FilterClose"] = "Filter schliessen",
	        ["VIU.Plugin.SolrSearch.ResultPage.Title"] = "Dermaplast Sortiment",
	        ["VIU.Plugin.SolrSearch.ResultPage.ShowMore"] = "Mehr anzeigen",
	        ["VIU.Plugin.SolrSearch.ResultPage.ResultsFor"] = "Suchergebnisse für “{0}“",
	        ["VIU.Plugin.SolrSearch.ResultPage.ProductsWithCount"] = "{0}",
        };
        
        private static Dictionary<string, string> AdminLocales => new()
        {
	        ["VIU.Plugin.SolrSearch.Admin.Setting.FilterableSpecificationAttributes"] = "Specification Attributes displayed in filter",
            ["VIU.Plugin.SolrSearch.Admin.Setting.FilterableSpecificationAttributes.Hint"] = "Specification Attributes displayed in filter",
            ["VIU.Plugin.SolrSearch.Admin.Setting.AllowEmptySearchQuery"] = "Allow empty search queries",
            ["VIU.Plugin.SolrSearch.Admin.Setting.AllowEmptySearchQuery.Hint"] = "Allow empty search queries",
            ["VIU.Plugin.SolrSearch.Admin.Setting.DefaultLanguage"] = "Default language",
            ["VIU.Plugin.SolrSearch.Admin.Setting.DefaultLanguage.Hint"] = "Default Language",
            ["VIU.Plugin.SolrSearch.Admin.Setting.PreparePriceModel"] = "Prepare Price Model",
            ["VIU.Plugin.SolrSearch.Admin.Setting.PreparePriceModel.Hint"] = "Prepare Price Model",
            ["VIU.Plugin.SolrSearch.Admin.Setting.PreparePictureModel"] = "Prepare Picture Model",
            ["VIU.Plugin.SolrSearch.Admin.Setting.PreparePictureModel.Hint"] = "Prepare Picture Model",
            ["VIU.Plugin.SolrSearch.Admin.Setting.PrepareSpecificationAttributes"] = "Prepare Specification Attributes",
            ["VIU.Plugin.SolrSearch.Admin.Setting.PrepareSpecificationAttributes.Hint"] = "Prepare Specification Attributes",
            ["VIU.Plugin.SolrSearch.Admin.Setting.IncludeCategoriesInFilter"] = "Include categories in filter",
            ["VIU.Plugin.SolrSearch.Admin.Setting.IncludeCategoriesInFilter.Hint"] = "Includes categories in the filter on the search result page",
            ["VIU.Plugin.SolrSearch.Admin.Setting.IncludeSpecificationAttributesInFilter"] = "Include Specification Attributes In Filter",
            ["VIU.Plugin.SolrSearch.Admin.Setting.IncludeSpecificationAttributesInFilter.Hint"] = "Include Specification Attributes In Filter",
            ["VIU.Plugin.SolrSearch.Admin.Settings.SpecificationAttributes.NoAttributes"] = "No attributes available.",
            ["VIU.Plugin.SolrSearch.Admin.Setting.EnableHeroProducts"] = "Enable Hero Products",
            ["VIU.Plugin.SolrSearch.Admin.Setting.EnableHeroProducts.Hint"] = "Enable Hero Products",
            ["VIU.Plugin.SolrSearch.Admin.Setting.HeroProducts"] = "Hero Products comma-separated",
            ["VIU.Plugin.SolrSearch.Admin.Setting.HeroProducts.Table.Product"] = "Product Name",
            ["VIU.Plugin.SolrSearch.Admin.Setting.HeroProducts.Table.DisplayOrder"] = "Display Order",
            ["VIU.Plugin.SolrSearch.Admin.Setting.HeroProducts.Table.MoveUp"] = "Down", //👇
            ["VIU.Plugin.SolrSearch.Admin.Setting.HeroProducts.Table.MoveDown"] = "Up", //👆
            ["VIU.Plugin.SolrSearch.Admin.Setting.HeroProducts.Table.AddNew"] = "Add new product",
            ["VIU.Plugin.SolrSearch.Admin.Setting.IncludeManufacturersInFilter"] = "Include manufacturers in filter",
            ["VIU.Plugin.SolrSearch.Admin.Setting.IncludeManufacturersInFilter.Hint"] = "Include manufacturers in filter",
            ["VIU.Plugin.SolrSearch.Admin.Setting.HideFacetOptionsWithNoCount"] = "Hide facet options with no count",
            ["VIU.Plugin.SolrSearch.Admin.Setting.HideFacetOptionsWithNoCount.Hint"] = "Hide facet options with no count",
            ["VIU.Plugin.SolrSearch.Admin.Setting.MaxReturnedDocuments"] = "Max number of documents returned",
            ["VIU.Plugin.SolrSearch.Admin.Setting.MaxReturnedDocuments.Hint"] = "Max number of documents returned",
            ["VIU.Plugin.SolrSearch.Admin.Setting.HighlightingEnabled"] = "Highlighting enabled",
            ["VIU.Plugin.SolrSearch.Admin.Setting.HighlightingEnabled.Hint"] = "Highlighting enabled",
            ["VIU.Plugin.SolrSearch.Admin.Setting.SpellcheckingEnabled"] = "Spellchecking Enabled",
            ["VIU.Plugin.SolrSearch.Admin.Setting.SpellcheckingEnabled.Hint"] = "Spellchecking Enabled",
            ["VIU.Plugin.SolrSearch.Admin.Setting.SpellcheckingOnlyMorePopular"] = "Spellchecking Only More Popular",
            ["VIU.Plugin.SolrSearch.Admin.Setting.SpellcheckingOnlyMorePopular.Hint"] = "Spellchecking Only More Popular",
            ["VIU.Plugin.SolrSearch.Admin.Setting.ProductNameQueryBoost"] = "Product Name Query Boost",
            ["VIU.Plugin.SolrSearch.Admin.Setting.ProductNameQueryBoost.Hint"] = "Product Name Query Boost",
            ["VIU.Plugin.SolrSearch.Admin.Setting.ProductShortDescriptionQueryBoost"] = "Product Short Description Query Boost",
            ["VIU.Plugin.SolrSearch.Admin.Setting.ProductShortDescriptionQueryBoost.Hint"] = "Product Short Description Query Boost",
            ["VIU.Plugin.SolrSearch.Admin.Setting.ProductFullDescriptionQueryBoost"] = "Product Full Description Query Boost",
            ["VIU.Plugin.SolrSearch.Admin.Setting.ProductFullDescriptionQueryBoost.Hint"] = "Product Full Description Query Boost",
            ["VIU.Plugin.SolrSearch.Admin.Setting.ProductSkuQueryBoost"] = "Product Sku Query Boost",
            ["VIU.Plugin.SolrSearch.Admin.Setting.ProductSkuQueryBoost.Hint"] = "Product Sku Query Boost",
            ["VIU.Plugin.SolrSearch.Admin.Setting.ProductGtinQueryBoost"] = "Product Gtin Query Boost",
            ["VIU.Plugin.SolrSearch.Admin.Setting.ProductGtinQueryBoost.Hint"] = "Product Gtin Query Boost",
            ["VIU.Plugin.SolrSearch.Admin.Setting.WildcardQueryEnabled"] = "Wildcard Query Enabled",
            ["VIU.Plugin.SolrSearch.Admin.Setting.WildcardQueryEnabled.Hint"] = "Wildcard Query Enabled",
            ["VIU.Plugin.SolrSearch.Admin.Setting.WildcardQueryBoost"] = "Wildcard Query Boost",
            ["VIU.Plugin.SolrSearch.Admin.Setting.WildcardQueryBoost.Hint"] = "Wildcard Query Boost",
            ["VIU.Plugin.SolrSearch.Admin.Setting.WildcardQueryMinLength"] = "Wildcard Query Min Length",
            ["VIU.Plugin.SolrSearch.Admin.Setting.WildcardQueryMinLength.Hint"] = "Wildcard Query Min Length",
            ["VIU.Plugin.SolrSearch.Admin.Setting.WildcardQuerySelectedType"] = "Wildcard Query Selected Type",
            ["VIU.Plugin.SolrSearch.Admin.Setting.WildcardQuerySelectedType.Hint"] = "Wildcard Query Selected Type",
            ["VIU.Plugin.SolrSearch.Admin.Setting.FuzzyQueryEnabled"] = "Fuzzy Query Enabled",
            ["VIU.Plugin.SolrSearch.Admin.Setting.FuzzyQueryEnabled.Hint"] = "Fuzzy Query Enabled",
            ["VIU.Plugin.SolrSearch.Admin.Setting.FuzzyQueryBoost"] = "Fuzzy Query Boost",
            ["VIU.Plugin.SolrSearch.Admin.Setting.FuzzyQueryBoost.Hint"] = "Fuzzy Query Boost",
            ["VIU.Plugin.SolrSearch.Admin.Setting.FuzzyQueryFuzziness"] = "Fuzzy Query Fuzziness",
            ["VIU.Plugin.SolrSearch.Admin.Setting.FuzzyQueryFuzziness.Hint"] = "Fuzzy Query Fuzziness",
            ["VIU.Plugin.SolrSearch.Admin.Setting.FuzzyQueryMinLength"] = "Fuzzy Query Min Length",
            ["VIU.Plugin.SolrSearch.Admin.Setting.FuzzyQueryMinLength.Hint"] = "Fuzzy Query Min Length",
            ["VIU.Plugin.SolrSearch.Admin.Setting.PhraseQueryEnabled"] = "Phrase Query Enabled",
            ["VIU.Plugin.SolrSearch.Admin.Setting.PhraseQueryEnabled.Hint"] = "Phrase Query Enabled",
            ["VIU.Plugin.SolrSearch.Admin.Setting.PhraseQueryBoost"] = "Phrase Query Boost",
            ["VIU.Plugin.SolrSearch.Admin.Setting.PhraseQueryBoost.Hint"] = "Phrase Query Boost",
            ["VIU.Plugin.SolrSearch.Admin.Setting.PhraseQueryProximity"] = "Phrase Query Proximity",
            ["VIU.Plugin.SolrSearch.Admin.Setting.PhraseQueryProximity.Hint"] = "Phrase Query Proximity",
            
            ["VIU.Plugin.SolrSearch.Menu.Top"] = "VIU SolrSearch",
            ["VIU.Plugin.SolrSearch.Menu.GeneralSettings"] = "SolrSearch General Settings",
            ["VIU.Plugin.SolrSearch.Admin.Settings.Title"] = "SolrSearch General Settings",
            ["VIU.Plugin.SolrSearch.Admin.TabTitle.SolrSettings"] = "Solr Settings",
            ["VIU.Plugin.SolrSearch.Admin.TabTitle.Boosting"] = "Boosting",
            ["VIU.Plugin.SolrSearch.Admin.TabTitle.HeroProducts"] = "Hero Products",
            ["VIU.Plugin.SolrSearch.Admin.BoostingNameFieldInfo"] = "The product-name fields can be configured with the following settings:",
        };
    }
}