import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { VisuPageSelectorComponent } from './visu-page-selector.component';

describe('VisuPageSelectorComponent', () => {
  let component: VisuPageSelectorComponent;
  let fixture: ComponentFixture<VisuPageSelectorComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ VisuPageSelectorComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(VisuPageSelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
