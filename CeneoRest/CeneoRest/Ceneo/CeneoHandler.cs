using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace CeneoRest.Ceneo
{
    public static class CeneoHandler
    {
        public static async Task<IActionResult> HandleOfferRequest(string uri)
        {
            try
            {
                Log.Information($"Request for uri {uri} started");
                var startTime = DateTime.Now;
                var page = await ScrapPage(uri);
                //TODO PARSE PAGE
                //TODO SEARCH FOR DATA AND ASSIGN IT TO result
                
                var result = "RESULT";
                var totalTime = DateTime.Now - startTime;
                Log.Information($"Request handled in {totalTime.TotalSeconds} seconds");
                return new JsonResult(result)
                {
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception e)
            {
                Log.Fatal(e.Message);
                return new JsonResult($"Fatal error: {e.Message}")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
            
            
        }
        private static async Task<string> ScrapPage(string uri)
        {
            var client = new HttpClient();
            var response = await client.GetAsync(uri);
            var pageContents = await response.Content.ReadAsStringAsync();

            return pageContents;
        }
    }
}
