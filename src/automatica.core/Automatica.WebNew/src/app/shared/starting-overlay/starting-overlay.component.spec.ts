import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';

import { StartingOverlayComponent } from './starting-overlay.component';

// Stale CLI scaffold: it declares the component without its module, providers or templates and never ran.
// Kept as pending until it gets a real setup (see the logic editor / login form specs for the test helpers).
xdescribe('StartingOverlayComponent', () => {
  let component: StartingOverlayComponent;
  let fixture: ComponentFixture<StartingOverlayComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ StartingOverlayComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(StartingOverlayComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
