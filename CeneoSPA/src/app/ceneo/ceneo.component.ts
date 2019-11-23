import { Component, OnInit, Input } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AppComponent } from "src/app/app.component";

@Component({
  selector: 'app-ceneo',
  templateUrl: './ceneo.component.html',
  styleUrls: ['./ceneo.component.css']
})


export class CeneoComponent implements OnInit {
  ceneoApiInfo: any;
  product1 =  new Product();
  product2 =  new Product();
  product3 =  new Product();
  product4 =  new Product();
  product5 =  new Product();
  input = [this.product1, this.product2, this.product3, this.product4, this.product5];
  
  public search1 = true;
  public submit = false;

  

  constructor(private http: HttpClient) { 
    
  }
  //TODO: jeden produkt do jednej zmiennej
  //czy nazwy produktow do zmiennej itd.
  //TODO: wartość submit to componentu app zaby
  //zmienic karte
  //TODO: obsługa odpowiedzi
  ngOnInit() {
    this.getCeneoApiInfo();
  }
  postOnCeneoApi(){
    this.http.post('http://localhost:5000/api/ceneo', this.input).subscribe(
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
  getCeneoApiInfo(){
    this.http.get('http://localhost:5000/api/ceneo').subscribe(response => {
      this.ceneoApiInfo = response;
    }, error => {
      console.log(error);
    });
  }


  //onEnter (value: string) {this.input.push(value);}
  
 
  onClickMe(value1: string, value2: string, value3: string, value4: string, value5: string,
    no1:number, no2:number, no3:number, no4:number, no5:number,
    min1: number, min2: number, min3: number, min4: number, min5: number,
    max1:number, max2:number, max3:number, max4:number, max5:number,){
    this.input.length = 0;
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
  }
}
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
