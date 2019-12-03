using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net;
using System.IO;
using System.Net.Http.Headers;
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
        private int _errorProductCounter = 0;
        private int _errorLimit = 20;

        public async Task<IActionResult> HandleSearchRequest(List<ProductDto> products)
        {


            var usedSellers = new List<string>(); //Do tej listy zapiszemy sprzedawcow u ktorych wybralismy juz produkty. Zrobimy to po to, by kazdy nastepny produkt u tego samego sprzedawcy mial wysylke za 0.
            foreach (var product in products)
            {
                _errorCounter = 0;
                _errorProductCounter = 0;
                Log.Information($"{product.name} foreach start");
                await GetSearchResult(product);
                Log.Information($"{product.name} foreach stop ");
            }

            Log.Information("STOP");
            _searchResults.Add(new SearchResult { Name = "testowy", Price = 9.5M });
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
                if (_errorCounter < _errorLimit)
                {
                    await GetSearchResult(productDto);
                }
                else
                {
                    Log.Fatal($"Maximum {_errorLimit} tries exceeded. Exception: {e.Message}");
                }
            }
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

            var result = new SearchResult { Info = "Test", Price = 9.5M, ShippingCost = 1M, Name = "Testowy", Link = "https://www.ceneo.pl/", SellersName = "RTV EURO AGD"};
            _usedSellers.Add(result.SellersName);
            return result;
        }

        private async Task<List<SearchResult>> GetSearchResultsForId(string productId, ProductDto productDto, int offerscounted = 5)
        {
            var uri = $"https://www.ceneo.pl/{productId.Trim()}";
            var pageDocument = new HtmlDocument();
            try
            {
                var pageContents = await ScrapPage(uri);
                WriteHtmlToFile(productId, pageContents); //TODO DELETE BEFORE RELEASE
                pageDocument.LoadHtml(pageContents);
            }
            catch (Exception e)
            {
                _errorProductCounter++;
                Log.Error($"Error {_errorProductCounter} for productId {productId} occured: {e.Message}");
                if (_errorProductCounter < _errorLimit)
                {
                    await GetSearchResultsForId(productId, productDto, offerscounted);
                }
                else
                {
                    Log.Fatal($"Maximum {_errorLimit} tries exceeded for productId {productId}. Exception: {e.Message}");
                }
            }

            var shopsList = pageDocument.DocumentNode.Descendants("tr")    //wczytywanie listy sklepów oferujących sprzedaż wybranego produktu
                .Where(node => node.GetAttributeValue("class", "")
                    .Contains("product-offer clickable-offer js_offer-container-click")).ToList();

            var productSearchResults = new List<SearchResult>();
            for (int i = 0; i < shopsList.Count; i++)
            {
                if (i >= offerscounted)
                    break;
    
                var shopChosen = shopsList[i];
                var rating = shopChosen //pobranie ilości gwiazdek
                    .Descendants("span").First(node => node.GetAttributeValue("class", "")
                        .Equals("screen-reader-text")).InnerText;

                var numberOfRatings = shopChosen    //pobranie ilości opinii
                    .Descendants("span").First(node => node.GetAttributeValue("class", "")
                        .Equals("dotted-link js_mini-shop-info js_no-conv")).InnerText;

                //"Ocena 5 / 5"
                //if (rating.Trim()[0] < 4)
                //{
                //    continue;
                //}
                //else if (Int32.Parse(numberOfRatings.Replace(" opinii", "")) < 20)
                //{
                //    continue;
                //}

                var name = pageDocument.DocumentNode
                    .Descendants("h1").First(node => node.GetAttributeValue("class", "")
                        .Contains("product-name js_product-h1-link")).InnerText;

                var searchResult = CreateSearchResult(shopChosen, name);
                productSearchResults.Add(searchResult);
            }

            if (productSearchResults.Count == 0)
            {
                var searchResult = CreateSearchResult(shopsList[0], productDto.name);
                productSearchResults.Add(searchResult);
            }

            _searchResults.Add(productSearchResults[0]);


            return productSearchResults;
        }

        private SearchResult CreateSearchResult(HtmlNode shopChosen, string name)
        {
            var sellersName = shopChosen.GetAttributeValue("data-shopurl", "");

            var price = shopChosen
                .Descendants("span").First(node => node.GetAttributeValue("class", "")
                    .Equals("price-format nowrap")).FirstChild.InnerText;

            var ship = shopChosen
                .Descendants("div").First(node => node.GetAttributeValue("class", "")
                    .Equals("product-delivery-info js_deliveryInfo")).InnerText;

            if (ship.Contains("Darmowa",StringComparison.OrdinalIgnoreCase))
            {
                ship = "0";
            }
            else if (ship.Contains("szczeg", StringComparison.CurrentCultureIgnoreCase))
            {
                ship = "15";
            }
            else
            {
                ship = (Decimal.Parse(ship.Replace("z wysyłką od ","").Replace(" zł","").Trim()) - Decimal.Parse(price.Trim())).ToString();
            }


            return new SearchResult
            {
                Name = name,
                Price = Decimal.Parse(price.Trim()),
                SellersName = sellersName,
                ShippingCost = Decimal.Parse(ship.Trim()),
            };
        }

        private async Task<string> ScrapPage(string uri)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(uri);
            var contents = await response.Content.ReadAsStringAsync();
            if (contents.Contains("nieprawidłowa domena dla klucza witryny"))
            {
                Log.Error("CAPTCHA SHOWED - nothing to do here");
                throw new Exception("CAPTCHA SHOWED - nothing to do here");
            }
            return contents;
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
