using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.SolrSearch.Areas.Admin.Models
{
    public partial record AddProductsToHeroProductsSearchModel : BaseSearchModel
    {
        [NopResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]
        public string SearchProductName { get; set; }
    }
}