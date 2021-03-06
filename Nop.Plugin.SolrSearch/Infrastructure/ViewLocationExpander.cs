using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Nop.Plugin.SolrSearch.Infrastructure
{
    public class ViewLocationExpander : IViewLocationExpander
    {
	    // todo: support some popular themes
        private readonly string[] _supportedThemes =
        {
            "Venture"
        };
        
        private const string THEME_KEY = "nop.themename";
        
        private readonly string[] _controllerWhisteList =
        {
            "solrsearch"
        };

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
	        // override default SearchBox
	        if (context.ViewName == "Components/SearchBox/Default")
	        {
		        viewLocations = new[] {
			        "~/Plugins/Nop.Plugin.SolrSearch/Views/Shared/{0}.cshtml",
		        }.Concat(viewLocations); 
	        }
	        
            if (_controllerWhisteList.Contains(context.ControllerName.ToLower()))
            {
                if (context.Values.TryGetValue(THEME_KEY, out var theme) && _supportedThemes.Contains(theme))
                {
                    viewLocations = new[]
                    {
                        $"~/Plugins/Nop.Plugin.SolrSearch/Themes/{theme}/Views/{{1}}/{{0}}.cshtml",
                        $"~/Plugins/Nop.Plugin.SolrSearch/Themes/{theme}/Views/Shared/{{0}}.cshtml",
                         "~/Plugins/Nop.Plugin.SolrSearch/Views/{1}/{0}.cshtml", 
                         "~/Plugins/Nop.Plugin.SolrSearch/Views/Shared/{0}.cshtml"
                    }.Concat(viewLocations);
                }
                else
                {
                    viewLocations = new[]
                    {
                        "~/Plugins/Nop.Plugin.SolrSearch/Views/{1}/{0}.cshtml", 
                        "~/Plugins/Nop.Plugin.SolrSearch/Views/Shared/{0}.cshtml"
                    }.Concat(viewLocations);
                }
            }

            if (context.ControllerName == "SolrSearchSettings" && context.AreaName == "Admin")
            {
                viewLocations = new[] {
                    "~/Plugins/Nop.Plugin.SolrSearch/Areas/Admin/Views/{1}/{0}.cshtml",
                    "~/Plugins/Nop.Plugin.SolrSearch/Areas/Admin/Views/Shared/{0}.cshtml"
                }.Concat(viewLocations);
            }

            return viewLocations;
        }

        public void PopulateValues(ViewLocationExpanderContext context) { }
    }
}
