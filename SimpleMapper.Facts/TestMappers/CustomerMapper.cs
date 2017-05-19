using System.Linq;
using SimpleMapper.Facts.TestObjects;

namespace SimpleMapper.Facts.TestMappers
{
    public class CustomerMapper : Mapper
    {
        public CustomerMapper()
        {
            Map.From<Customer>().To<CustomerModel>().SetManually((customer, model) =>
            {
                model.Orders = string.Join(",", customer.Orders.Select(x => x.Name));
            });
        }
    }
}