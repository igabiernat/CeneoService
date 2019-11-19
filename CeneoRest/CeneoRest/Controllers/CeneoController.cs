using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CeneoRest.Ceneo;
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

        [HttpPost("offer")]
        public async Task<IActionResult> Offer()
        {
            var item = "telefon";
            var result = await CeneoHandler.HandleOfferRequest($"https://www.ceneo.pl/szukaj-{item}/".ToLower());
            return StatusCode( 200,result);
        }
    }
}
