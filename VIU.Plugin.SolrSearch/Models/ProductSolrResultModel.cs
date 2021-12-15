using System.Collections.Generic;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Models.Catalog;

namespace VIU.Plugin.SolrSearch.Models
{
    public class ProductSolrResultModel
    {
        public ProductSolrResultModel()
        {
            ProductFacets = new List<ProductFacet>();
            Products = new List<ProductOverviewModel>();
            q = string.Empty;
            Facets = string.Empty;
        }

        public string Warning { get; set; }

        public bool NoResults { get; set; }

        /// <summary>
        /// Query string
        /// </summary>
        [NopResourceDisplayName("Search.SearchTerm")]
        public string q { get; set; }

        public string Facets { get; set; }

        public string PresetLanguage { get; set; }

        public IList<ProductOverviewModel> Products { get; set; }

        public IList<ProductFacet> ProductFacets { get; set; }
    }

    public class ProductFacet
    {
        public ProductFacet()
        {
            FacetValues = new List<FacetValue>();
        }
        
        public string FacetName { get; set; }

        public string FacetDisplayName { get; set; }

        public IEnumerable<FacetValue> FacetValues { get; set; }

        public class FacetValue
        {
            public string OptionName { get; set; }
            public string OptionDisplayName { get; set; }
            public int OptionProductCount { get; set; }
            public bool FilterActive { get; set; }
            public int DisplayOrder { get; set; }
        }
    }
}
