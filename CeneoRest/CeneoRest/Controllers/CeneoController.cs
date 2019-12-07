using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CeneoRest.Ceneo;
using CeneoRest.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CeneoRest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CeneoController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly CeneoHandler _ceneoHandler = new CeneoHandler();

        public CeneoController(IConfiguration config)
        {
            _config = config;
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
            var result = await _ceneoHandler.HandleSearchRequest(products, _config);
            return new JsonResult(result);
        }
        [HttpGet("test")]
        public async Task<IActionResult> Test()
        {
            var products = new List<ProductDto>
            {
                new ProductDto {Num = 2,max_price = 1000,min_price = 100,min_reputation = 4,Name = "telefon"},
                new ProductDto {Num = 1,max_price = 100,min_price = 40,min_reputation = 1,Name = "etui+na+telefon"},
                new ProductDto {Num = 3,max_price = 200, min_price = 10, min_reputation = 3, Name = "kubek"}
            };

            var result = await _ceneoHandler.HandleSearchRequest(products, _config);
            return new JsonResult(result);
        }
    }
}
