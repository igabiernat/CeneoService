using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    }
}
