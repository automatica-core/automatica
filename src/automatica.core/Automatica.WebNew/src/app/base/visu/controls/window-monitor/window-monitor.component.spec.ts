import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { WindowMonitorComponent } from './window-monitor.component';

describe('WindowMonitorComponent', () => {
  let component: WindowMonitorComponent;
  let fixture: ComponentFixture<WindowMonitorComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ WindowMonitorComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(WindowMonitorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
