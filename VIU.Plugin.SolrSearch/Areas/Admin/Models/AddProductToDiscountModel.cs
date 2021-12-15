using System.Collections.Generic;
using Nop.Web.Framework.Models;

namespace VIU.Plugin.SolrSearch.Areas.Admin.Models
{
	public record AddProductToHeroProductsModel : BaseNopModel
	{
		public AddProductToHeroProductsModel()
		{
			SelectedProductIds = new List<int>();
		}

		public IList<int> SelectedProductIds { get; set; }
	}
}