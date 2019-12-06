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
using System.Text.RegularExpressions;

namespace CeneoRest.Ceneo
{
    public  class CeneoHandler
    {
        private readonly List<string> _usedSellers = new List<string>();
        private readonly List<SearchResult> _searchResults = new List<SearchResult>();
        private List<SearchResult> _allProducts = new List<SearchResult>();
        private int _errorCounter = 0;
        private int _errorProductCounter = 0;
        private int _errorLimit = 20;

        public async Task<IActionResult> HandleSearchRequest(List<ProductDto> products)
        {


            var usedSellers = new List<string>(); //Do tej listy zapiszemy sprzedawcow u ktorych wybralismy juz produkty. Zrobimy to po to, by kazdy nastepny produkt u tego samego sprzedawcy mial wysylke za 0.
            Dictionary <string, List<string>> sellersProducts = new Dictionary<string, List<string>>();
            foreach (var product in products)
            {
                _errorCounter = 0;
                _errorProductCounter = 0;
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
                var uri = $"http://ceneo.pl/szukaj-{productDto.name.Replace(' ', '+')};m{productDto.min_price};n{productDto.max_price};0112-0.htm";

                var pageContents = await ScrapPage(uri);
                //WriteHtmlToFile(productDto.name.Trim(), pageContents); //TODO DELETE BEFORE RELEASE
                var pageDocument = new HtmlDocument();
                pageDocument.LoadHtml(pageContents);
                //pageDocument.Load("CeneoHTML.html");    //na razie z pliku
                Log.Fatal(uri);
                var result = await CalculateBestSearchResult(pageDocument, productDto);

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

        private async Task<List<SearchResult>> CalculateBestSearchResult(HtmlDocument pageDocument, ProductDto productDto)
        {
            var shops = pageDocument.DocumentNode.SelectNodes("//a[@class = 'js_seoUrl js_clickHash go-to-product']");
            decimal minPrice = decimal.MaxValue;
            int index = 0;
            for (int i = 0; i<shops.Count; i = i + 2)
            {
                var startingPriceString = shops[i].Descendants("span")
                    .First(node => node.GetAttributeValue("class", "")
                    .Equals("price-format nowrap")).FirstChild.InnerText;

                decimal startingPrice = decimal.Parse(startingPriceString);

                if (startingPrice < minPrice)
                {
                    minPrice = startingPrice;
                    index = i;
                }
                    
            }
            var productId = shops[index].GetAttributeValue("href","error");

            if (productId == "error")
            {
                throw new Exception("CalculateBestSearchResult() error: productId not found");
            }

            var offersCountedIntoAlgorithm = 5;

            var result = await GetSearchResultsForId(productId, productDto, offersCountedIntoAlgorithm);

            //var result = new SearchResult { Info = "Test", Price = 9.5M, ShippingCost = 1M, Name = "Testowy", Link = "https://www.ceneo.pl/", SellersName = "RTV EURO AGD"};
            //_usedSellers.Add(result.SellersName);
            return result;
        }

        private async Task<List<SearchResult>> GetSearchResultsForId(string productId, ProductDto productDto, int offerscounted = 5)
        {
            var uri = $"https://www.ceneo.pl{productId.Trim()}";
            var pageDocument = new HtmlDocument();
            try
            {
                var pageContents = await ScrapPage(uri);
                //WriteHtmlToFile(productId, pageContents); //TODO DELETE BEFORE RELEASE
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
                var ratingString = shopChosen //pobranie ilości gwiazdek
                    .Descendants("span").First(node => node.GetAttributeValue("class", "")
                        .Equals("screen-reader-text")).InnerText;

                ratingString = ratingString.Substring(0, ratingString.IndexOf("/"));
                ratingString = Regex.Replace(ratingString, "[A-Za-z]", "");
                decimal rating = decimal.Parse(ratingString);

                var numberOfRatingsString = shopChosen    //pobranie ilości opinii
                    .Descendants("span").First(node => node.GetAttributeValue("class", "")
                        .Equals("dotted-link js_mini-shop-info js_no-conv")).InnerText;

                numberOfRatingsString = Regex.Replace(numberOfRatingsString, "[A-Za-z]", "");
                decimal numberOfRatings = decimal.Parse(numberOfRatingsString);

                String shipInfoString = GetShipString(shopChosen);
                if (shipInfoString.Contains("szczeg", StringComparison.CurrentCultureIgnoreCase))
                    continue;

                if (rating < 4)
                    continue;

                if (numberOfRatings < 20)
                    continue;


                var name = pageDocument.DocumentNode
                    .Descendants("h1").First(node => node.GetAttributeValue("class", "")
                        .Contains("product-name js_product-h1-link")).InnerText;

                var searchResult = CreateSearchResult(shopChosen, name);
                productSearchResults.Add(searchResult);
                _allProducts.Add(searchResult);

            }

            if (productSearchResults.Count == 0)
            {
                var searchResult = CreateSearchResult(shopsList[0], productDto.name);
                productSearchResults.Add(searchResult);
            }

            _searchResults.Add(productSearchResults[0]);


            return productSearchResults;
        }

        private String GetShipString(HtmlNode ShopChosen)
        {
            var shipString = ShopChosen.Descendants("div")
                                .First(node => node.GetAttributeValue("class", "")
                                .Equals("product-delivery-info js_deliveryInfo")).InnerText;
            return shipString;
        }

        private SearchResult CreateSearchResult(HtmlNode shopChosen, string name)
        {
            var sellersName = shopChosen.GetAttributeValue("data-shopurl", "");

            var priceString = shopChosen
                .Descendants("span").First(node => node.GetAttributeValue("class", "")
                    .Equals("price-format nowrap")).FirstChild.InnerText;

            decimal price = decimal.Parse(priceString);

            var shipInfoString = GetShipString(shopChosen);

            decimal ship = 0;

            if (shipInfoString.Contains("Darmowa",StringComparison.OrdinalIgnoreCase))
            {
                ship = 0;
            }
            else
            {
                String withShippingString = Regex.Replace(shipInfoString, "[A-Za-złą]", "");
                decimal withShipping = decimal.Parse(withShippingString);
                ship = withShipping - price;
            }


            return new SearchResult
            {
                Name = name,
                Price = price,
                SellersName = sellersName,
                ShippingCost = ship,
            };
        }

        private async Task<string> ScrapPage(string uri)
        {
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(uri);
            var content = doc.Text;

            return content;
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
