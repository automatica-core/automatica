import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { HttpErrorResponse, provideHttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";

import { SatelliteConfigComponent } from "./satellite-config.component";
import { SatelliteService } from "src/app/services/satellite.services";
import { DesignTimeDataService } from "src/app/services/design-time-data.service";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { Satellite } from "src/app/base/model/satellites/satellite";
import { WebApiException, ExceptionSeverity } from "src/app/base/model/web-api-exception";
import { FakeTranslateAsyncPipe, createNotifySpy, createTranslateSpy, flushPromises, rejected } from "src/app/testing/test-helpers";

const URL = "/webapi/satellite";

function satelliteJson(name: string, extra: any = {}) {
  return {
    TypeInfo: "Satellite", ObjId: "sat-" + name.toLowerCase().replace(/\s/g, "-"), Name: name, Description: "",
    ClientId: "", ClientKey: "", Connected: false, ...extra
  };
}

describe("SatelliteConfigComponent", () => {
  let fixture: ComponentFixture<SatelliteConfigComponent>;
  let component: SatelliteConfigComponent;
  let http: HttpTestingController;
  let notify: jasmine.SpyObj<NotifyService>;
  let translate: jasmine.SpyObj<L10nTranslationService>;
  let savedServer: string;

  beforeEach(() => {
    // BaseServiceHelper prefers a stored server address, the tests talk to the relative /webapi
    savedServer = localStorage.getItem("s1server");
    localStorage.removeItem("s1server");

    notify = createNotifySpy();
    translate = createTranslateSpy({
      "ERROR.SATELLITE_KEY_TOO_SHORT": "The satellite key needs at least 16 characters.",
      "ERROR.UNAUTHORISED": "Unauthorised"
    });

    TestBed.configureTestingModule({
      declarations: [SatelliteConfigComponent],
      imports: [FakeTranslateAsyncPipe],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        SatelliteService,
        { provide: DesignTimeDataService, useValue: {} },
        { provide: Router, useValue: { navigate: jasmine.createSpy("navigate") } },
        { provide: NotifyService, useValue: notify },
        { provide: L10nTranslationService, useValue: translate },
        AppService
      ],
      schemas: [NO_ERRORS_SCHEMA]
    });
    http = TestBed.inject(HttpTestingController);
    // handleError logs every error it reports
    spyOn(console, "log");
  });

  afterEach(() => {
    http.verify();
    if (savedServer !== null) {
      localStorage.setItem("s1server", savedServer);
    }
  });

  async function init(satellites: any[] = [satelliteJson("Garage", { Connected: true, ClientId: "garage-client" }), satelliteJson("Attic")]) {
    fixture = TestBed.createComponent(SatelliteConfigComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    http.expectOne(r => r.method === "GET" && r.url === URL).flush(satellites);
    await flushPromises();
    fixture.detectChanges();
  }

  function rows(): HTMLElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll(".list-pane .li"));
  }

  function text(selector: string): string {
    return fixture.nativeElement.querySelector(selector)?.textContent.trim();
  }

  async function save() {
    const done = component.save();
    await flushPromises();
    return done;
  }

  // ---------------------------------------------------------------------------------------------
  describe("load", () => {
    it("load_ListsTheSatellitesSortedByName", async () => {
      await init();

      expect(component.loaded).toBeTrue();
      expect(component.satellites.every(a => a instanceof Satellite)).toBeTrue();
      expect(rows().map(a => a.querySelector("b").textContent.trim())).toEqual(["Attic", "Garage"]);
      expect(rows()[1].querySelector(".mono").textContent.trim()).toBe("garage-client");
      expect(rows()[1].querySelector(".pill.ok")).not.toBeNull();
      expect(rows()[0].querySelector(".pill.ok")).toBeNull();
      expect(text(".page-head .sub")).toBe("UI.SATELLITES.COUNT " + JSON.stringify({ online: 1, count: 2 }));
      expect(component.isDirty).toBeFalse();
      expect(TestBed.inject(AppService).isLoading).toBeFalse();
    });

    it("constructor_SetsTheAppTitle", async () => {
      await init();

      expect(TestBed.inject(AppService).title).toBe("COMMON.SATELLITES");
    });

    it("load_NoSatellites_ShowsTheEmptyState", async () => {
      await init([]);

      expect(rows().length).toBe(0);
      expect(text(".list-pane .empty-state b")).toBe("UI.SATELLITES.EMPTY");
    });

    it("load_StillRunning_ShowsNoEmptyStateYet", () => {
      fixture = TestBed.createComponent(SatelliteConfigComponent);
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector(".list-pane .empty-state")).toBeNull();
      expect(fixture.nativeElement.querySelector(".page-head .sub")).toBeNull();
      expect(TestBed.inject(AppService).isLoading).toBeTrue();
      http.expectOne(URL).flush([]);
    });

    it("load_ServerError_IsReportedThroughHandleError", async () => {
      fixture = TestBed.createComponent(SatelliteConfigComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();

      http.expectOne(URL).flush({ TypeInfo: "WebApiException", errorText: "SATELLITE_LOAD", severity: ExceptionSeverity.Error },
        { status: 500, statusText: "Server Error" });
      await flushPromises();

      expect(notify.notifyError).toHaveBeenCalledOnceWith("ERROR.SATELLITE_LOAD");
      expect(component.loaded).toBeTrue();
      expect(TestBed.inject(AppService).isLoading).toBeFalse();
    });

    it("load_Reload_KeepsTheSelectionById", async () => {
      await init();
      component.select(component.satellites.find(a => a.Name === "Garage"));

      component.load();
      http.expectOne(URL).flush([satelliteJson("Garage", { Description: "renamed" }), satelliteJson("Attic")]);
      await flushPromises();

      expect(component.selectedSatellite.Name).toBe("Garage");
      expect(component.selectedSatellite.Description).toBe("renamed");
      expect(component.isDirty).toBeFalse();
    });

    it("load_SelectedSatelliteGone_ClearsTheSelection", async () => {
      await init();
      component.select(component.satellites.find(a => a.Name === "Garage"));

      component.load();
      http.expectOne(URL).flush([satelliteJson("Attic")]);
      await flushPromises();

      expect(component.selectedSatellite).toBeUndefined();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("filter", () => {
    beforeEach(async () => {
      await init([
        satelliteJson("Garage", { ClientId: "gw-01" }),
        satelliteJson("attic", { Description: "Upstairs sensor hub" }),
        satelliteJson("Basement")
      ]);
    });

    it("filteredSatellites_NoSearch_AllSortedCaseInsensitive", () => {
      expect(component.filteredSatellites.map(a => a.Name)).toEqual(["attic", "Basement", "Garage"]);
    });

    it("filteredSatellites_MatchesNameDescriptionAndClientId", () => {
      component.search = "GAR";
      expect(component.filteredSatellites.map(a => a.Name)).toEqual(["Garage"]);

      component.search = "upstairs";
      expect(component.filteredSatellites.map(a => a.Name)).toEqual(["attic"]);

      component.search = "  gw-0 ";
      expect(component.filteredSatellites.map(a => a.Name)).toEqual(["Garage"]);
    });

    it("filteredSatellites_NoMatch_ShowsTheNoMatchState", () => {
      component.search = "kitchen";
      fixture.detectChanges();

      expect(rows().length).toBe(0);
      expect(text(".list-pane .empty-state b")).toBe("UI.PAGES.NO_MATCH");
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("save", () => {
    beforeEach(async () => {
      await init();
    });

    it("save_PostsTheWholeListAndReportsSaved", async () => {
      component.satellites[0].ClientKey = "made-up-test-key-0123456789";
      component.onChanged(void 0);

      const done = save();
      const req = http.expectOne(URL);
      expect(req.request.method).toBe("POST");
      expect(req.request.body.map((a: any) => a.Name)).toEqual(component.satellites.map(a => a.Name));
      expect(req.request.body[0].ClientKey).toBe("made-up-test-key-0123456789");
      req.flush([]);
      await done;

      expect(notify.notifySuccess).toHaveBeenCalledOnceWith("COMMON.SAVED");
      expect(component.isDirty).toBeFalse();
      expect(TestBed.inject(AppService).isLoading).toBeFalse();
    });

    it("save_KeyTooShort_ShowsTheTranslatedWarningAndStaysDirty", async () => {
      component.onChanged(void 0);

      const done = save();
      // the server answers a client key below 16 characters with a 400 that carries a warning
      http.expectOne(URL).flush(
        { TypeInfo: "WebApiException", errorText: "SATELLITE_KEY_TOO_SHORT", severity: ExceptionSeverity.Warning },
        { status: 400, statusText: "Bad Request" });
      await done;

      expect(translate.translate).toHaveBeenCalledWith("ERROR.SATELLITE_KEY_TOO_SHORT");
      expect(notify.notifyWarning).toHaveBeenCalledOnceWith("The satellite key needs at least 16 characters.", 5000);
      expect(notify.notifySuccess).not.toHaveBeenCalled();
      expect(notify.notifyError).not.toHaveBeenCalled();
      expect(component.isDirty).toBeTrue();
      expect(TestBed.inject(AppService).isLoading).toBeFalse();
    });

    it("save_LowerCaseTypeInfo_IsAlsoAWebApiException", async () => {
      const done = save();
      http.expectOne(URL).flush(
        { typeInfo: "WebApiException", errorText: "SATELLITE_KEY_TOO_SHORT", severity: ExceptionSeverity.Warning },
        { status: 400, statusText: "Bad Request" });
      await done;

      expect(notify.notifyWarning).toHaveBeenCalledOnceWith("The satellite key needs at least 16 characters.", 5000);
    });

    it("save_Unauthorised_ShowsTheUnauthorisedError", async () => {
      const done = save();
      http.expectOne(URL).flush(null, { status: 401, statusText: "Unauthorized" });
      await done;

      expect(notify.notifyError).toHaveBeenCalledOnceWith("Unauthorised");
      expect(notify.notifySuccess).not.toHaveBeenCalled();
    });

    it("save_ErrorWithoutAnExceptionBody_ShowsTheHttpMessage", async () => {
      const done = save();
      http.expectOne(URL).flush("proxy error", { status: 502, statusText: "Bad Gateway" });
      await done;

      expect(notify.notifyError).toHaveBeenCalledTimes(1);
      expect(notify.notifyError.calls.mostRecent().args[0]).toContain("502");
      expect(notify.notifySuccess).not.toHaveBeenCalled();
    });

    it("save_InfoSeverity_IsAnInfo", async () => {
      const done = save();
      http.expectOne(URL).flush({ TypeInfo: "WebApiException", errorText: "NOTHING_CHANGED", severity: ExceptionSeverity.Info },
        { status: 400, statusText: "Bad Request" });
      await done;

      expect(notify.notifyInfo).toHaveBeenCalledOnceWith("ERROR.NOTHING_CHANGED");
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("handleError (base component)", () => {
    beforeEach(async () => {
      await init();
    });

    function failSaveWith(error: any) {
      spyOn(TestBed.inject(SatelliteService), "save").and.callFake(() => rejected(error));
      return component.save();
    }

    it("handleError_DeadSeverity_IsAnError", async () => {
      const ex = new WebApiException();
      ex.ErrorText = "SATELLITE_DEAD";
      ex.Severity = ExceptionSeverity.Dead;

      await failSaveWith(ex);

      expect(notify.notifyError).toHaveBeenCalledOnceWith("ERROR.SATELLITE_DEAD");
    });

    it("handleError_HttpErrorResponse_ShowsItsMessage", async () => {
      const error = new HttpErrorResponse({ status: 0, statusText: "Unknown Error", url: URL });

      await failSaveWith(error);

      expect(notify.notifyError).toHaveBeenCalledOnceWith(error.message);
    });

    it("handleError_AnythingElse_IsShownAsIs", async () => {
      await failSaveWith("offline");

      expect(notify.notifyError).toHaveBeenCalledOnceWith("offline");
      expect(TestBed.inject(AppService).isLoading).toBeFalse();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("create and edit", () => {
    beforeEach(async () => {
      await init();
    });

    it("openCreate_ResetsTheForm", () => {
      component.newName = "old";
      component.createError = "old error";

      component.openCreate();

      expect(component.createVisible).toBeTrue();
      expect(component.newName).toBe("");
      expect(component.createError).toBeUndefined();
    });

    it("create_EmptyOrBlankName_ShowsNameRequired", () => {
      component.openCreate();

      component.create();
      expect(component.createError).toBe("UI.SATELLITES.NAME_REQUIRED");

      component.newName = "   ";
      component.create();
      expect(component.createError).toBe("UI.SATELLITES.NAME_REQUIRED");
      expect(component.satellites.length).toBe(2);
      expect(component.createVisible).toBeTrue();
      expect(component.isDirty).toBeFalse();
    });

    it("create_Name_AddsASelectedUnsavedSatellite", () => {
      component.openCreate();
      component.newName = "  Cellar  ";

      component.create();
      fixture.detectChanges();

      expect(component.satellites.length).toBe(3);
      const created = component.selectedSatellite;
      expect(created.Name).toBe("Cellar");
      expect(created.ObjId).toBeTruthy();
      // name, description, client id and client key in the inspector
      expect(created.Properties.length).toBe(4);
      expect(component.isDirty).toBeTrue();
      expect(component.createVisible).toBeFalse();
      expect(text(".editor-pane h2")).toBe("Cellar");
      expect(fixture.nativeElement.querySelector(".pill.warn")).not.toBeNull();
      expect(rows().map(a => a.querySelector("b").textContent.trim())).toEqual(["Attic", "Cellar", "Garage"]);
      // nothing is posted before "Save"
      http.expectNone(URL);
    });

    it("select_ClickOnARow_OpensTheEditor", () => {
      rows()[1].click();
      fixture.detectChanges();

      expect(component.selectedSatellite.Name).toBe("Garage");
      expect(rows()[1].classList).toContain("selected");
      expect(rows()[1].getAttribute("aria-current")).toBe("true");
      expect(text(".editor-pane h2")).toBe("Garage");
      expect(fixture.nativeElement.querySelector(".split.has-selection")).not.toBeNull();
    });

    it("closeEditor_ShowsTheSelectHint", () => {
      component.select(component.satellites[0]);
      component.closeEditor();
      fixture.detectChanges();

      expect(component.selectedSatellite).toBeUndefined();
      expect(text(".editor-pane .empty-state b")).toBe("UI.SATELLITES.SELECT");
    });

    it("onChanged_MarksTheListUnsaved", () => {
      expect(fixture.nativeElement.querySelector(".pill.warn")).toBeNull();

      component.onChanged({});
      fixture.detectChanges();

      expect(component.isDirty).toBeTrue();
      expect(fixture.nativeElement.querySelector(".pill.warn")).not.toBeNull();
    });

    it("deleteSelected_NothingSelected_DoesNothing", async () => {
      await component.deleteSelected();

      expect(component.satellites.length).toBe(2);
      expect(component.isDirty).toBeFalse();
    });
  });
});
