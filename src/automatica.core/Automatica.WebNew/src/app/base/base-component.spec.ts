import { EventEmitter } from "@angular/core";
import { fakeAsync, tick, discardPeriodicTasks } from "@angular/core/testing";
import { HttpErrorResponse } from "@angular/common/http";
import { L10nTranslationService } from "angular-l10n";
import { Subject } from "rxjs";

import { BaseComponent } from "./base-component";
import { WebApiException, ExceptionSeverity } from "./model/web-api-exception";
import { AppService } from "../services/app.service";
import { NotifyService } from "../services/notify.service";
import { createNotifySpy, createTranslateSpy, flushPromises } from "../testing/test-helpers";

/** opens the protected members of the base class */
class TestComponent extends BaseComponent {
  loads = 0;
  override load() {
    this.loads++;
    return Promise.resolve();
  }
  init() { this.baseOnInit(); }
  destroy() { this.baseOnDestroy(); }
  error(e: any) { this.handleError(e); }
  parse(e: any) { return this.parseErrorString(e); }
  register(e: EventEmitter<any>, cb: (a: any) => void) { return this.registerEvent(e, cb); }
  unregister(s: any) { this.unregisterEvent(s); }
  observe(o: Subject<any>, cb: (a: any) => void) { return this.registerObservable(o, cb); }
  every(cb: () => void, ms: number) { this.registerInterval(cb, ms); }
}

function exception(text: string, severity: ExceptionSeverity) {
  const ex = new WebApiException();
  ex.ErrorText = text;
  ex.Severity = severity;
  return ex;
}

