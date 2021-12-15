using System.Collections.Generic;
using SolrNet;

namespace VIU.Plugin.SolrSearch.Infrastructure
{
	public class ProductSearchedEvent
	{
		public ProductSearchedEvent(List<ISolrQuery> queries, string q, string defaultLanguage, bool isDefault)
		{
			Queries = queries;
			Q = q;
			DefaultLanguage = defaultLanguage;
			IsDefault = isDefault;
		}
		
		public List<ISolrQuery> Queries { get; set; }
		public string Q { get; set; }
		public string DefaultLanguage { get; set; }
		public bool IsDefault { get; set; }
	}
}