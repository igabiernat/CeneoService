using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CeneoRest.Models
{
    public class ProductDto
    {
        public string name { get; set; }
        public int? num { get; set; }
        public Decimal? min_price { get; set; }
        public Decimal? max_price { get; set; }
        public Decimal? min_reputation { get; set; }
    }
}
