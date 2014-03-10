using System.Collections.Generic;
using SuperSimpleWebshop.Domain;
using SuperSimpleWebshop.Domain.Interfaces;

namespace SuperSimpleWebshop.Shipping.Dummy
{
    public class DummyProvider : IShippingProvider
    {
        #region IShippingProvider Members
        public string GetName()
        {
            return "Dummy";
        }

        public List<ShippingProviderMethod> GetAllShippingMethods(string name, Order order)
        {
            var helper = new ShippingConfigHelper(name);

            var shippingMethods = helper.ShippingProviderMethods;

            foreach (var s in shippingMethods)
            {
                s.ProviderName = GetName();
                s.ProviderNodeName = name;
                s.Description = s.Name;
            }

            return shippingMethods;
        }
        #endregion
    }
}
