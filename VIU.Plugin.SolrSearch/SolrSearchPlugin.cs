using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Web.Framework.Menu;

namespace VIU.Plugin.SolrSearch
{
    public class SolrSearchPlugin : BasePlugin, IAdminMenuPlugin
    {
        private readonly ILocalizationService _localizationService;
        private readonly ICustomerService _customerService;
        private readonly IWorkContext _workContext;

        public SolrSearchPlugin(ILocalizationService localizationService, ICustomerService customerService, IWorkContext workContext)
        {
            _localizationService = localizationService;
            _customerService = customerService;
            _workContext = workContext;
        }

        public async Task ManageSiteMapAsync(SiteMapNode rootNode)
        {
            var menuItem = new SiteMapNode
            {
                SystemName = "VIU.Plugin.SolrSearch",
                Title = await _localizationService.GetResourceAsync("VIU.Plugin.SolrSearch.Menu.Top"),
                Visible = await _customerService.IsAdminAsync(await _workContext.GetCurrentCustomerAsync()),
                IconClass = "fas fa-sliders"
            };
            
            var coreMenuItem = new SiteMapNode
            {
                SystemName = "VIU.Plugin.SolrSearch.GeneralSettings",
                Title = await _localizationService.GetResourceAsync("VIU.Plugin.SolrSearch.Menu.GeneralSettings"),
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
    }
}