/* tslint:disable:no-unused-variable */
import { waitForAsync, ComponentFixture, TestBed } from "@angular/core/testing";
import { By } from "@angular/platform-browser";
import { DebugElement } from "@angular/core";

import { ConfigTreeComponent, formatTreeValue } from "./config-tree.component";

// Stale CLI scaffold: it declares the component without its module, providers or templates and never ran.
// Kept as pending until it gets a real setup (see the logic editor / login form specs for the test helpers).
xdescribe("ConfigTreeComponent", () => {
  let component: ConfigTreeComponent;
  let fixture: ComponentFixture<ConfigTreeComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ConfigTreeComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ConfigTreeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});

describe("config tree value", () => {
  it("formatTreeValue_FormatsByType", () => {
    expect(formatTreeValue(undefined)).toBe("");
    expect(formatTreeValue(null)).toBe("");
    expect(formatTreeValue("")).toBe("");
    expect(formatTreeValue(0)).toBe("0");
    expect(formatTreeValue(22.6)).toBe("22.6");
    expect(formatTreeValue(75937.51123)).toBe("75937.51");
    expect(formatTreeValue(true)).toBe("true");
    expect(formatTreeValue("unlocked")).toBe("unlocked");
    expect(formatTreeValue({ a: 1 })).toBe("{…}");
  });
});
