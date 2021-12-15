using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Domain.Tasks;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Tasks;
using Nop.Web.Framework.Menu;
using Task = System.Threading.Tasks.Task;

namespace VIU.Plugin.SolrSearch
{
    public class SolrSearchPlugin : BasePlugin, IAdminMenuPlugin
    {
        private readonly ILocalizationService _localizationService;
        private readonly ICustomerService _customerService;
        private readonly IWorkContext _workContext;
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly ILanguageService _languageService;

        public SolrSearchPlugin(ILocalizationService localizationService, ICustomerService customerService, IWorkContext workContext, IScheduleTaskService scheduleTaskService, ILanguageService languageService)
        {
            _localizationService = localizationService;
            _customerService = customerService;
            _workContext = workContext;
            _scheduleTaskService = scheduleTaskService;
            _languageService = languageService;
        }

        public async Task ManageSiteMapAsync(SiteMapNode rootNode)
        {
            var menuItem = new SiteMapNode
            {
                SystemName = "VIU.Plugin.SolrSearch",
                Title = await _localizationService.GetResourceAsync("VIU.Plugin.SolrSearch.Admin.Menu.Top"),
                Visible = await _customerService.IsAdminAsync(await _workContext.GetCurrentCustomerAsync()),
                IconClass = "fas fa-sliders"
            };
            
            var coreMenuItem = new SiteMapNode
            {
                SystemName = "VIU.Plugin.SolrSearch.GeneralSettings",
                Title = await _localizationService.GetResourceAsync("VIU.Plugin.SolrSearch.Admin.Menu.GeneralSettings"),
                ControllerName = "ViuSolrSearchSettings",
                ActionName = "Configure",
                Visible = await _customerService.IsAdminAsync(await _workContext.GetCurrentCustomerAsync()),
                RouteValues = new RouteValueDictionary
                {
                    {
                        "Area", "Admin"
                    }
                },
                IconClass = "fa fa-gears"
            };
            menuItem.ChildNodes.Add(coreMenuItem);
            
            rootNode.ChildNodes.Add(menuItem);
        }

