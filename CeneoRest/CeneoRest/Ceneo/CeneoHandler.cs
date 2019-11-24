using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using CeneoRest.Models;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace CeneoRest.Ceneo
{
    public static class CeneoHandler
    {
        public static async Task<IActionResult> HandleSearchRequest(List<Product> products)
        {
            var usedSeller = new List<string>(); //Do tej listy zapiszemy sprzedawcow u ktorych wybralismy juz produkty. Zrobimy to po to, by kazdy nastepny produkt u tego samego sprzedawcy mial wysylke za 0.
            var searchResults = new List<SearchResult>(); //Do tej listy zapiszemy wybrane przez nas produkty, który zwrócimy do klienta.
            //PARALLEL CZYLI WIELOWĄTKOWO - "na raz wyślemy zapytania o wszystkie produkty, a nie będziemy czekać po kolei na każdy." Jak nie zadziala to zrobimy normalnie.
            Parallel.ForEach(products, async product =>
            {
                var uri = $"https://www.ceneo.pl/szukaj-{product.name.Trim()}/";
                var pageContents = await ScrapPage(uri);
                HtmlDocument pageDocument = new HtmlDocument();
                pageDocument.LoadHtml(pageContents);
                CalculateBestSearchResult(pageDocument);
            });
            //var page = await ScrapPage($"".ToLower());
            return new JsonResult(searchResults);
        }

        public static async Task<IActionResult> HandleSearchRequest(string uri)
        {
            //STARA WERSJA, DO USUNIECIA
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

        private static SearchResult CalculateBestSearchResult(HtmlDocument pageDocument)
        {
            //throw new NotImplementedException();
            return new SearchResult {Info = "Test", Price = 9.5M, ShippingCost = 1M, Name = "Testowy"};
        }
    }
}
