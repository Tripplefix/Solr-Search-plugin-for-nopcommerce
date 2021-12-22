using System.Threading.Tasks;
using Nop.Core.Domain.Logging;
using Nop.Core.Events;
using Nop.Services.Catalog;
using Nop.Services.Events;
using Nop.Services.Logging;
using Nop.Plugin.SolrSearch.Services;

namespace Nop.Plugin.SolrSearch.Infrastructure.EventConsumer
{
	public class ProductUpdatedEventConsumer : 
		IConsumer<EntityInsertedEvent<ActivityLog>>
	{
		private readonly IProductIndexingService _productIndexingService;
		private readonly IProductService _productService;
		private readonly ICustomerActivityService _customerActivityService;

		public ProductUpdatedEventConsumer(IProductIndexingService productIndexingService, IProductService productService, ICustomerActivityService customerActivityService)
		{
			_productIndexingService = productIndexingService;
			_productService = productService;
			_customerActivityService = customerActivityService;
		}

		// Instead of the EntityInsertedEvent of the product, we listen to the one on the activity log.
		// This workaround is necessary because the former is triggered before the product pictures are updated.
		public async Task HandleEventAsync(EntityInsertedEvent<ActivityLog> eventMessage)
		{
			const string addNewProduct = "AddNewProduct";
			const string editProduct = "EditProduct";
			const string deleteProduct = "DeleteProduct";
			
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

			var activityLogType = await _customerActivityService.GetActivityTypeByIdAsync(log.ActivityLogTypeId);

			switch (activityLogType.SystemKeyword)
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