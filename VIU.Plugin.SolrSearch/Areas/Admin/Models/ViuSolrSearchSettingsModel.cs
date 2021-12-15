using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using VIU.Plugin.SolrSearch.Settings;

namespace VIU.Plugin.SolrSearch.Areas.Admin.Models
{
    public record ViuSolrSearchSettingsModel : BaseNopModel, ISettingsModel
    {
        public ViuSolrSearchSettingsModel()
        {
            AvailableFilterableSpecificationAttributes = new List<SelectListItem>();
            SelectedFilterableSpecificationAttributeIds = new List<int>();
            AvailableWildcardQueryTypes = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Text = "Postfix",
                    Value = "1",
                    Selected = true
                },
                new SelectListItem
                {
                    Text = "Prefix",
                    Value = "2"
                },
                new SelectListItem
                {
                    Text = "Prefix & Postfix",
                    Value = "3"
                }
            };
        }
        
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.AllowEmptySearchQuery")]
        public bool AllowEmptySearchQuery { get; set; }

        public IList<SelectListItem> AvailableFilterableSpecificationAttributes { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.FilterableSpecificationAttributes")]
        public IList<int> SelectedFilterableSpecificationAttributeIds { get; set; }

        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.EnableHeroProducts")]
        public bool EnableHeroProducts { get; set; }

        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.HeroProducts")]
        public string HeroProducts { get; set; }

        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.DefaultLanguage")]
        public string DefaultLanguage { get; set; }

        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.IncludeSpecificationAttributesInFilter")]
        public bool IncludeSpecificationAttributesInFilter { get; set; }

        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.IncludeCategoriesInFilter")]
        public bool IncludeCategoriesInFilter { get; set; }

        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.IncludeManufacturersInFilter")]
        public bool IncludeManufacturersInFilter { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.HideFacetOptionsWithNoCount")]
        public bool HideFacetOptionsWithNoCount { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.MaxReturnedDocuments")]
        public int MaxReturnedDocuments { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.HighlightingEnabled")]
        public bool HighlightingEnabled { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.SpellcheckingEnabled")]
        public bool SpellcheckingEnabled { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.SpellcheckingOnlyMorePopular")]
        public bool SpellcheckingOnlyMorePopular { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.PreparePriceModel")]
        public bool PreparePriceModel { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.PreparePictureModel")]
        public bool PreparePictureModel { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.PrepareSpecificationAttributes")]
        public bool PrepareSpecificationAttributes { get; set; }
        
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.ProductNameQueryBoost")]
        public double? ProductNameQueryBoost { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.ProductShortDescriptionQueryBoost")]
        public double? ProductShortDescriptionQueryBoost { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.ProductFullDescriptionQueryBoost")]
        public double? ProductFullDescriptionQueryBoost { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.ProductSkuQueryBoost")]
        public double? ProductSkuQueryBoost { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.ProductGtinQueryBoost")]
        public double? ProductGtinQueryBoost { get; set; }
        
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.WildcardQueryEnabled")]
        public bool WildcardQueryEnabled { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.WildcardQueryBoost")]
        public double? WildcardQueryBoost { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.WildcardQueryMinLength")]
        public int WildcardQueryMinLength { get; set; }

        public IList<SelectListItem> AvailableWildcardQueryTypes { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.WildcardQuerySelectedType")]
        public ViuSolrSearchSettings.WildcardQueryType WildcardQuerySelectedType { get; set; }

        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.FuzzyQueryEnabled")]
        public bool FuzzyQueryEnabled { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.FuzzyQueryBoost")]
        public double? FuzzyQueryBoost { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.FuzzyQueryFuzziness")]
        public int? FuzzyQueryFuzziness { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.FuzzyQueryMinLength")]
        public int FuzzyQueryMinLength { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.PhraseQueryEnabled")]
        public bool PhraseQueryEnabled { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.PhraseQueryBoost")]
        public double? PhraseQueryBoost { get; set; }
        
        [NopResourceDisplayName("VIU.Plugin.SolrSearch.Admin.Setting.PhraseQueryProximity")]
        public int? PhraseQueryProximity { get; set; }
    }
}