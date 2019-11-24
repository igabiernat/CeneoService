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
        // GET: api/Ceneo
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "ceneo", "value" };
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] List<Product> products)
        {
            var result = await CeneoHandler.HandleSearchRequest(products);
            return StatusCode( 200,result);
        }
        [HttpGet("test")]
        public async Task<IActionResult> Test()
        {
            var products = new List<Product>
            {
                new Product {num = 2,max_price = 1000,min_price = 100,min_reputation = 4,name = "telefon"},
                new Product {num = 1,max_price = 100,min_price = 40,min_reputation = 1,name = "etui na telefon"}
            };
            var result = await CeneoHandler.HandleSearchRequest(products);
            return StatusCode(200, result);
        }
    }
}
