using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using umbraco;
using uWebshop.Common;
using uWebshop.Domain;
using uWebshop.Domain.Helpers;
using uWebshop.Domain.Interfaces;
using System.Globalization;
using Log = uWebshop.Domain.Log;

namespace uWebshop.Shipping.AusPost
{
	public class AusPostProvider : IShippingProvider
	{
		private readonly IHttpRequestSender _requestSender;

		public AusPostProvider()
		{
			_requestSender = new HttpRequestSender();
		}

		public AusPostProvider(IHttpRequestSender requestSender = null)
		{
			_requestSender = requestSender ?? new HttpRequestSender();
		}

		public string GetName()
		{
			return "AusPost";
		}

		public IEnumerable<ShippingProviderMethod> GetAllShippingMethods(int id)
		{
			var methods = new List<ShippingProviderMethod>();
			var provider = ShippingProviderHelper.GetShippingProvider(id);

			var helper = new ShippingConfigHelper(provider);
			var request = new ShippingRequest();

			var orderInfo = OrderHelper.GetOrderInfo();

			var postalCodeFrom = helper.Settings["zipPostalCodeFrom"];

			var customerPostalCode = OrderHelper.CustomerInformationValue(orderInfo, "customerPostalCode");

			if (string.IsNullOrEmpty(customerPostalCode))
			{
				Log.Instance.LogDebug("AUSPOST GetAllShippingMethods: customerPostalCode IsNullOrEmpty: CUSTOMER SHOULD ENTER A POSTALCODE FIRST!");

				return methods;
			}

			var orderWeight = orderInfo.OrderLines.Sum(x => x.OrderLineWeight)/1000;

			if (orderWeight < 0.1)
			{
				orderWeight = 0.1;
			}
			if (orderWeight > 20)
			{
				Log.Instance.LogDebug("AUSPOST GetAllShippingMethods: Weight > 20: Weight should be in grams");

				return methods;
			}

			var orderWeightCulture = decimal.Parse(orderWeight.ToString(), NumberStyles.Currency, CultureInfo.GetCultureInfo("en-AU"));

			var orderWidth = orderInfo.OrderLines.Sum(x => x.ProductInfo.Weight);

			if (orderWidth < 5)
			{
				orderWidth = 5;
			}
			if (orderWidth > 105)
			{
				Log.Instance.LogDebug("AUSPOST GetAllShippingMethods: Wide > 105: width should be in cm");

				return methods;
			}

			var orderWidthCulture = decimal.Parse(orderWidth.ToString(), NumberStyles.Currency, CultureInfo.GetCultureInfo("en-AU"));

			var orderHeight = orderInfo.OrderLines.Sum(x => x.ProductInfo.Height);

			if (orderHeight < 5)
			{
				orderHeight = 5;
			}
			if (orderHeight > 105)
			{
				Log.Instance.LogDebug("AUSPOST GetAllShippingMethods: Hight > 105: height should be in cm");

				return methods;
			}

			var orderHeightCulture = decimal.Parse(orderHeight.ToString(), NumberStyles.Currency, CultureInfo.GetCultureInfo("en-AU"));

			var orderLength = orderInfo.OrderLines.Sum(x => x.ProductInfo.Length);

			if (orderLength < 5)
			{
				orderLength = 5;
			}
			if (orderLength > 105)
			{
				Log.Instance.LogDebug("AUSPOST GetAllShippingMethods: Length > 105: length should be in cm");

				return methods;
			}

			var widthOrLength = orderWidth;

			if (orderLength > orderWidth)
			{
				widthOrLength = orderLength;
			}

			var girth = 2*(Math.Round(widthOrLength, MidpointRounding.AwayFromZero) + Math.Round(orderHeight, MidpointRounding.AwayFromZero));

			if (girth < 16)
			{
				Log.Instance.LogDebug("AUSPOST GetAllShippingMethods: Girth < 16 (sizes should be in cm)");

				return methods;
			}

			if (girth > 140)
			{
				Log.Instance.LogDebug("AUSPOST GetAllShippingMethods: Girth > 140 (sizes should be in cm)");

				return methods;
			}

			var orderLengthCulture = decimal.Parse(orderLength.ToString(), NumberStyles.Currency, CultureInfo.GetCultureInfo("en-AU"));


			if (orderInfo.CustomerInfo.CountryCode.ToUpper() == "AU")
			{
				request.ShippingUrlBase = helper.Settings["ServiceUrlDomestic"];
				request.Parameters.Add("from_postcode", postalCodeFrom);
				request.Parameters.Add("to_postcode", customerPostalCode);
				request.Parameters.Add("length", Math.Round(orderLengthCulture, 2).ToString());
				request.Parameters.Add("width", Math.Round(orderWidthCulture, 2).ToString());
				request.Parameters.Add("height", Math.Round(orderHeightCulture, 2).ToString());
			}
			else
			{
				request.ShippingUrlBase = helper.Settings["ServiceUrlInternational"];
				request.Parameters.Add("country_code", orderInfo.CustomerInfo.CountryCode);
			}

			request.Parameters.Add("weight", Math.Round(orderWeightCulture, 2).ToString());

			var requestHeader = new WebHeaderCollection {{"AUTH-KEY", helper.Settings["authKey"]}};

			Log.Instance.LogDebug("AUSPOST API URL: " + request.ShippingUrlBase);
			Log.Instance.LogDebug("AUSPOST API ParametersAsString: " + request.ParametersAsString);
			Log.Instance.LogDebug("AUSPOST API requestHeader AUTH-KEY: " + requestHeader.GetValues("AUTH-KEY"));

			var issuerRequest = _requestSender.GetRequest(request.ShippingUrlBase, request.ParametersAsString, requestHeader);


			XNamespace ns = string.Empty;
			var issuerXml = XDocument.Parse(issuerRequest);

			foreach (var service in issuerXml.Descendants(ns + "service"))
			{
				var issuerId = service.Descendants(ns + "code").First().Value;
				var issuerName = service.Descendants(ns + "name").First().Value;
				var issuerPriceValue = service.Descendants(ns + "price").First().Value;

				decimal issuerPrice;

				decimal.TryParse(issuerPriceValue, out issuerPrice);

				var priceInCents = issuerPrice*100;

				var paymentImageId = 0;

				var logoDictionaryItem = library.GetDictionaryItem(issuerId + "LogoId");

				if (string.IsNullOrEmpty(logoDictionaryItem))
				{
					int.TryParse(library.GetDictionaryItem(issuerId + "LogoId"), out paymentImageId);
				}

				methods.Add(new ShippingProviderMethod {Id = issuerId, Description = issuerName, Title = issuerName, Name = issuerName, ProviderName = GetName(), ImageId = paymentImageId, PriceInCents = (int) priceInCents, Vat = 21});
			}

			return methods;
		}
	}
}