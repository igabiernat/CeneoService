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
        private List<string> usedSellers = new List<string>();
        public IActionResult HandleSearchRequest(List<Product> products)
        {
            var usedSellers = new List<string>(); //Do tej listy zapiszemy sprzedawcow u ktorych wybralismy juz produkty. Zrobimy to po to, by kazdy nastepny produkt u tego samego sprzedawcy mial wysylke za 0.
            var searchResults = new List<SearchResult>(); //Do tej listy zapiszemy wybrane przez nas produkty, który zwrócimy do klienta.
            //PARALLEL CZYLI WIELOWĄTKOWO - "na raz wyślemy zapytania o wszystkie produkty, a nie będziemy czekać po kolei na każdy." Jak nie zadziala to zrobimy normalnie.
            Parallel.ForEach(products, async product =>
            {
                var uri = $"https://www.ceneo.pl/;szukaj-{product.name.Trim()}";
                var pageContents = await ScrapPage(uri);
                HtmlDocument pageDocument = new HtmlDocument();
                //pageDocument.LoadHtml(pageContents);
                pageDocument.Load("CeneoHTML.html");    //na razie z pliku
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
            var pageContents = await httpClient.GetStringAsync(uri);
            return pageContents;
        }

        private SearchResult CalculateBestSearchResult(HtmlDocument pageDocument)
        {
            //throw new NotImplementedException();
            //TODO

            var productsList = pageDocument.DocumentNode.Descendants("strong") //wczytywanie listy produktów    
                .Where(node => node.GetAttributeValue("class","")
                .Equals("cat-prod-row-name")).ToList();

            var productInfo = productsList[1].SelectSingleNode("a");   //wybranie pierwszego produktu (który nie jest sponsorowany) //TODO - wybieranie produktu który ma powyżej 5 sklepów
            
            var id = productInfo.GetAttributeValue("href", ""); //pobieranie id produktu //TODO - generowanie url, który przeniesie do wybranego produktu

            HtmlDocument chosenProduct = new HtmlDocument();
            chosenProduct.Load("productHTML.html"); //na razie z pliku bo inaczej nie działa :)

            var shopsList = chosenProduct.DocumentNode.Descendants("tr")    //wczytywanie listy sklepów oferujących sprzedaż wybranego produktu
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("product-offer clickable-offer js_offer-container-click  js_product-offer")).ToList();
            var shopChosen = shopsList[1];  //wybór sklepu //TODO - wybór najlepszego sklepu

            var sellersName = shopChosen.GetAttributeValue("data-shopurl", ""); //pobranie nazwy sprzedającego

            var rating = shopChosen.Descendants("span") //pobranie ilości gwiazdek
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("screen-reader-text")).First().InnerText;

            var numberOfRatings = shopChosen.Descendants("span")    //pobranie ilości opinii
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("dotted-link js_mini-shop-info js_no-conv")).First().InnerText;
            
            var ship = shopChosen.Descendants("div")    //pobranie informacji o wysyłce //TODO
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("product-delivery-info js_deliveryInfo")).First().InnerText;

            var price = shopChosen.Descendants("span")  //pobranie ceny produktu
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("price-format nowrap")).First().FirstChild.InnerText;
          
            var result = new SearchResult { Info = "Test", Price = 9.5M, ShippingCost = 1M, Name = "Testowy", Link = "https://www.ceneo.pl/", SellersName = "RTV EURO AGD"};
            usedSellers.Add(result.SellersName);
            return result;
        }
    }
}
