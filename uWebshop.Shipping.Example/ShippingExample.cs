using System.Collections.Generic;
using System.Linq;
using uWebshop.Common;
using uWebshop.API;
using uWebshop.Domain;
using uWebshop.Domain.Interfaces;
using Store = uWebshop.API.Store;

namespace uWebshop.Shipping.Example
{
    public class ExampleProvider : IShippingProvider
    {
        private readonly IHttpRequestSender _requestSender;

        public ExampleProvider()
        {
            _requestSender = new HttpRequestSender();
        }

        public ExampleProvider(IHttpRequestSender requestSender = null)
        {
            _requestSender = requestSender ?? new HttpRequestSender();
        }

        public string GetName()
        {
            return "Example";
        }

        public IEnumerable<ShippingProviderMethod> GetAllShippingMethods(int id)
        {
            //var provider = new ShippingProvider(id);
            //var helper = new ShippingConfigHelper(provider);
            //var request = new ShippingRequest();
            //var orderInfo = OrderHelper.GetOrderInfo();

            var vat = Store.GetStore().GlobalVat;

            var order = Basket.GetBasket();

            if (order != null)
            {
                vat = order.AverageOrderVatPercentage;
            }

            var issuerId = "0";
            var issuerName = "Example";

            var method = new ShippingProviderMethod
            {
                ShippingProviderUpdateService =
                    new ExampleShippingProviderShippingProviderUpdateService(),
                Id = issuerId,
                Description = issuerName,
                Title = issuerName,
                Name = issuerName,
                ProviderName = GetName(),
                Vat = vat,
                PriceInCents = 500
            };

            return new List<ShippingProviderMethod> { method };
        }
        public class ExampleShippingProviderShippingProviderUpdateService : IShippingProviderUpdateService
        {
            public void Update(ShippingProviderMethod shippingProviderMethod, OrderInfo orderInfo)
            {
                if (orderInfo != null)
                {
                    // your logic here, and set the price at the end like this:

                    Log.Instance.LogDebug("Hoeveelheid artikelen totaal: " +
                                          orderInfo.OrderLines.Sum(x => x.ProductInfo.ItemCount));
                    Log.Instance.LogDebug("Hoeveel eerste artikel in mandje: " +
                                          orderInfo.OrderLines.First().ProductInfo.ItemCount);

                    // CALCULATE SHIPPING PRICE

                    int priceInCents;
                    var itemCount = orderInfo.OrderLines.Sum(line => line.ProductInfo.ItemCount.GetValueOrDefault(1));
                    if (itemCount == 1)
                        priceInCents = 500;
                    else if (itemCount == 2)
                        priceInCents = 400;
                    else
                        priceInCents = 0;
                    shippingProviderMethod.PriceInCents = priceInCents;

                    shippingProviderMethod.PriceInCents = 500;
                }
            }
        }
    }
}