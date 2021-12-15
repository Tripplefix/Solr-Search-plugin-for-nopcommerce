using Nop.Web.Framework.Models;

namespace VIU.Plugin.SolrSearch.Areas.Admin.Models
{
	public record HeroProductModel : BaseNopEntityModel
	{
		public int ProductId { get; set; }

		public string ProductName { get; set; }

		public int DisplayOrder { get; set; }
	}
}