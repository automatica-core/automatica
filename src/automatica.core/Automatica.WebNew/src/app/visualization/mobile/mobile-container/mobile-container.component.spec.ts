import { async, ComponentFixture, TestBed } from "@angular/core/testing";

import { MobileContainerComponent } from "./mobile-container.component";

describe("MobileContainerComponent", () => {
  let component: MobileContainerComponent;
  let fixture: ComponentFixture<MobileContainerComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [MobileContainerComponent]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MobileContainerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
