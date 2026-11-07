import { ChangeDetectionStrategy, Component, NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { NavigationEnd, Router } from "@angular/router";
import { Subject } from "rxjs";

import { SideNavigationMenuComponent } from "./side-navigation-menu.component";
import { NavItem, navigation } from "src/app/app-navigation";
import { FakeTranslateAsyncPipe } from "src/app/testing/test-helpers";

@Component({
  template: `<app-side-navigation-menu [items]="items" [compactMode]="compact" (navigated)="navigated.push($event)"
    (expandRequested)="expandRequests = expandRequests + 1"></app-side-navigation-menu>`,
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: false
})
class HostComponent {
  items: NavItem[];
  compact = false;
  navigated: NavItem[] = [];
  expandRequests = 0;
}

/** a copy, the menu writes `expanded` into the items */
function copy(items: NavItem[]): NavItem[] {
  return JSON.parse(JSON.stringify(items));
}

const nested: NavItem[] = [
  { text: "UI.NAV.OVERVIEW", path: "/admin/home", icon: "gauge" },
  {
    text: "UI.NAV.PARENT", icon: "folder", items: [
      { text: "UI.NAV.CHILD_A", path: "/admin/a" },
      { text: "UI.NAV.CHILD_B", path: "/admin/b" }
    ]
  }
];

describe("SideNavigationMenuComponent", () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let router: { url: string, events: Subject<any>, navigateByUrl: jasmine.Spy };

  beforeEach(() => {
    router = { url: "/admin/home", events: new Subject<any>(), navigateByUrl: jasmine.createSpy("navigateByUrl").and.resolveTo(true) };
    TestBed.configureTestingModule({
      declarations: [HostComponent, SideNavigationMenuComponent],
      imports: [FakeTranslateAsyncPipe],
      providers: [{ provide: Router, useValue: router }],
      schemas: [NO_ERRORS_SCHEMA]
    });
  });

  function create(items: NavItem[] = copy(navigation), url = "/admin/home", compact = false) {
    router.url = url;
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    host.items = items;
    host.compact = compact;
    fixture.detectChanges();
  }

  function links(): HTMLAnchorElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll("a.item"));
  }

  function link(path: string): HTMLAnchorElement {
    return fixture.nativeElement.querySelector(`a.item[href="${path}"]`);
  }

  function menu(): SideNavigationMenuComponent {
    return fixture.debugElement.children[0].componentInstance;
  }

  it("render_AllEntriesWithTheirGroupLabels", () => {
    create();

    expect(links().map(a => a.getAttribute("href"))).toEqual([
      "/admin/home", "/visualization", "/admin/logic-editor", "/admin/config", "/admin/area-config", "/admin/category-config",
      "/admin/visualisation", "/admin/telegram-monitor", "/admin/user-config", "/admin/usergroup-config", "/admin/plugins",
      "/admin/satellites", "/admin/logs", "/admin/system"
    ]);
    const groups = Array.from(fixture.nativeElement.querySelectorAll(".group-label")).map((a: HTMLElement) => a.textContent.trim());
    expect(groups).toEqual(["UI.NAV.OPERATE", "UI.NAV.CONFIGURE", "UI.NAV.SYSTEM"]);
    expect(link("/admin/config").querySelector(".item-text").textContent.trim()).toBe("UI.NAV.DEVICES");
    expect(link("/admin/config").getAttribute("title")).toBe("UI.NAV.DEVICES");
    expect(fixture.nativeElement.querySelector(".brand-name").textContent.trim()).toBe("automatica");
  });

  it("render_NoItems_ShowsTheLoadIndicator", () => {
    create([]);

    expect(links().length).toBe(0);
    expect(fixture.nativeElement.querySelector("dx-load-indicator")).not.toBeNull();
  });

  it("active_CurrentPageIsMarked", () => {
    create(copy(navigation), "/admin/config");

    expect(link("/admin/config").classList).toContain("active");
    expect(link("/admin/config").getAttribute("aria-current")).toBe("page");
    expect(link("/admin/home").classList).not.toContain("active");
    expect(link("/admin/home").hasAttribute("aria-current")).toBeFalse();
  });

  it("active_SubPage_KeepsItsEntryActive", () => {
    create(copy(navigation), "/admin/logic-editor/page-1?x=1");

    expect(link("/admin/logic-editor").classList).toContain("active");
  });

  it("active_PrefixOfAnotherPath_IsNotActive", () => {
    // /admin/config must not light up on /admin/configuration
    create(copy(navigation), "/admin/configuration");

    expect(link("/admin/config").classList).not.toContain("active");
  });

  it("active_VisualizationEntry_IsNeverActive", () => {
    create(copy(navigation), "/visualization");

    expect(link("/visualization").classList).not.toContain("active");
  });

  it("navigationEnd_MovesTheActiveEntryOnTheOnPushMenu", () => {
    create();

    router.events.next(new NavigationEnd(1, "/admin/x", "/admin/satellites"));
    fixture.detectChanges();

    expect(link("/admin/satellites").classList).toContain("active");
    expect(link("/admin/home").classList).not.toContain("active");
  });

  it("click_NavigatesAndReportsIt", () => {
    create();

    const event = new MouseEvent("click", { bubbles: true, cancelable: true });
    link("/admin/satellites").dispatchEvent(event);

    expect(event.defaultPrevented).toBeTrue();
    expect(router.navigateByUrl).toHaveBeenCalledOnceWith("/admin/satellites");
    expect(host.navigated.map(a => a.path)).toEqual(["/admin/satellites"]);
  });

  it("parent_Click_TogglesItsChildren", () => {
    create(copy(nested));
    const parent = links()[1];
    expect(parent.getAttribute("aria-expanded")).toBe("false");
    expect(links().length).toBe(2);

    parent.click();
    fixture.detectChanges();

    expect(links().length).toBe(4);
    expect(links()[1].getAttribute("aria-expanded")).toBe("true");
    expect(router.navigateByUrl).not.toHaveBeenCalled();

    links()[1].click();
    fixture.detectChanges();
    expect(links().length).toBe(2);
  });

  it("parent_ChildIsTheCurrentPage_StartsExpanded", () => {
    create(copy(nested), "/admin/b");

    expect(links().length).toBe(4);
    expect(link("/admin/b").classList).toContain("active");
    expect(link("/admin/b").style.getPropertyValue("--depth")).toBe("1");
  });

  it("chevron_Click_TogglesWithoutNavigating", () => {
    create(copy(nested));

    fixture.nativeElement.querySelector(".chevron").dispatchEvent(new MouseEvent("click", { bubbles: true, cancelable: true }));
    fixture.detectChanges();

    expect(links().length).toBe(4);
    expect(host.navigated.length).toBe(0);
  });

  it("compact_HidesTextsAndGroupLabels", () => {
    create(copy(navigation), "/admin/home", true);

    expect(fixture.nativeElement.querySelector("nav.compact")).not.toBeNull();
    expect(fixture.nativeElement.querySelector(".item-text")).toBeNull();
    expect(fixture.nativeElement.querySelector(".group-label")).toBeNull();
    expect(fixture.nativeElement.querySelectorAll(".group-rule").length).toBe(3);
    expect(fixture.nativeElement.querySelector(".brand-name")).toBeNull();
    // the title still names the entry
    expect(link("/admin/config").getAttribute("title")).toBe("UI.NAV.DEVICES");
  });

  it("compact_ParentClick_AsksTheLayoutToExpand", () => {
    create(copy(nested), "/admin/home", true);

    links()[1].click();

    expect(host.expandRequests).toBe(1);
    expect(host.items[1].expanded).toBeTrue();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it("destroy_StopsListeningToTheRouter", () => {
    create();
    const instance = menu();

    fixture.destroy();

    expect(router.events.observed).toBeFalse();
    expect(instance).toBeTruthy();
  });
});
