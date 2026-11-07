import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';

import { TelegramMonitorComponent } from './telegram-monitor.component';

// Stale CLI scaffold: it declares the component without its module, providers or templates and never ran.
// Kept as pending until it gets a real setup (see the logic editor / login form specs for the test helpers).
xdescribe('TelegramMonitorComponent', () => {
  let component: TelegramMonitorComponent;
  let fixture: ComponentFixture<TelegramMonitorComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ TelegramMonitorComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TelegramMonitorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
