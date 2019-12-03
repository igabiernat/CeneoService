import { Component,Input,OnInit, ViewChild, AfterViewInit, Directive } from '@angular/core';
import { CeneoComponent } from "src/app/ceneo/ceneo.component";
import { HttpClient } from '@angular/common/http';
import { analyzeAndValidateNgModules } from '@angular/compiler';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls:['./app.component.css']

})
export class AppComponent implements OnInit{
  title = 'CeneoSPA';
  public search1 = true;
  public submit = false;
  ceneoApiInfo: any;
  
  product1 =  new Product();
  product2 =  new Product();
  product3 =  new Product();
  product4 =  new Product();
  product5 =  new Product();
  input = [this.product1, this.product2, this.product3, this.product4, this.product5];
  //arrayResults: SearchResult[];
  //arrayResults = [new SearchResult, new SearchResult, new SearchResult, new SearchResult, new SearchResult]
  public totalPrice = 0;


  constructor(private http: HttpClient) { 
    
  }



  ngOnInit() {
    this.getCeneoApiInfo();
  }


  //## send data ##
  postOnCeneoApi(){
    this.http.post('http://localhost:5000/api/ceneo/test', this.input).subscribe(
      (val) => {
          console.log("POST call successful value returned in body", 
                      val);
      },
      response => {
          console.log("POST call in error", response);
      },
      () => {
          console.log("The POST observable is now completed.");
      });
  }

  //## get results ##
  getCeneoApiInfo(){
    this.http.get('http://localhost:5000/api/ceneo/test').subscribe(response => {
      this.ceneoApiInfo = response;
      //this.arrayResults = JSON.parse(this.ceneoApiInfo);
    }, error => {
      console.log(error);
    });
  }


  //## beginning of summary ##
  summary(){
   
    
      for(var i in this.ceneoApiInfo){
        this.totalPrice += Number(i['price']) + Number(i['shippingCost']);
    }
  }


  //## on tab results, come back to searching ##
  onClickMeBack(){
    this.search1 = true;
    this.submit = false;
  }

  //## button submit, send data, change tab, receive results, summary-total price ##
  onClickMe(value1: string, value2: string, value3: string, value4: string, value5: string,
     no1: number, no2: number, no3: number, no4: number, no5: number, 
     min1: number, min2: number, min3: number, min4: number, min5: number,
     max1: number, max2: number, max3: number, max4: number, max5: number){
    
    this.product1.name = value1;
    this.product2.name = value2;
    this.product3.name = value3;
    this.product4.name = value4;
    this.product5.name = value5;

    this.product1.num = no1;
    this.product2.num = no2;
    this.product3.num = no3;
    this.product4.num = no4;
    this.product5.num = no5;

    this.product1.min_price = min1;
    this.product2.min_price = min2;
    this.product3.min_price = min3;
    this.product4.min_price = min4;
    this.product5.min_price = min5;

    this.product1.max_price = max1;
    this.product2.max_price = max2;
    this.product3.max_price = max3;
    this.product4.max_price = max4;
    this.product5.max_price = max5;

    this.search1 = false;
    this.submit = true;
    this.postOnCeneoApi();
    this.getCeneoApiInfo();
    this.summary();
  }

}

//## class created to easily send data from form ##
class Product{
  name: string;
  num: number;
  min_price: number;
  max_price: number;
  min_reputation: number;
  min_rating_no: number;

  constructor(){
    this.min_reputation = 4;
    this.min_rating_no = 20;
  }
}

//unuseful
// //## class to form received data from rest ##
// class SearchResult{
//   name: string;
//   price: number;
//   shipping: number;
//   link: string;
//   info: string;
//   sellersName: string;

//   constructor(){

//   }
// }
