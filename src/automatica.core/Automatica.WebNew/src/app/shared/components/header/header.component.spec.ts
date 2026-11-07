import { EventEmitter, NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { ActivatedRoute, Router } from "@angular/router";

import { HeaderComponent } from "./header.component";
import { AppService } from "src/app/services/app.service";
import { LoginService } from "src/app/services/login.service";
import { HubConnectionService } from "src/app/base/communication/hubs/hub-connection.service";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { SettingsService, Language as SettingsLanguage } from "src/app/services/settings.service";
import { ThemeService } from "src/app/services/theme.service";
import { TranslationConfigService } from "src/app/services/translation-config.service";
import { DeviceService } from "src/app/services/device/device.service";
import { FakeTranslateAsyncPipe, flushPromises } from "src/app/testing/test-helpers";

describe("HeaderComponent", () => {
  let fixture: ComponentFixture<HeaderComponent>;
  let component: HeaderComponent;
  let router: { url: string, navigate: jasmine.Spy };
  let login: { getCurrentUser: jasmine.Spy, logout: jasmine.Spy };
  let hub: { init: jasmine.Spy, stop: jasmine.Spy };
  let dataHub: { connectionStateChanged: EventEmitter<any>, connected: boolean };
  let settings: { getByKey: jasmine.Spy };
  let theme: { themeChanged: EventEmitter<string>, getCurrentTheme: jasmine.Spy, applyTheme: jasmine.Spy };
  let translation: { applyLanguage: jasmine.Spy };
  let previousTitle: string;

  function user(roles: string[], names: any = { FirstName: "Ada", LastName: "Lovelace", UserName: "ada" }) {
    return { ...names, InverseThis2Roles: roles.map(key => ({ This2RoleNavigation: { Key: key } })) };
  }

  beforeEach(() => {
    previousTitle = document.title;
    router = { url: "/admin/home", navigate: jasmine.createSpy("navigate").and.resolveTo(true) };
    login = { getCurrentUser: jasmine.createSpy("getCurrentUser").and.returnValue(user(["administrator"])), logout: jasmine.createSpy("logout") };
    hub = { init: jasmine.createSpy("init").and.resolveTo(undefined), stop: jasmine.createSpy("stop").and.resolveTo(undefined) };
    dataHub = { connectionStateChanged: new EventEmitter<any>(), connected: true };
    settings = {
      getByKey: jasmine.createSpy("getByKey").and.callFake((key: string) =>
        Promise.resolve({ Value: key === "projectName" ? "Test house" : 0 }))
    };
    theme = {
      themeChanged: new EventEmitter<string>(),
      getCurrentTheme: jasmine.createSpy("getCurrentTheme").and.returnValue("dark"),
      applyTheme: jasmine.createSpy("applyTheme")
    };
    translation = { applyLanguage: jasmine.createSpy("applyLanguage").and.resolveTo(undefined) };

    TestBed.configureTestingModule({
      declarations: [HeaderComponent],
      imports: [FakeTranslateAsyncPipe],
      providers: [
        AppService,
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: {} },
        { provide: LoginService, useValue: login },
        { provide: HubConnectionService, useValue: hub },
        { provide: DataHubService, useValue: dataHub },
        { provide: SettingsService, useValue: settings },
        { provide: ThemeService, useValue: theme },
        { provide: TranslationConfigService, useValue: translation },
        { provide: DeviceService, useValue: { isWeb: true } }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    });
  });

  afterEach(() => {
    document.title = previousTitle;
  });

  async function create() {
    fixture = TestBed.createComponent(HeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await flushPromises(10);
    fixture.detectChanges();
  }

  function text(selector: string): string {
    return fixture.nativeElement.querySelector(selector)?.textContent.trim();
  }

  it("render_TitleProjectAndConnection", async () => {
    TestBed.inject(AppService).setAppTitle("COMMON.SATELLITES");

    await create();

    expect(text(".title")).toBe("COMMON.SATELLITES");
    expect(text(".project")).toBe("Test house");
    expect(document.title).toBe("Test house");
    expect(text(".status-text")).toBe("UI.HEADER.CONNECTED");
    expect(fixture.nativeElement.querySelector(".status.down")).toBeNull();
    expect(text(".avatar")).toBe("AL");
  });

  it("render_NoMenuToggleUnlessEnabled", async () => {
    await create();
    expect(fixture.nativeElement.querySelector(".icon-btn")).toBeNull();

    fixture.componentRef.setInput("menuToggleEnabled", true);
    fixture.detectChanges();
    let toggles = 0;
    component.menuToggle.subscribe(() => toggles++);
    fixture.nativeElement.querySelector(".icon-btn").click();

    expect(toggles).toBe(1);
  });

  it("language_ServerLanguageIsApplied", async () => {
    settings.getByKey.and.callFake((key: string) => Promise.resolve({ Value: key === "projectName" ? "P" : 1 }));

    await create();

    expect(translation.applyLanguage).toHaveBeenCalledOnceWith(SettingsLanguage.English);
  });

  it("language_German", async () => {
    await create();

    expect(translation.applyLanguage).toHaveBeenCalledOnceWith(SettingsLanguage.German);
  });

  it("connection_Lost_IsShown", async () => {
    await create();

    dataHub.connectionStateChanged.emit(false);
    fixture.detectChanges();

    expect(text(".status-text")).toBe("UI.HEADER.DISCONNECTED");
    expect(fixture.nativeElement.querySelector(".status.down")).not.toBeNull();
    expect(fixture.nativeElement.querySelector(".status-dot.bad")).not.toBeNull();
  });

  it("admin_OnAnAdminPage_OffersTheVisualization", async () => {
    await create();

    expect(text(".switch-text")).toBe("UI.HEADER.TO_VISU");
    fixture.nativeElement.querySelector(".switch").click();
    expect(router.navigate).toHaveBeenCalledOnceWith(["visualization"]);
  });

  it("admin_OnTheVisualization_OffersTheAdmin", async () => {
    router.url = "/visualization/page";
    await create();

    expect(text(".switch-text")).toBe("UI.HEADER.TO_ADMIN");
    fixture.nativeElement.querySelector(".switch").click();
    expect(router.navigate).toHaveBeenCalledOnceWith(["admin", "home"]);
  });

  it("admin_NoAdministrator_HasNoSwitch", async () => {
    login.getCurrentUser.and.returnValue(user(["visu"], { UserName: "kiosk" }));

    await create();

    expect(fixture.nativeElement.querySelector(".switch")).toBeNull();
    expect(text(".avatar")).toBe("KI");
  });

  it("userMenu_ShowsTheNameAndSwitchesTheTheme", async () => {
    await create();

    fixture.nativeElement.querySelector(".avatar").click();
    fixture.detectChanges();

    expect(text(".menu-head b")).toBe("Ada Lovelace");
    const [light, dark] = Array.from(fixture.nativeElement.querySelectorAll(".segmented button")) as HTMLButtonElement[];
    expect(dark.getAttribute("aria-pressed")).toBe("true");
    expect(light.getAttribute("aria-pressed")).toBe("false");

    light.click();
    expect(theme.applyTheme).toHaveBeenCalledOnceWith("light");
    theme.themeChanged.emit("light");
    fixture.detectChanges();
    expect(light.getAttribute("aria-pressed")).toBe("true");
  });

  it("userMenu_EscapeOrOutsideClick_Closes", async () => {
    await create();

    component.menuOpen = true;
    document.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape" }));
    expect(component.menuOpen).toBeFalse();

    component.menuOpen = true;
    document.body.click();
    expect(component.menuOpen).toBeFalse();
  });

  it("userMenu_ClickInside_StaysOpen", async () => {
    await create();
    fixture.nativeElement.querySelector(".avatar").click();
    fixture.detectChanges();

    fixture.nativeElement.querySelector(".menu-head").click();

    expect(component.menuOpen).toBeTrue();
  });

  it("logout_StopsTheHubsAndGoesToTheLogin", async () => {
    await create();
    fixture.nativeElement.querySelector(".avatar").click();
    fixture.detectChanges();

    fixture.nativeElement.querySelector(".menu-item").click();
    await flushPromises();

    expect(hub.stop).toHaveBeenCalled();
    expect(login.logout).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(["/login"]);
    expect(component.menuOpen).toBeFalse();
  });

  it("destroy_BeforeTheHubIsUp_LoadsNoSettings", async () => {
    let release: () => void;
    hub.init.and.returnValue(new Promise<void>(r => release = r));
    fixture = TestBed.createComponent(HeaderComponent);
    fixture.detectChanges();

    fixture.destroy();
    release();
    await flushPromises();

    expect(settings.getByKey).not.toHaveBeenCalled();
    expect(dataHub.connectionStateChanged.observed).toBeFalse();
  });
});
