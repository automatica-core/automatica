import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';

import { UserConfigComponent } from './user-config.component';

// Stale CLI scaffold: it declares the component without its module, providers or templates and never ran.
// Kept as pending until it gets a real setup (see the logic editor / login form specs for the test helpers).
xdescribe('UserConfigComponent', () => {
  let component: UserConfigComponent;
  let fixture: ComponentFixture<UserConfigComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ UserConfigComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(UserConfigComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
