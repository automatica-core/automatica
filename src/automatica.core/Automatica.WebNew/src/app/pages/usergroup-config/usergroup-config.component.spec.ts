import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';

import { UsergroupConfigComponent } from './usergroup-config.component';

// Stale CLI scaffold: it declares the component without its module, providers or templates and never ran.
// Kept as pending until it gets a real setup (see the logic editor / login form specs for the test helpers).
xdescribe('UsergroupConfigComponent', () => {
  let component: UsergroupConfigComponent;
  let fixture: ComponentFixture<UsergroupConfigComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ UsergroupConfigComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(UsergroupConfigComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
