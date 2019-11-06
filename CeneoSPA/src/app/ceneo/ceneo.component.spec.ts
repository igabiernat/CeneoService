/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { CeneoComponent } from './ceneo.component';

describe('CeneoComponent', () => {
  let component: CeneoComponent;
  let fixture: ComponentFixture<CeneoComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CeneoComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CeneoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
