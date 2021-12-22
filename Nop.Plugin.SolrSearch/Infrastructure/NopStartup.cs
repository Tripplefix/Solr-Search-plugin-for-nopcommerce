using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Data;
using SolrNet;
using Nop.Plugin.SolrSearch.Models;

namespace Nop.Plugin.SolrSearch.Infrastructure
{
    public class NopStartup : INopStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var ds = DataSettingsManager.LoadSettings();
            services.AddSolrNet<ProductSolrDocument>(ds.RawDataSettings.FirstOrDefault(kv => kv.Key == "solrProducts").Value);
            
            services.Configure<RazorViewEngineOptions>(options =>
            {
               options.ViewLocationExpanders.Add(new ViewLocationExpander());
            });
        }

        public void Configure(IApplicationBuilder application) { }

        public int Order => 1001;
    }
}