describe("BaseComponent", () => {
  let notify: jasmine.SpyObj<NotifyService>;
  let translate: jasmine.SpyObj<L10nTranslationService>;
  let app: AppService;
  let component: TestComponent;

  beforeEach(() => {
    notify = createNotifySpy();
    translate = createTranslateSpy({ "ERROR.SATELLITE_KEY_TOO_SHORT": "Key too short" });
    app = new AppService();
    component = new TestComponent(notify, translate, app);
    spyOn(console, "log");
  });

  // ---------------------------------------------------------------------------------------------
  describe("handleError", () => {
    it("handleError_WarningException_IsATranslatedWarningFor5s", () => {
      component.error(exception("SATELLITE_KEY_TOO_SHORT", ExceptionSeverity.Warning));

      expect(translate.translate).toHaveBeenCalledWith("ERROR.SATELLITE_KEY_TOO_SHORT");
      expect(notify.notifyWarning).toHaveBeenCalledOnceWith("Key too short", 5000);
      expect(notify.notifyError).not.toHaveBeenCalled();
    });

    it("handleError_ErrorException_IsAnError", () => {
      component.error(exception("X", ExceptionSeverity.Error));

      expect(notify.notifyError).toHaveBeenCalledOnceWith("ERROR.X");
    });

    it("handleError_DeadException_IsAnError", () => {
      component.error(exception("X", ExceptionSeverity.Dead));

      expect(notify.notifyError).toHaveBeenCalledOnceWith("ERROR.X");
    });

    it("handleError_InfoException_IsAnInfo", () => {
      component.error(exception("X", ExceptionSeverity.Info));

      expect(notify.notifyInfo).toHaveBeenCalledOnceWith("ERROR.X");
      expect(notify.notifyError).not.toHaveBeenCalled();
    });

    it("handleError_HttpErrorResponse_ShowsItsMessage", () => {
      const error = new HttpErrorResponse({ status: 500, statusText: "Server Error", url: "/webapi/x" });

      component.error(error);

      expect(notify.notifyError).toHaveBeenCalledOnceWith(error.message);
      expect(error.message).toContain("500");
    });

    it("handleError_AnythingElse_IsPassedOn", () => {
      const error = new Error("boom");

      component.error(error);
      component.error("text");

      expect(notify.notifyError.calls.allArgs()).toEqual(<any>[[error], ["text"]]);
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("parseErrorString", () => {
    it("parseErrorString_WebApiException_IsTheTranslatedKey", () => {
      expect(component.parse(exception("SATELLITE_KEY_TOO_SHORT", ExceptionSeverity.Warning))).toBe("Key too short");
    });

    it("parseErrorString_HttpErrorResponse_IsItsMessage", () => {
      const error = new HttpErrorResponse({ status: 404, statusText: "Not Found", url: "/webapi/x" });

      expect(component.parse(error)).toBe(error.message);
    });

    it("parseErrorString_AnythingElse_IsReturnedAsIs", () => {
      const error = { custom: true };

      expect(component.parse(error)).toBe(error);
      expect(component.parse("text")).toBe("text");
      expect(component.parse(undefined)).toBeUndefined();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("loading", () => {
    it("isLoading_HoldsOneAppLoadingReference", () => {
      component.isLoading = true;
      component.isLoading = true;
      expect(app.isLoading).toBeTrue();

      component.isLoading = false;
      expect(app.isLoading).toBeFalse();
      expect(component.isLoading).toBeFalse();
    });

    it("isLoading_TwoComponents_DoNotHideEachOther", () => {
      const other = new TestComponent(notify, translate, app);

      component.isLoading = true;
      other.isLoading = true;
      component.isLoading = false;

      expect(app.isLoading).toBeTrue();
      other.isLoading = false;
      expect(app.isLoading).toBeFalse();
    });

    it("destroy_WhileLoading_ReleasesTheSpinner", () => {
      component.isLoading = true;

      component.destroy();

      expect(app.isLoading).toBeFalse();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("events", () => {
    it("serverStartedAgain_ReloadsThePage", async () => {
      component.init();

      app.isStarting = true;
      app.isStarting = false;
      await flushPromises();

      expect(component.loads).toBe(1);
    });

    it("serverStartedAgain_WhileLoading_DoesNotReload", async () => {
      component.init();
      component.isLoading = true;

      app.isStarting = true;
      app.isStarting = false;
      await flushPromises();

      expect(component.loads).toBe(0);
    });

    it("serverNotStartingAtAll_DoesNotReload", async () => {
      component.init();

      app.isStarting = false;
      await flushPromises();

      expect(component.loads).toBe(0);
    });

    it("registerEvent_UntilDestroyed", () => {
      const event = new EventEmitter<number>();
      const seen: number[] = [];
      component.register(event, a => seen.push(a));

      event.emit(1);
      component.destroy();
      event.emit(2);

      expect(seen).toEqual([1]);
    });

    it("registerEvent_AfterDestroy_IsNotKept", () => {
      const event = new EventEmitter<number>();
      const seen: number[] = [];
      component.destroy();

      const sub = component.register(event, a => seen.push(a));
      event.emit(1);

      expect(sub.closed).toBeTrue();
      expect(seen).toEqual([]);
    });

    it("unregisterEvent_StopsOnlyThatSubscription", () => {
      const event = new EventEmitter<number>();
      const a: number[] = [];
      const b: number[] = [];
      const subA = component.register(event, x => a.push(x));
      component.register(event, x => b.push(x));

      component.unregister(subA);
      event.emit(1);

      expect(a).toEqual([]);
      expect(b).toEqual([1]);
    });

    it("registerObservable_UntilDestroyed", () => {
      const subject = new Subject<number>();
      const seen: number[] = [];
      component.observe(subject, a => seen.push(a));

      subject.next(1);
      component.destroy();
      subject.next(2);

      expect(seen).toEqual([1]);
      expect(subject.observed).toBeFalse();
    });

    it("registerInterval_ClearedOnDestroy", fakeAsync(() => {
      let ticks = 0;
      component.every(() => ticks++, 100);

      tick(250);
      expect(ticks).toBe(2);

      component.destroy();
      tick(500);
      expect(ticks).toBe(2);
      discardPeriodicTasks();
    }));

    it("registerInterval_AfterDestroy_IsNotStarted", fakeAsync(() => {
      let ticks = 0;
      component.destroy();

      component.every(() => ticks++, 100);
      tick(500);

      expect(ticks).toBe(0);
    }));
  });
});
