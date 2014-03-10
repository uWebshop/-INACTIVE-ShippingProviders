using System.Collections.Generic;
using System.Linq;
using uWebshop.Common;
using uWebshop.Domain;
using uWebshop.Domain.Helpers;
using uWebshop.Domain.Interfaces;

namespace uWebshop.Shipping.Example
{
	public class ExampleShippingProvider : IShippingProvider
	{
		private readonly IHttpRequestSender _requestSender;

		public ExampleShippingProvider()
		{
			_requestSender = new HttpRequestSender();
		}

		public ExampleShippingProvider(IHttpRequestSender requestSender = null)
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

			string issuerId = "0";
			string issuerName = "DHL";

			var method = new ShippingProviderMethod {ShippingProviderUpdateService = new ExampleShippingProviderShippingProviderUpdateService(), Id = issuerId, Description = issuerName, Title = issuerName, Name = issuerName, ProviderName = GetName()};

			return new List<ShippingProviderMethod> {method};
		}

		public class ExampleShippingProviderShippingProviderUpdateService : IShippingProviderUpdateService
		{
			public void Update(ShippingProviderMethod shippingProviderMethod, OrderInfo orderInfo)
			{
				Log.Instance.LogDebug("Hoeveelheid artikelen totaal: " + orderInfo.OrderLines.Sum(x => x.ProductInfo.ItemCount));
				Log.Instance.LogDebug("Hoeveel eerste artikel in mandje: " + orderInfo.OrderLines.First().ProductInfo.ItemCount);

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
			}
		}
	}
}