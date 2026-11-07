import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';

import { AreaConfigComponent } from './area-config.component';

// Stale CLI scaffold: it declares the component without its module, providers or templates and never ran.
// Kept as pending until it gets a real setup (see the logic editor / login form specs for the test helpers).
xdescribe('AreaConfigComponent', () => {
  let component: AreaConfigComponent;
  let fixture: ComponentFixture<AreaConfigComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ AreaConfigComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(AreaConfigComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
