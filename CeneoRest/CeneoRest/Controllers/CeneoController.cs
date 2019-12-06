﻿using System;
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
            var result = await _ceneoHandler.HandleSearchRequest(products);
            return new JsonResult(result);
        }
        [HttpGet("test")]
        public async Task<IActionResult> Test()
        {
            var products = new List<ProductDto>
            {
                new ProductDto {num = 2,max_price = 1000,min_price = 100,min_reputation = 4,name = "telefon"},
                new ProductDto {num = 1,max_price = 100,min_price = 40,min_reputation = 1,name = "etui+na+telefon"},
                new ProductDto {num = 3,max_price = 200, min_price = 10, min_reputation = 3, name = "kubek"}
            };

            var result = await _ceneoHandler.HandleSearchRequest(products);
            return new JsonResult(result);
        }
    }
}
