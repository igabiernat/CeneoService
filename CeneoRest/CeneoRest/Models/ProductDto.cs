using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CeneoRest.Models
{
    public class ProductDto
    {
        public string Name { get; set; }
        public int? Num { get; set; }
        public Decimal? MinPrice { get; set; }
        public Decimal? MaxPrice { get; set; }
        public Decimal? MinReputation { get; set; }
    }
}
