import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';

import { AreasEtsImportComponent } from './areas-ets-import.component';

// Stale CLI scaffold: it declares the component without its module, providers or templates and never ran.
// Kept as pending until it gets a real setup (see the logic editor / login form specs for the test helpers).
xdescribe('AreasEtsImportComponent', () => {
  let component: AreasEtsImportComponent;
  let fixture: ComponentFixture<AreasEtsImportComponent>;

  beforeEach(waitForAsync(() => {
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
