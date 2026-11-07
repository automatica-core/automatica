import { TestBed } from "@angular/core/testing";
import { NgZone } from "@angular/core";

import { ViewportService } from "./viewport.service";

/** a MediaQueryList whose change event the test fires */
class FakeMediaQueryList {
  listeners: ((e: { matches: boolean }) => void)[] = [];
  constructor(public media: string, public matches: boolean) { }
  addEventListener(type: string, listener: any) {
    if (type === "change") {
      this.listeners.push(listener);
    }
  }
  fire(matches: boolean) {
    this.matches = matches;
    this.listeners.forEach(a => a({ matches }));
  }
}

describe("ViewportService", () => {
  let query: FakeMediaQueryList;
  let matchMedia: jasmine.Spy;

  function create(matches: boolean) {
    matchMedia = spyOn(window, "matchMedia").and.callFake((media: string) => {
      query = new FakeMediaQueryList(media, matches);
      return <any>query;
    });
    return TestBed.inject(ViewportService);
  }

  it("narrow_AsksForThePhoneBreakpoint", () => {
    create(false);

    expect(matchMedia).toHaveBeenCalledOnceWith("(max-width: 767.98px)");
  });

  it("wideScreen_SplitsHorizontally", () => {
    const viewport = create(false);

    expect(viewport.narrow()).toBeFalse();
    expect(viewport.splitDirection).toBe("horizontal");
  });

  it("phone_StacksVertically", () => {
    const viewport = create(true);

    expect(viewport.narrow()).toBeTrue();
    expect(viewport.splitDirection).toBe("vertical");
  });

  it("resize_AcrossTheBreakpoint_FollowsTheQuery", () => {
    const viewport = create(false);

    query.fire(true);
    expect(viewport.narrow()).toBeTrue();
    expect(viewport.splitDirection).toBe("vertical");

    query.fire(false);
    expect(viewport.narrow()).toBeFalse();
    expect(viewport.splitDirection).toBe("horizontal");
  });

  it("change_IsAppliedInsideTheAngularZone", () => {
    // media query events come from outside the zone, the pages only re-render when the signal is set inside it
    const zone = TestBed.inject(NgZone);
    const run = spyOn(zone, "run").and.callThrough();
    const viewport = create(false);
    let inZone: boolean;
    run.and.callFake(((fn: () => any) => NgZone.prototype.run.call(zone, () => { inZone = NgZone.isInAngularZone(); return fn(); })) as any);

    zone.runOutsideAngular(() => query.fire(true));

    expect(run).toHaveBeenCalled();
    expect(inZone).toBeTrue();
    expect(viewport.narrow()).toBeTrue();
  });

  it("noMatchMedia_StaysWide", () => {
    spyOn(window, "matchMedia").and.returnValue(undefined);

    const viewport = TestBed.inject(ViewportService);

    expect(viewport.narrow()).toBeFalse();
    expect(viewport.splitDirection).toBe("horizontal");
  });

  it("isARootSingleton", () => {
    create(false);

    expect(TestBed.inject(ViewportService)).toBe(TestBed.inject(ViewportService));
  });
});
