import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';

import { LicenseComponent } from './license.component';

// Stale CLI scaffold: it declares the component without its module, providers or templates and never ran.
// Kept as pending until it gets a real setup (see the logic editor / login form specs for the test helpers).
xdescribe('LicenseComponent', () => {
  let component: LicenseComponent;
  let fixture: ComponentFixture<LicenseComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ LicenseComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LicenseComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
