using VIU.Plugin.SolrSearch.Models;

namespace VIU.Plugin.SolrSearch.Infrastructure
{
	public class ProductIndexedEvent
	{
		public ProductIndexedEvent(ProductSolrDocument solrDocument, string language)
		{
			SolrDocument = solrDocument;
			Language = language;
		}
		
		public ProductSolrDocument SolrDocument { get; set; }
		public string Language { get; set; }
	}
}