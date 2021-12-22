loadPricesForProducts();

function loadPricesForProducts(retry) {
    const productsContainer = document.querySelector("*[data-prices-async]");
    
    if(!productsContainer) return;
    
    const priceEndpointUrl = productsContainer.dataset.pricesAsyncUrl;
    const productItems = productsContainer.querySelectorAll(".product-item");
    const productIds = [...productItems].map(function(element){
        return element.dataset.productid;
    });
    
    $.ajax({
        cache: false,
        url: priceEndpointUrl,
        data: {
            productIds
        },
        traditional: true,
        type: "GET",
        success: function(response){
            const products = response;

            if(!products || products.length === 0){
                // retry
                if(!retry){
                    loadPricesForProducts(true);
                }else{
                    console.error("Preise für die Produkte konnten nicht geladen werden!");
                }
            }

            for (const product of products) {
                const productBox = document.querySelector('*[data-productid="' + product.id + '"]');

                if(product.priceModel.Price){
                    const priceContainer = productBox.querySelector(".actual-price");

                    if(priceContainer){
                        priceContainer.textContent = product.priceModel.Price;
                    }
                }

                if(product.priceModel.OldPrice){
                    const oldPriceContainer = productBox.querySelector(".old-price");

                    if(oldPriceContainer){
                        oldPriceContainer.textContent = product.priceModel.OldPrice;
                    }
                }
            }
        },
        error: function(){
            // retry
            if(!retry){
                loadPricesForProducts(true);
            }else{
                console.error("Preise für die Produkte konnten nicht geladen werden!");
            }
        }
    });
}
