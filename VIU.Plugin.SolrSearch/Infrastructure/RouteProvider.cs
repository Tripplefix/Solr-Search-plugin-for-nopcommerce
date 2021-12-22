using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Domain.Localization;
using Nop.Data;
using Nop.Services.Localization;
using Nop.Web.Framework.Mvc.Routing;
using Nop.Web.Infrastructure;

namespace VIU.Plugin.SolrSearch.Infrastructure
{
    public class RouteProvider : BaseRouteProvider, IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Route builder</param>
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            var lang = GetLanguageRoutePattern();
            
            endpointRouteBuilder.MapControllerRoute("SolrSearch",
                $"{lang}/search",
                new { controller = "SolrSearch", action = "SearchProducts" });
            
            endpointRouteBuilder.MapControllerRoute("SearchProductsUpdate",
                "/search-update",
                new { controller = "SolrSearch", action = "SearchProductsUpdate" });

            //autocomplete search term (AJAX)
            endpointRouteBuilder.MapControllerRoute(name: "ProductSearchAutoComplete", 
	            pattern: "catalog/searchtermautocomplete",
	            defaults: new { controller = "CatalogExtended", action = "SearchTermAutoComplete" });
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority => int.MaxValue;
    }
}
