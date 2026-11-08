import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed, fakeAsync, flush, tick } from "@angular/core/testing";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";

import { RECONNECT_BANNER_DELAY_MS, RECONNECT_FULLSCREEN_DELAY_MS, StartingOverlayComponent } from "./starting-overlay.component";
import { AppService } from "src/app/services/app.service";
import { NotifyService } from "src/app/services/notify.service";
import { TranslationConfigService } from "src/app/services/translation-config.service";
import { HubConnectionService } from "src/app/base/communication/hubs/hub-connection.service";
import { FakeTranslateAsyncPipe, createNotifySpy, createTranslateSpy } from "src/app/testing/test-helpers";

describe("StartingOverlayComponent", () => {
  let fixture: ComponentFixture<StartingOverlayComponent>;
  let component: StartingOverlayComponent;
  let appService: AppService;
  let router: { url: string };
  let translationConfig: { init: jasmine.Spy };
  let hubs: { init: jasmine.Spy };

  beforeEach(() => {
    router = { url: "/admin/config" };
    translationConfig = { init: jasmine.createSpy("init").and.resolveTo() };
    hubs = { init: jasmine.createSpy("init").and.resolveTo() };

    TestBed.configureTestingModule({
      declarations: [StartingOverlayComponent],
      imports: [FakeTranslateAsyncPipe],
      providers: [
        AppService,
        { provide: Router, useValue: router },
        { provide: NotifyService, useValue: createNotifySpy() },
        { provide: L10nTranslationService, useValue: createTranslateSpy() },
        { provide: TranslationConfigService, useValue: translationConfig },
        { provide: HubConnectionService, useValue: hubs }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    });
    appService = TestBed.inject(AppService);
    fixture = TestBed.createComponent(StartingOverlayComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => fixture.destroy());

  function banner(): HTMLElement {
    fixture.detectChanges();
    return fixture.nativeElement.querySelector(".reconnect-banner");
  }

  it("a short drop shows nothing and reloads nothing", fakeAsync(() => {
    appService.isStarting = true;
    tick(RECONNECT_BANNER_DELAY_MS - 1);
    appService.isStarting = false;
    flush();

    expect(component.indication).toBe("none");
    expect(banner()).toBeNull();
    expect(translationConfig.init).not.toHaveBeenCalled();
    expect(hubs.init).not.toHaveBeenCalled();
  }));

  it("shows a banner after the grace delay, the full screen overlay only for a long outage", fakeAsync(() => {
    appService.isStarting = true;
    tick(RECONNECT_BANNER_DELAY_MS);

    expect(component.indication).toBe("banner");
    expect(component.fullscreenVisible).toBeFalse();
    expect(banner()).not.toBeNull();

    tick(RECONNECT_FULLSCREEN_DELAY_MS - RECONNECT_BANNER_DELAY_MS);

    expect(component.indication).toBe("fullscreen");
    expect(component.fullscreenVisible).toBeTrue();
    expect(banner()).toBeNull();

    appService.isStarting = false;
    flush();
  }));

  it("hides when the server is back and reloads translations and hubs after a shown outage", fakeAsync(() => {
    appService.isStarting = true;
    tick(RECONNECT_BANNER_DELAY_MS);

    appService.isStarting = false;
    flush();

    expect(component.indication).toBe("none");
    expect(banner()).toBeNull();
    expect(translationConfig.init).toHaveBeenCalledTimes(1);
    expect(hubs.init).toHaveBeenCalledTimes(1);
  }));

  it("a new outage starts the grace delay again", fakeAsync(() => {
    appService.isStarting = true;
    tick(RECONNECT_BANNER_DELAY_MS);
    appService.isStarting = false;
    flush();

    appService.isStarting = true;
    tick(RECONNECT_BANNER_DELAY_MS - 1);
    expect(component.indication).toBe("none");
    tick(1);
    expect(component.indication).toBe("banner");

    appService.isStarting = false;
    flush();
  }));

  it("never shows on the login page", fakeAsync(() => {
    router.url = "/login";
    appService.isStarting = true;
    tick(RECONNECT_FULLSCREEN_DELAY_MS);

    expect(component.indication).toBe("none");
    expect(banner()).toBeNull();

    appService.isStarting = false;
    flush();
  }));
});
