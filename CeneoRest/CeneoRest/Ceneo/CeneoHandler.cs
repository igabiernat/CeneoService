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
using CeneoRest.resources;
using Microsoft.Extensions.Configuration;

namespace CeneoRest.Ceneo
{
    public  class CeneoHandler
    {
        
        private readonly List<SearchResult> _cheapestSingleResults = new List<SearchResult>();
        private readonly List<SearchResult> _allProducts = new List<SearchResult>();
        private int _errorCounter = 0;
        private int _errorProductCounter = 0;
        private string _mode;

        public async Task<List<SearchResult>> HandleSearchRequest(List<ProductDto> products, IConfiguration config)
        {
            _mode = config.GetSection("Mode").Value;

            RemoveEmptyProducts(products);

            Dictionary <string, List<string>> sellersProducts = new Dictionary<string, List<string>>();
            foreach (var product in products)
            {
                _errorCounter = 0;
                _errorProductCounter = 0;
                Log.Information($"{product.Name} foreach start");
                await GetSearchResult(product);
                Log.Information($"{product.Name} foreach stop ");
            }

            Log.Information("STOP");

            var _sellersProducts = new Dictionary<string, List<SearchResult>>();
            foreach (SearchResult product in _allProducts)
            {
                //Sprawdzamy czy mamy już tego sprzedawcę w słowniku
                if (_sellersProducts.TryGetValue(product.SellersName, out List<SearchResult> searchResults))
                {
                    //Sprawdzamy czy mamy już taki produkt w słowniku
                    if (searchResults.Select(p => p.Info).Contains(product.Info))
                    {
                        var old = searchResults.FirstOrDefault(p => p.Info == product.Info);
                        var oldPrice = old?.Price + old?.ShippingCost;
                        var newPrice = product?.Price + product?.ShippingCost;

                        if (newPrice < oldPrice)
                        {
                            searchResults.Remove(old);
                            searchResults.Add(product);
                        }
                    }
                    else
                    {
                        searchResults.Add(product);
                    }
                }
                else
                {
                    _sellersProducts.Add(product.SellersName, new List<SearchResult>{ product});
                }

                //IGA
                if (!sellersProducts.ContainsKey(product.SellersName))
                    sellersProducts.Add(product.SellersName, new List<string>());
                sellersProducts[product.SellersName].Add(product.Info);
                //if (sellersProducts[product.SellersName].Contains(product.Info)
            }

            //Posortowanie słownika wg. długości list z produktami i przypisanie produktów.
            var sortedLists =_sellersProducts.Values.OrderByDescending(s => s.Count).ToList();
            var groupedShopping = new List<SearchResult>();
            var groupedShoppingTmp = new List<SearchResult>();
            var currentListCount = -1;
            foreach (var list in sortedLists)
            {
                if (list.Count != currentListCount)
                {
                    currentListCount = list.Count;
                    groupedShopping.AddRange(groupedShoppingTmp);
                    groupedShoppingTmp = new List<SearchResult>();
                }
                foreach (var product in list)
                {
                    if (groupedShoppingTmp.Any(p => p.Info == product.Info))
                    {
                        var old = groupedShoppingTmp.FirstOrDefault(p => p.Info == product.Info);
                        if (old.Price > product.Price)
                        {
                            groupedShoppingTmp.Remove(old);
                            groupedShoppingTmp.Add(product);
                        }
                    }
                    else
                    {
                        groupedShopping.Add(product);
                    }
                }
            }
            
            RemoveShippingCostsForSameSeller(_cheapestSingleResults);
            RemoveShippingCostsForSameSeller(groupedShopping);

            MultiplyByQty(_cheapestSingleResults, products);
            MultiplyByQty(groupedShopping, products);

            var groupedShoppingPrice = SumOrderPrice(groupedShopping);
            var cheapestSinglePrice = SumOrderPrice(_cheapestSingleResults);

            if (cheapestSinglePrice >= groupedShoppingPrice)
            {
                return groupedShopping;
            }
            else
            {
                return _cheapestSingleResults;
            }
        }

        private void MultiplyByQty(List<SearchResult> searchResults, List<ProductDto> products)
        {
            foreach (var product in products)
            {
               var result = searchResults.FirstOrDefault(r => r.Info != product.Name);
               if (result is null)
               {
                   searchResults.Add(new SearchResult{Name = product.Name, Info = "Nie znaleziono produktu dla podanych kryteriów"});
               }
               else
               {
                result.Price *= product?.Num ?? 1;
               }
            }
        }

        private void RemoveEmptyProducts(List<ProductDto> products)
        {
            foreach (var product in products)
            {
                if (product.Name is null || product.Name == "")
                {
                    products.Remove(product);
                }
            }
        }

        private decimal SumOrderPrice(List<SearchResult> searchResults)
        {
            decimal price = 0;
            foreach (var searchResult in searchResults)
            {
                price += searchResult.Price + searchResult.ShippingCost;
            }

            return price;
        }

