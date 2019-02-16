import { async, ComponentFixture, TestBed } from "@angular/core/testing";

import { NodeValueSelectorComponent } from "./node-value-selector.component";

describe("NodeValueSelectorComponent", () => {
  let component: NodeValueSelectorComponent;
  let fixture: ComponentFixture<NodeValueSelectorComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ NodeValueSelectorComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NodeValueSelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
