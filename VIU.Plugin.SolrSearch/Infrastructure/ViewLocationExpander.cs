using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor;

namespace VIU.Plugin.SolrSearch.Infrastructure
{
    public class ViewLocationExpander : IViewLocationExpander
    {
        private readonly string[] _supportedThemes =
        {
            "Molicare",
            "Betriebsapotheke",
            "Sterillium",
            "Dermaplast",
            "Venture" // legacy betriebsapotheke theme
        };
        
        private const string THEME_KEY = "nop.themename";
        
        private readonly string[] _controllerWhisteList =
        {
            "solrsearch"
        };

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            if (_controllerWhisteList.Contains(context.ControllerName.ToLower()))
            {
                if (context.Values.TryGetValue(THEME_KEY, out var theme) && _supportedThemes.Contains(theme))
                {
                    viewLocations = new[]
                    {
                        $"~/Plugins/SolrSearch/Themes/{theme}/Views/{{1}}/{{0}}.cshtml",
                        $"~/Plugins/SolrSearch/Themes/{theme}/Views/Shared/{{0}}.cshtml",
                         "~/Plugins/SolrSearch/Views/{1}/{0}.cshtml", 
                         "~/Plugins/SolrSearch/Views/Shared/{0}.cshtml"
                    }.Concat(viewLocations);
                }
                else
                {
                    viewLocations = new[]
                    {
                        "~/Plugins/SolrSearch/Views/{1}/{0}.cshtml", 
                        "~/Plugins/SolrSearch/Views/Shared/{0}.cshtml"
                    }.Concat(viewLocations);
                }
            }

            if (context.ControllerName == "ViuSolrSearchSettings" && context.AreaName == "Admin")
            {
                viewLocations = new[] {
                    "~/Plugins/SolrSearch/Areas/Admin/Views/{1}/{0}.cshtml",
                    "~/Plugins/SolrSearch/Areas/Admin/Views/Shared/{0}.cshtml"
                }.Concat(viewLocations);
            }

            return viewLocations;
        }

        public void PopulateValues(ViewLocationExpanderContext context) { }
    }
}