        private void RemoveShippingCostsForSameSeller(List<SearchResult> searchResults)
        {
            var sellersShipping = new Dictionary<string,decimal>();
            foreach (var searchResult in searchResults)
            {
                if (sellersShipping.TryGetValue(searchResult.SellersName, out decimal shippingCost))
                {
                    if (shippingCost > searchResult.ShippingCost)
                            shippingCost = searchResult.ShippingCost;
                }
                else
                {
                    sellersShipping.Add(searchResult.SellersName, searchResult.ShippingCost);
                }

                searchResult.ShippingCost = 0;
            }

            foreach (var searchResult in searchResults)
            {
                sellersShipping.TryGetValue(searchResult.SellersName, out decimal shippingCost);
                searchResult.ShippingCost = shippingCost;
                shippingCost = 0;
            }
        }

        private async Task GetSearchResult(ProductDto productDto)
        {
            try
            {
                var pageDocument = new HtmlDocument();
                if (_mode == "offline")
                {
                    pageDocument.Load($"{productDto.Name}.html");    //na razie z pliku
                }
                else
                {
                    var uri = $"http://ceneo.pl/szukaj-{productDto.Name.Replace(' ', '+')};m{productDto.min_price};n{productDto.max_price};0112-0.htm";
                    var pageContents = await ScrapPage(uri);
                    WriteHtmlToFile(productDto.Name.Trim(), pageContents); //TODO DELETE BEFORE RELEASE
                    pageDocument.LoadHtml(pageContents);
                }
                
                var result = await CalculateBestSearchResult(pageDocument, productDto);
                _errorCounter = 0;
            }
            catch (Exception e)
            {
                _errorCounter++;
                Log.Error($"Error {_errorCounter} for {productDto.Name} occured: {e.Message}");
                if (_errorCounter < Constants.ErrorsLimit)
                {
                    await GetSearchResult(productDto);
                }
                else
                {
                    Log.Fatal($"Maximum {Constants.ErrorsLimit} tries exceeded. Exception: {e.Message}");
                }
            }
        }

        private async Task<List<SearchResult>> CalculateBestSearchResult(HtmlDocument pageDocument, ProductDto productDto)
        {
            var shops = pageDocument.DocumentNode.SelectNodes("//a[@class = 'js_seoUrl js_clickHash go-to-product']");
            var minPrice = decimal.MaxValue;
            int index = 0;
            for (int i = 0; i<shops.Count; i += 2)
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
            var pageDocument = new HtmlDocument();
            try
            {
                if (_mode == "offline")
                {
                    pageDocument.Load($"{productDto.Name}_details.html");
                }
                else
                {
                    var uri = $"https://www.ceneo.pl{productId.Trim()}";
                    var pageContents = await ScrapPage(uri);
                    WriteHtmlToFile($"{productDto.Name}_{productId.Remove(0, 1)}", pageContents); //TODO DELETE BEFORE RELEASE
                    pageDocument.LoadHtml(pageContents);
                }
            }
            catch (Exception e)
            {
                _errorProductCounter++;
                Log.Error($"Error {_errorProductCounter} for productId {productId} occured: {e.Message}");
                if (_errorProductCounter < Constants.ErrorsLimit)
                {
                    await GetSearchResultsForId(productId, productDto, offerscounted);
                }
                else
                {
                    Log.Fatal($"Maximum {Constants.ErrorsLimit} tries exceeded for productId {productId}. Exception: {e.Message}");
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

                var info = productDto.Name;

                var searchResult = CreateSearchResult(shopChosen, name, info);
                productSearchResults.Add(searchResult);
                _allProducts.Add(searchResult);

            }

            if (productSearchResults.Count == 0)
            {
                var info = productDto.Name;
                var searchResult = CreateSearchResult(shopsList[0], productDto.Name, info);
                productSearchResults.Add(searchResult);
            }

            _cheapestSingleResults.Add(productSearchResults[0]);


            return productSearchResults;
        }

        private String GetShipString(HtmlNode shopChosen)
        {
            var shipString = shopChosen.Descendants("div")
                                .First(node => node.GetAttributeValue("class", "")
                                .Equals("product-delivery-info js_deliveryInfo")).InnerText;
            return shipString;
        }

        private SearchResult CreateSearchResult(HtmlNode shopChosen, string name, string productInfo)
        {
            var sellersName = shopChosen.GetAttributeValue("data-shopurl", "");

            var priceString = shopChosen
                .Descendants("span").First(node => node.GetAttributeValue("class", "")
                    .Equals("price-format nowrap")).FirstChild.InnerText;

            var price = decimal.Parse(priceString);

            var shipInfoString = GetShipString(shopChosen);

            decimal ship = 0;

            if (shipInfoString.Contains("Darmowa",StringComparison.OrdinalIgnoreCase))
            {
                ship = 0;
            }
            else
            {
                var withShippingString = Regex.Replace(shipInfoString, "[A-Za-złą]", "");
                var withShipping = decimal.Parse(withShippingString);
                ship = withShipping - price;
            }

            var link = $"http://ceneo.pl{shopChosen.SelectSingleNode("//a[@class = 'btn btn-primary btn-m btn-cta go-to-shop']").GetAttributeValue("href","")}";


            return new SearchResult
            {
                Name = name,
                Info = productInfo,
                Price = price,
                SellersName = sellersName,
                ShippingCost = ship,
                Link = link
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
