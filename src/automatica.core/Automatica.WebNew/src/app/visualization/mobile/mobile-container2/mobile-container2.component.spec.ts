import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MobileContainer2Component } from './mobile-container2.component';

describe('MobileContainer2Component', () => {
  let component: MobileContainer2Component;
  let fixture: ComponentFixture<MobileContainer2Component>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ MobileContainer2Component ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MobileContainer2Component);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
