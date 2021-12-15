using System.Threading.Tasks;
using Nop.Core.Domain.Logging;
using Nop.Core.Events;
using Nop.Services.Catalog;
using Nop.Services.Events;
using VIU.Plugin.SolrSearch.Services;

namespace VIU.Plugin.SolrSearch.Infrastructure.EventConsumer
{
	public class ProductUpdatedEventConsumer : 
		IConsumer<EntityInsertedEvent<ActivityLog>>
	{
		private readonly IProductIndexingService _productIndexingService;
		private readonly IProductService _productService;

		public ProductUpdatedEventConsumer(IProductIndexingService productIndexingService, IProductService productService)
		{
			_productIndexingService = productIndexingService;
			_productService = productService;
		}

		// Instead of the EntityInsertedEvent of the product, we listen to the one on the activity log.
		// This workaraound is necessary because the former is triggered before the product pictures are updated.
		public async Task HandleEventAsync(EntityInsertedEvent<ActivityLog> eventMessage)
		{
			const int addNewProduct = 22;
			const int editProduct = 103;
			const int deleteProduct = 62;
			
			var log = eventMessage.Entity;
			var productId = log.EntityId ?? 0;

			if (log.EntityName != "Product")
				return;
			
			var product = await _productService.GetProductByIdAsync(productId);

			if (product == null)
				return;

			// if a product is unpublished, remove it from the index
			if (!product.Published)
			{
				await _productIndexingService.DeleteProduct(product);

				return;
			}

			switch (log.ActivityLogTypeId)
			{
				case addNewProduct:
				case editProduct:
					await _productIndexingService.AddOrUpdateProduct(product);

					break;
				case deleteProduct:
					await _productIndexingService.DeleteProduct(product);

					break;
			}
		}
	}
}