using Nop.Core.Configuration;

namespace Nop.Plugin.SolrSearch.Settings
{
    public class SolrSearchSettings : ISettings
    {
        public SolrSearchSettings()
        {
            WildcardQuerySelectedType = WildcardQueryType.Postfix;
        }

        public SolrSearchSettings(bool allowEmptySearchQuery) : this()
        {
	        AllowEmptySearchQuery = allowEmptySearchQuery;
        }

        public bool AllowEmptySearchQuery { get; set; }

        public string SelectedFilterableSpecificationAttributeIds { get; set; }

        public bool EnableHeroProducts { get; set; }
        public string HeroProducts { get; set; }

        public string DefaultLanguage { get; set; }
        public bool IncludeSpecificationAttributesInFilter { get; set; }
        public bool IncludeCategoriesInFilter { get; set; }
        public bool IncludeManufacturersInFilter { get; set; }
        public bool HideFacetOptionsWithNoCount { get; set; }
        public int MaxReturnedDocuments { get; set; }
        public bool HighlightingEnabled { get; set; }
        public bool SpellcheckingEnabled { get; set; }
        public bool SpellcheckingOnlyMorePopular { get; set; }
        public bool PreparePriceModel { get; set; }
        public bool PreparePictureModel { get; set; }
        public bool PrepareSpecificationAttributes { get; set; }
        
        public double? ProductNameQueryBoost { get; set; }
        public double? ProductSkuQueryBoost { get; set; }
        public double? ProductGtinQueryBoost { get; set; }
        
        public bool WildcardQueryEnabled { get; set; }
        public double? WildcardQueryBoost { get; set; }
        public int WildcardQueryMinLength { get; set; }
        public WildcardQueryType WildcardQuerySelectedType { get; set; }

        public enum WildcardQueryType
        {
            Postfix = 1,
            Prefix = 2,
            PrefixAndPostfix = 3
        }
        
        public bool FuzzyQueryEnabled { get; set; }
        public double? FuzzyQueryBoost { get; set; }
        public int? FuzzyQueryFuzziness { get; set; }
        public int FuzzyQueryMinLength { get; set; }
        
        public bool PhraseQueryEnabled { get; set; }
        public double? PhraseQueryBoost { get; set; }
        public int? PhraseQueryProximity { get; set; }
    }
}