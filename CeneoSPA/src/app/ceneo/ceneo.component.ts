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
  public input = " ";
  
  

  constructor(private http: HttpClient) { }

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


  onEnter (value: string) {this.input = value;}
}
