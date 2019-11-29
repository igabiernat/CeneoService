using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CeneoRest.Models;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace CeneoRest.Ceneo
{
    public  class CeneoHandler
    {
        private List<string> usedSellers = new List<string>();
        public IActionResult HandleSearchRequest(List<Product> products)
        {
            var usedSellers = new List<string>(); //Do tej listy zapiszemy sprzedawcow u ktorych wybralismy juz produkty. Zrobimy to po to, by kazdy nastepny produkt u tego samego sprzedawcy mial wysylke za 0.
            var searchResults = new List<SearchResult>(); //Do tej listy zapiszemy wybrane przez nas produkty, który zwrócimy do klienta.
            //PARALLEL CZYLI WIELOWĄTKOWO - "na raz wyślemy zapytania o wszystkie produkty, a nie będziemy czekać po kolei na każdy." Jak nie zadziala to zrobimy normalnie.
            Parallel.ForEach(products, async product =>
            {
                var uri = "https://www.ceneo.pl/;szukaj-{product.name.Trim()}";
                //var uri = $"https://www.amazon.com/s?k=samsung+galaxy+s9&ref=nb_sb_noss_2/";
                //var uri = $"http://www.ceneo.pl/Telefony_i_akcesoria;szukaj-samsung+galaxy+s9";
                var pageContents = await ScrapPage(uri);
                HtmlDocument pageDocument = new HtmlDocument();
                pageDocument.LoadHtml(pageContents);
                var result = CalculateBestSearchResult(pageDocument);
                searchResults.Add(result);
                Log.Information("FOREACH STOP");
            });
            //TODO REFACTOR SLEEP
            Thread.Sleep(5000);
            Log.Information("STOP");
            //var page = await ScrapPage($"".ToLower());
            return new JsonResult(searchResults);
        }
   
        private async Task<string> ScrapPage(string uri)
        {
            var httpClient = new HttpClient();
            var pageContents = httpClient.GetStringAsync(uri);
            return await pageContents;
        }

        private SearchResult CalculateBestSearchResult(HtmlDocument pageDocument)
        {
            //throw new NotImplementedException();
            //TODO

            var ProductList = pageDocument.DocumentNode.Descendants("div")
               .Where(node => node.GetAttributeValue("class", "")
               .Equals("category-list-body js_category-list-body js_search-results")).ToList();
            //var pickedProduct = pageDocument.DocumentNode.SelectSingleNode("//div[@class = '\"category-list-body js_category-list-body js_search-results\"']");
            //var pickedProduct = pageDocument.DocumentNode.SelectSingleNode("//div[@class = 'category-list-body js_category-list-body js_search-results']//strong[@class='cat-prod-row-name']/a");
            //var productId = pickedProduct.Attributes["href"].Value;
            //var picked = pageDocument.DocumentNode.Descendants("strong")
            //    .Where(node => node.GetAttributeValue("class","")
            //    .Equals("\"cat-prod-row-name\"")).ToList();

            var result = new SearchResult { Info = "Test", Price = 9.5M, ShippingCost = 1M, Name = "Testowy", Link = "https://www.ceneo.pl/", SellersName = "RTV EURO AGD"};
            usedSellers.Add(result.SellersName);
            return result;
        }
    }
}