        public override async Task InstallAsync()
        {
            //install synchronization task
            if (await _scheduleTaskService.GetTaskByTypeAsync(SolrSearchPluginDefaults.BuildIndexTask) == null)
            {
                await _scheduleTaskService.InsertTaskAsync(new ScheduleTask
                {
                    Enabled = true,
                    Seconds = SolrSearchPluginDefaults.DefaultReindexPeriod * 60 * 60,
                    Name = SolrSearchPluginDefaults.BuildIndexTaskName,
                    Type = SolrSearchPluginDefaults.BuildIndexTask,
                });
            }
            
            
            // admin texts
            await _localizationService.AddLocaleResourceAsync(new Dictionary<string, string>
            {
                ["VIU.Plugin.SolrSearch.Admin.Menu.Top"] = "VIU SolrSearch",
                ["VIU.Plugin.SolrSearch.Admin.Menu.GeneralSettings"] = "SolrSearch General Settings",
                ["VIU.Plugin.SolrSearch.Admin.Setting.AllowEmptySearchQuery"] = "Allow empty search queries",
                ["VIU.Plugin.SolrSearch.Admin.Setting.DefaultLanguage"] = "Default language",
                ["VIU.Plugin.SolrSearch.Admin.Setting.FilterableSpecificationAttributes"] = "Specification Attributes displayed in filter",
                ["VIU.Plugin.SolrSearch.Admin.Setting.HeroProducts"] = "Hero Products comma-separated",
                ["VIU.Plugin.SolrSearch.Admin.Setting.HeroProducts.Table.Product"] = "Product Name",
                ["VIU.Plugin.SolrSearch.Admin.Setting.HeroProducts.Table.DisplayOrder"] = "Display Order",
                ["VIU.Plugin.SolrSearch.Admin.Setting.HeroProducts.Table.MoveUp"] = "👇",
                ["VIU.Plugin.SolrSearch.Admin.Setting.HeroProducts.Table.MoveDown"] = "👆",
                ["VIU.Plugin.SolrSearch.Admin.Setting.HeroProducts.Table.AddNew"] = "Add new product",
                ["VIU.Plugin.SolrSearch.Admin.Setting.IncludeCategoriesInFilter"] = "Include categories in filter",
                ["VIU.Plugin.SolrSearch.Admin.Setting.SpecificationAttributes.NoAttributes"] = "No attributes available.",
                ["VIU.Plugin.SolrSearch.Admin.Setting.Title"] = "SolrSearch General Settings",
            });
            
            
            var languages = await _languageService.GetAllLanguagesAsync();

            foreach (var language in languages)
            {
                switch (language.UniqueSeoCode)
                {
                    case "de":
                        
                        // german texts
                        await _localizationService.AddLocaleResourceAsync(new Dictionary<string, string>
                        {
                            ["VIU.Plugin.SolrSearch.FacetFilter.SelectDefaultValue"] = "Filtern",
                            ["VIU.Plugin.SolrSearch.ResultPage.ProductsWithCount"] = "{0} Artikel zu<strong> «{1}» </strong>gefunden",
                            ["VIU.Plugin.SolrSearch.ResultPage.FilterTitle"] = "Filter",
                            ["VIU.Plugin.SolrSearch.ResultPage.FilterReset"] = "Zurücksetzen",
                            ["VIU.Plugin.SolrSearch.ResultPage.FilterClose"] = "Filter schliessen",
                            ["VIU.Plugin.SolrSearch.ResultPage.Title"] = "Dermaplast Sortiment",
                            ["VIU.Plugin.SolrSearch.ResultPage.ShowMore"] = "Mehr anzeigen",
                        }, language.Id);
                        break;
                    case "fr":
                        
                        // french texts
                        await _localizationService.AddLocaleResourceAsync(new Dictionary<string, string>
                        {
                            ["VIU.Plugin.SolrSearch.FacetFilter.SelectDefaultValue"] = "Filtre",
                            ["VIU.Plugin.SolrSearch.ResultPage.ProductsWithCount"] = "{0} Article à<strong> «{1}» </strong>trouvés",
                            ["VIU.Plugin.SolrSearch.ResultPage.FilterTitle"] = "Filtre",
                            ["VIU.Plugin.SolrSearch.ResultPage.FilterReset"] = "#FRZurücksetzen",
                            ["VIU.Plugin.SolrSearch.ResultPage.FilterClose"] = "#FRFilter schliessen",
                            ["VIU.Plugin.SolrSearch.ResultPage.Title"] = "#FRDermaplast Sortiment",
                            ["VIU.Plugin.SolrSearch.ResultPage.ShowMore"] = "#FRMehr anzeigen",
                        }, language.Id);
                        break;
                    
                    case "en":
                    default:
                        
                        // english texts
                        await _localizationService.AddLocaleResourceAsync(new Dictionary<string, string>
                        {
                            ["VIU.Plugin.SolrSearch.FacetFilter.SelectDefaultValue"] = "Filter",
                            ["VIU.Plugin.SolrSearch.ResultPage.ProductsWithCount"] = "{0} products found for<strong> «{1}» </strong>",
                            ["VIU.Plugin.SolrSearch.ResultPage.FilterTitle"] = "Filter",
                            ["VIU.Plugin.SolrSearch.ResultPage.FilterReset"] = "Reset",
                            ["VIU.Plugin.SolrSearch.ResultPage.FilterClose"] = "Close filter",
                            ["VIU.Plugin.SolrSearch.ResultPage.Title"] = "Results",
                            ["VIU.Plugin.SolrSearch.ResultPage.ShowMore"] = "Show more",
                        }, language.Id);
                        break;
                }
            }
            
            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            //schedule task
            var task = await _scheduleTaskService.GetTaskByTypeAsync(SolrSearchPluginDefaults.BuildIndexTask);
            if (task != null)
                await _scheduleTaskService.DeleteTaskAsync(task);

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("VIU.Plugin.SolrSearch");
            
            await base.UninstallAsync();
        }
    }
}