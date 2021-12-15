using Nop.Core.Configuration;

namespace VIU.Plugin.SolrSearch.Settings
{
    public class ViuSolrSearchSettings : ISettings
    {
        public bool AllowEmptySearchQuery { get; set; }

        public string SelectedFilterableSpecificationAttributeIds { get; set; }

        public string HeroProducts { get; set; }

        public string DefaultLanguage { get; set; }
        public bool IncludeCategoriesInFilter { get; set; }
    }
}