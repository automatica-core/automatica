import { async, ComponentFixture, TestBed } from "@angular/core/testing";

import { VisuPageListComponent } from "./visu-page-list.component";

describe("VisuPageListComponent", () => {
  let component: VisuPageListComponent;
  let fixture: ComponentFixture<VisuPageListComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ VisuPageListComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(VisuPageListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
