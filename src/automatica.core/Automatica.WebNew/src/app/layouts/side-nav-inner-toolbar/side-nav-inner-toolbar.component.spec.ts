import { Component, EventEmitter, Input, NO_ERRORS_SCHEMA, Output } from "@angular/core";
import { ComponentFixture, TestBed, fakeAsync, tick, flush } from "@angular/core/testing";
import { BreakpointObserver, BreakpointState } from "@angular/cdk/layout";
import { BehaviorSubject } from "rxjs";

import { SideNavInnerToolbarComponent } from "./side-nav-inner-toolbar.component";
import { navigation } from "src/app/app-navigation";
import { FakeTranslateAsyncPipe, isolateLocalStorage } from "src/app/testing/test-helpers";

const COLLAPSED_KEY = "automaticaNavCollapsed";

@Component({ selector: "app-side-navigation-menu", template: "<ng-content></ng-content>", standalone: false })
class StubMenuComponent {
  @Input() items: any[];
  @Input() compactMode: boolean;
  @Output() navigated = new EventEmitter<any>();
  @Output() expandRequested = new EventEmitter<void>();
}

@Component({ selector: "app-header", template: "", standalone: false })
class StubHeaderComponent {
  @Input() menuToggleEnabled: boolean;
  @Output() menuToggle = new EventEmitter<void>();
}

describe("SideNavInnerToolbarComponent", () => {
  let fixture: ComponentFixture<SideNavInnerToolbarComponent>;
  let mobile: BehaviorSubject<BreakpointState>;
  let wide: boolean;
  let restore: () => void;

  beforeEach(() => {
    restore = isolateLocalStorage(COLLAPSED_KEY);
    mobile = new BehaviorSubject<BreakpointState>({ matches: false, breakpoints: {} });
    wide = true;
    spyOn(window, "matchMedia").and.callFake((q: string) => <any>{ matches: q === "(min-width: 1280px)" ? wide : false, addEventListener() { } });

    TestBed.configureTestingModule({
      declarations: [SideNavInnerToolbarComponent, StubMenuComponent, StubHeaderComponent],
      imports: [FakeTranslateAsyncPipe],
      providers: [{ provide: BreakpointObserver, useValue: { observe: () => mobile } }],
      schemas: [NO_ERRORS_SCHEMA]
    });
  });

  afterEach(() => restore());

  function create() {
    fixture = TestBed.createComponent(SideNavInnerToolbarComponent);
    fixture.componentRef.setInput("menuItems", navigation);
    fixture.detectChanges();
  }

  function menu(): StubMenuComponent {
    return fixture.debugElement.query(a => a.componentInstance instanceof StubMenuComponent).componentInstance;
  }

  function header(): StubHeaderComponent {
    return fixture.debugElement.query(a => a.componentInstance instanceof StubHeaderComponent).componentInstance;
  }

  function shell(): HTMLElement {
    return fixture.nativeElement.querySelector(".shell");
  }

  it("render_MenuWithTheItemsAndTheHeader", () => {
    create();

    expect(menu().items).toBe(navigation);
    expect(menu().compactMode).toBeFalse();
    expect(header().menuToggleEnabled).toBeFalse();
    expect(fixture.nativeElement.querySelector(".collapse")).not.toBeNull();
    expect(fixture.nativeElement.querySelector(".content")).not.toBeNull();
  });

  it("collapsed_NothingStoredOnASmallLaptop_StartsCollapsed", () => {
    wide = false;

    create();

    expect(menu().compactMode).toBeTrue();
    expect(fixture.nativeElement.querySelector(".collapse").getAttribute("title")).toBe("UI.NAV.EXPAND");
  });

  it("collapsed_StoredChoice_WinsOverTheScreenWidth", () => {
    wide = false;
    localStorage.setItem(COLLAPSED_KEY, "0");

    create();

    expect(menu().compactMode).toBeFalse();
  });

  it("collapseButton_TogglesAndRemembers", () => {
    create();

    fixture.nativeElement.querySelector(".collapse").click();
    fixture.detectChanges();

    expect(menu().compactMode).toBeTrue();
    expect(localStorage.getItem(COLLAPSED_KEY)).toBe("1");

    fixture.nativeElement.querySelector(".collapse").click();
    fixture.detectChanges();
    expect(menu().compactMode).toBeFalse();
    expect(localStorage.getItem(COLLAPSED_KEY)).toBe("0");
  });

  it("expandRequested_FromTheCompactMenu_Expands", () => {
    localStorage.setItem(COLLAPSED_KEY, "1");
    create();

    menu().expandRequested.emit();
    fixture.detectChanges();

    expect(menu().compactMode).toBeFalse();
  });

  it("phone_HeaderTogglesTheMenuAndNavigationClosesIt", () => {
    mobile.next({ matches: true, breakpoints: {} });
    create();

    expect(shell().classList).toContain("mobile");
    expect(header().menuToggleEnabled).toBeTrue();
    // never compact on a phone, the menu slides in instead
    expect(menu().compactMode).toBeFalse();
    expect(fixture.nativeElement.querySelector(".collapse")).toBeNull();

    header().menuToggle.emit();
    fixture.detectChanges();
    expect(shell().classList).toContain("menu-open");
    expect(fixture.nativeElement.querySelector(".scrim")).not.toBeNull();

    menu().navigated.emit({});
    fixture.detectChanges();
    expect(shell().classList).not.toContain("menu-open");
  });

  it("phone_ScrimClick_ClosesTheMenu", () => {
    mobile.next({ matches: true, breakpoints: {} });
    create();
    header().menuToggle.emit();
    fixture.detectChanges();

    fixture.nativeElement.querySelector(".scrim").click();
    fixture.detectChanges();

    expect(shell().classList).not.toContain("menu-open");
  });

  it("breakpoint_BackToDesktop_ClosesThePhoneMenu", () => {
    mobile.next({ matches: true, breakpoints: {} });
    create();
    header().menuToggle.emit();
    fixture.detectChanges();

    mobile.next({ matches: false, breakpoints: {} });
    fixture.detectChanges();

    expect(shell().classList).not.toContain("mobile");
    expect(shell().classList).not.toContain("menu-open");
  });

  it("animated_OnlyAfterTheFirstFrames", fakeAsync(() => {
    create();
    expect(shell().classList).not.toContain("animated");

    tick(300);
    fixture.detectChanges();

    expect(shell().classList).toContain("animated");
    flush();
  }));

  it("destroy_StopsObservingTheBreakpoint", () => {
    create();

    fixture.destroy();

    expect(mobile.observed).toBeFalse();
  });
});
