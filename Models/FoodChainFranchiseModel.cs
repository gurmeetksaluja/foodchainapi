using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoodChainBrandsAPI    
{
    public class FoodChainFranchiseModel
    {
        public int Id { get; set; }
        public int FoodChainId { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PinCode { get; set; }

    }
}
