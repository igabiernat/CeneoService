using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CeneoRest.Ceneo;
using CeneoRest.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CeneoRest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CeneoController : ControllerBase
    {
        private readonly CeneoHandler _ceneoHandler = new CeneoHandler();

        public CeneoController()
        { 
        }

        // GET: api/Ceneo
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "ceneo", "value" };
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] List<ProductDto> products)
        {
            //var result = await _ceneoHandler.HandleSearchRequest(products);
            var results = new List<SearchResult>();
            foreach (var product in products)
            {
                results.Add(new SearchResult
                {
                    Name = product.name,
                    Info = "info",
                    Price = product.max_price ?? 0,
                });
            }

            return new JsonResult(results);
        }
        [HttpGet("test")]
        public async Task<IActionResult> Test()
        {
            //var products = new List<ProductDto>
            //{
            //    new ProductDto {num = 2,max_price = 1000,min_price = 100,min_reputation = 4,name = "telefon"},
            //    new ProductDto {num = 1,max_price = 100,min_price = 40,min_reputation = 1,name = "etui+na+telefon"}
            //};
            //var result = await _ceneoHandler.HandleSearchRequest(products);
            var results = new List<SearchResult>
            {
                new SearchResult
                {
                    Name = "nazwa", Info = "info", Link = "https://ceneo.pl", Price = 10, SellersName = "auchan",
                    ShippingCost = 2.5M
                },
                new SearchResult
                {
                    Name = "nazwa2", Info = "info2", Link = "https://ceneo.pl", Price = 15, SellersName = "auchan",
                    ShippingCost = 0
                },
                new SearchResult
                {
                    Name = "nazwa3", Info = "info3", Link = "https://ceneo.pl", Price = 215.42M, SellersName = "tesco",
                    ShippingCost = 10
                },
            };

            return new JsonResult(results);
        }
    }
}
