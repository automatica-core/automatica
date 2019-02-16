import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { UsergroupConfigComponent } from './usergroup-config.component';

describe('UsergroupConfigComponent', () => {
  let component: UsergroupConfigComponent;
  let fixture: ComponentFixture<UsergroupConfigComponent>;

  beforeEach(async(() => {
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
