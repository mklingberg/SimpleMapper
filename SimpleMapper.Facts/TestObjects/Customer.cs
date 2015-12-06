using System.Collections.Generic;

namespace SimpleMapper.Facts.TestObjects{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Orders> Orders { get; set; }
    }
}