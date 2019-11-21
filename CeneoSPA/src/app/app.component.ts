import { Component,Input, ViewChild, AfterViewInit, Directive } from '@angular/core';
import { CeneoComponent } from "src/app/ceneo/ceneo.component";

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls:['./app.component.css']

})
export class AppComponent{
  title = 'CeneoSPA';
  public search1 = true;
  public search2 = false;
  public submit = false;

 
  onClickMe(){
    this.search1 = false;
    this.submit = true;
  }
  onClickMeBack(){
    this.search1 = true;
    this.submit = false;
  }



/**   onClickMe1(){
    this.search1= true;
    this.search2 = false;
  }
  onClickMe2(){
    this.search2= true;
    this.search1 = false;
  }
  onClickMeSubmit(){
    this.search2= false;
    this.search1 = false;

  }*/


  
}
