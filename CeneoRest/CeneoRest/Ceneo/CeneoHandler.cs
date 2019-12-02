﻿using System;
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
        public async Task<IActionResult> HandleSearchRequest(List<Product> products)
        {


            var usedSellers = new List<string>(); //Do tej listy zapiszemy sprzedawcow u ktorych wybralismy juz produkty. Zrobimy to po to, by kazdy nastepny produkt u tego samego sprzedawcy mial wysylke za 0.
            var searchResults = new List<SearchResult>(); //Do tej listy zapiszemy wybrane przez nas produkty, który zwrócimy do klienta.

            foreach (var product in products)
            {
                Log.Information($"{product.name} foreach start");
                var uri = $"http://ceneo.pl/szukaj-{product.name.Replace(' ', '+')};0112-0.htm";
                var pageContents = await ScrapPage(uri);
                WriteHtmlToFile(product, pageContents);
                var pageDocument = new HtmlDocument();
                pageDocument.LoadHtml(pageContents);
                pageDocument.Load("CeneoHTML.html");    //na razie z pliku
                var result = CalculateBestSearchResult(pageDocument);
                searchResults.Add(result);
                Log.Information($"{product.name} foreach stop ");
            }

            Log.Information("STOP");
            return new JsonResult(searchResults);
        }

        private async Task<string> ScrapPage(string uri)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(uri);
            var contents = await response.Content.ReadAsStringAsync();
            return contents;
        }

        private SearchResult CalculateBestSearchResult(HtmlDocument pageDocument)
        {
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

            var rating = shopChosen //pobranie ilości gwiazdek
                .Descendants("span").First(node => node.GetAttributeValue("class", "")
                .Equals("screen-reader-text")).InnerText;

            var numberOfRatings = shopChosen    //pobranie ilości opinii
                .Descendants("span").First(node => node.GetAttributeValue("class", "")
                .Equals("dotted-link js_mini-shop-info js_no-conv")).InnerText;
            
            var ship = shopChosen    //pobranie informacji o wysyłce //TODO
                .Descendants("div").First(node => node.GetAttributeValue("class", "")
                .Equals("product-delivery-info js_deliveryInfo")).InnerText;

            var price = shopChosen  //pobranie ceny produktu
                .Descendants("span").First(node => node.GetAttributeValue("class", "")
                .Equals("price-format nowrap")).FirstChild.InnerText;
          
            var result = new SearchResult { Info = "Test", Price = 9.5M, ShippingCost = 1M, Name = "Testowy", Link = "https://www.ceneo.pl/", SellersName = "RTV EURO AGD"};
            usedSellers.Add(result.SellersName);
            return result;
        }
        private void WriteHtmlToFile(Product product, string pageContents)
        {
            using (var writer = File.CreateText(product.name.Trim() + ".html"))
            {
                writer.Write(pageContents);
            }
        }
    }
}
