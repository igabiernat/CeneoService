using System;
using System.Collections.Generic;
using System.Linq;


namespace CeneoRest.Models
{
    public class SearchResult
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal ShippingCost { get; set; }
        public string Link { get; set; }
        public string Info { get; set; }
        public string SellersName { get; set; }

        public SearchResult Clone()
        {
            return new SearchResult
            {
                Name = this.Name,
                ShippingCost = this.ShippingCost,
                Info = this.Info,
                Price = this.Price,
                SellersName = this.SellersName,
                Link = this.Link
            };
        }
    }
}
