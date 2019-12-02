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
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Text;

namespace CeneoRest.Ceneo
{
    public  class CeneoHandler
    {
        private readonly List<string> _usedSellers = new List<string>();
        private readonly List<SearchResult> _searchResults = new List<SearchResult>();
        private int _errorCounter = 0;

        public async Task<IActionResult> HandleSearchRequest(List<ProductDto> products)
        {


            var usedSellers = new List<string>(); //Do tej listy zapiszemy sprzedawcow u ktorych wybralismy juz produkty. Zrobimy to po to, by kazdy nastepny produkt u tego samego sprzedawcy mial wysylke za 0.
            foreach (var product in products)
            {
                Log.Information($"{product.name} foreach start");
                await GetSearchResult(product);
                Log.Information($"{product.name} foreach stop ");
            }

            Log.Information("STOP");
            return new JsonResult(_searchResults);
        }

        private async Task GetSearchResult(ProductDto productDto)
        {
            try
            {
                var uri = $"http://ceneo.pl/szukaj-{productDto.name.Replace(' ', '+')};0112-0.htm";
                var pageContents = await ScrapPage(uri);
                WriteHtmlToFile(productDto.name.Trim(), pageContents); //TODO DELETE BEFORE RELEASE
                var pageDocument = new HtmlDocument();
                pageDocument.LoadHtml(pageContents);
                //pageDocument.Load("CeneoHTML.html");    //na razie z pliku
                var result =await CalculateBestSearchResult(pageDocument, productDto);
                _searchResults.Add(result);
                _errorCounter = 0;
            }
            catch (Exception e)
            {
                _errorCounter++;
                Log.Error($"Error {_errorCounter} for {productDto.name} occured: {e.Message}");
                if (_errorCounter < 10)
                {
                    await GetSearchResult(productDto);
                }
                else
                {
                    Log.Fatal($"Maximum 10 tries exceeded. Exception: {e.Message}");
                    throw new Exception($"Maximum 10 tries exceeded. Exception: {e.Message}");
                }
            }
        }

        private async Task<string> ScrapPage(string uri)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(uri);
            var contents = await response.Content.ReadAsStringAsync();
            return contents;
        }

        private async Task<SearchResult> CalculateBestSearchResult(HtmlDocument pageDocument, ProductDto productDto)
        {
            var productId = pageDocument.DocumentNode.SelectSingleNode("(//div[contains(@class,'cat-prod-row js_category-list-item js_clickHashData js_man-track-event ')])").GetAttributeValue("data-pid","error");
            if (productId == "error")
            {
                throw new Exception("CalculateBestSearchResult() error: productId not found");
            }

            var offersCountedIntoAlgorithm = 5;
            await GetSearchResultsForId(productId, productDto, offersCountedIntoAlgorithm);


            //var productsList = pageDocument.DocumentNode.Descendants("strong") //wczytywanie listy produktów    
            //    .Where(node => node.GetAttributeValue("class","")
            //    .Equals("cat-prod-row-name")).ToList();

            //var productInfo = productsList[1].SelectSingleNode("a");   //wybranie pierwszego produktu (który nie jest sponsorowany) //TODO - wybieranie produktu który ma powyżej 5 sklepów
            
            //var id = productInfo.GetAttributeValue("href", ""); //pobieranie id produktu //TODO - generowanie url, który przeniesie do wybranego produktu

            //HtmlDocument chosenProduct = new HtmlDocument();
            //chosenProduct.Load("productHTML.html"); //na razie z pliku bo inaczej nie działa :)

            //var shopsList = chosenProduct.DocumentNode.Descendants("tr")    //wczytywanie listy sklepów oferujących sprzedaż wybranego produktu
            //    .Where(node => node.GetAttributeValue("class", "")
            //    .Equals("fileName-offer clickable-offer js_offer-container-click  js_product-offer")).ToList();
            //var shopChosen = shopsList[1];  //wybór sklepu //TODO - wybór najlepszego sklepu

            //var sellersName = shopChosen.GetAttributeValue("data-shopurl", ""); //pobranie nazwy sprzedającego

            //var rating = shopChosen //pobranie ilości gwiazdek
            //    .Descendants("span").First(node => node.GetAttributeValue("class", "")
            //    .Equals("screen-reader-text")).InnerText;

            //var numberOfRatings = shopChosen    //pobranie ilości opinii
            //    .Descendants("span").First(node => node.GetAttributeValue("class", "")
            //    .Equals("dotted-link js_mini-shop-info js_no-conv")).InnerText;
            
            //var ship = shopChosen    //pobranie informacji o wysyłce //TODO
            //    .Descendants("div").First(node => node.GetAttributeValue("class", "")
            //    .Equals("fileName-delivery-info js_deliveryInfo")).InnerText;

            //var price = shopChosen  //pobranie ceny produktu
            //    .Descendants("span").First(node => node.GetAttributeValue("class", "")
            //    .Equals("price-format nowrap")).FirstChild.InnerText;
          



            var result = new SearchResult { Info = "Test", Price = 9.5M, ShippingCost = 1M, Name = "Testowy", Link = "https://www.ceneo.pl/", SellersName = "RTV EURO AGD"};
            _usedSellers.Add(result.SellersName);
            return result;
        }

        private async Task<List<SearchResult>> GetSearchResultsForId(string productId, ProductDto productDto, int offerscounted = 5)
        {
            var uri = $"https://www.ceneo.pl/{productId.Trim()}";
            var pageContents = await ScrapPage(uri);
            WriteHtmlToFile(productId, pageContents); //TODO DELETE BEFORE RELEASE
            var pageDocument = new HtmlDocument();
            pageDocument.LoadHtml(pageContents);

            var searchResults = new List<SearchResult>();
            for (int i = 0; i < offerscounted; i++)
            {
                //TODO ASSIGN OFFERS TO
            }

            return searchResults;
        }

        private void WriteHtmlToFile(string fileName, string pageContents)
        {
            using (var writer = File.CreateText(fileName + ".html"))
            {
                writer.Write(pageContents);
            }
        }
    }
}
