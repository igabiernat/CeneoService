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
  input = [];  
  public search1 = true;
  public submit = false;

 
  onClickMe(){
    this.search1 = false;
    this.submit = true;
  }
  

  constructor(private http: HttpClient) { 
    this.input = [];
  }
  //TODO: jeden produkt do jednej zmiennej
  //czy nazwy produktow do zmiennej itd.
  ngOnInit() {
    this.getCeneoApiInfo();
  }

  getCeneoApiInfo(){
    this.http.get('http://localhost:5000/api/ceneo').subscribe(response => {
      this.ceneoApiInfo = response;
    }, error => {
      console.log(error);
    });
  }


  onEnter (value: string) {this.input.push(value);}
}
