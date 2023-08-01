import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CategoryConfigComponent } from './category-config.component';

describe('CategoryConfigComponent', () => {
  let component: CategoryConfigComponent;
  let fixture: ComponentFixture<CategoryConfigComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CategoryConfigComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CategoryConfigComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
