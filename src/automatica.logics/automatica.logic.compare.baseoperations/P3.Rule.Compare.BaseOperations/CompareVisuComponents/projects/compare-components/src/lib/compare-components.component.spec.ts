import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CompareComponentsComponent } from './compare-components.component';

describe('CompareComponentsComponent', () => {
  let component: CompareComponentsComponent;
  let fixture: ComponentFixture<CompareComponentsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CompareComponentsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CompareComponentsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
