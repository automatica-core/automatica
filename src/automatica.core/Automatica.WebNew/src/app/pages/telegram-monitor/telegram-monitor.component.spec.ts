import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TelegramMonitorComponent } from './telegram-monitor.component';

describe('TelegramMonitorComponent', () => {
  let component: TelegramMonitorComponent;
  let fixture: ComponentFixture<TelegramMonitorComponent>;

  beforeEach(async(() => {
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
