﻿using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using VIU.Plugin.SolrSearch.Factories;
using VIU.Plugin.SolrSearch.Services;

namespace VIU.Plugin.SolrSearch.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public void Register(IServiceCollection services, ITypeFinder typeFinder, AppSettings appSettings)
        {
            services.AddScoped<IProductIndexingService, ProductIndexingService>();
            services.AddScoped<IProductSearchService, ProductSearchService>();
            services.AddScoped<ISolrSearchFactory, SolrSearchFactory>();
            services.AddScoped<IExtendedProductModelFactory, ExtendedProductModelFactory>();
        }

        public int Order => 100000;
    }
}
