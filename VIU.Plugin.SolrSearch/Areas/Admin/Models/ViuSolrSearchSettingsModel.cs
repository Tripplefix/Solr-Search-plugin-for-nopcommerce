using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace VIU.Plugin.SolrSearch.Areas.Admin.Models
{
    public record ViuSolrSearchSettingsModel : BaseNopModel, ISettingsModel
    {
        public ViuSolrSearchSettingsModel()
        {
            AvailableFilterableSpecificationAttributes = new List<SelectListItem>();
            SelectedFilterableSpecificationAttributeIds = new List<int>();
        }
        
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.AllowEmptySearchQuery")]
        public bool AllowEmptySearchQuery { get; set; }

        public IList<SelectListItem> AvailableFilterableSpecificationAttributes { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.FilterableSpecificationAttributes")]
        public IList<int> SelectedFilterableSpecificationAttributeIds { get; set; }

        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.HeroProducts")]
        public string HeroProducts { get; set; }

        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.DefaultLanguage")]
        public string DefaultLanguage { get; set; }

        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.IncludeCategoriesInFilter")]
        public bool IncludeCategoriesInFilter { get; set; }
    }
}