import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { AreasEtsImportComponent } from './areas-ets-import.component';

describe('AreasEtsImportComponent', () => {
  let component: AreasEtsImportComponent;
  let fixture: ComponentFixture<AreasEtsImportComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ AreasEtsImportComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(AreasEtsImportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
