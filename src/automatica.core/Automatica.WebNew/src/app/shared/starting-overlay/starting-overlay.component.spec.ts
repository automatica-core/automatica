import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { StartingOverlayComponent } from './starting-overlay.component';

describe('StartingOverlayComponent', () => {
  let component: StartingOverlayComponent;
  let fixture: ComponentFixture<StartingOverlayComponent>;

  beforeEach(async(() => {
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
