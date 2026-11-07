import { waitForAsync, ComponentFixture, TestBed } from "@angular/core/testing";

import { NodeValueSelectorComponent } from "./node-value-selector.component";

// Stale CLI scaffold: it declares the component without its module, providers or templates and never ran.
// Kept as pending until it gets a real setup (see the logic editor / login form specs for the test helpers).
xdescribe("NodeValueSelectorComponent", () => {
  let component: NodeValueSelectorComponent;
  let fixture: ComponentFixture<NodeValueSelectorComponent>;

  beforeEach(waitForAsync(() => {
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
