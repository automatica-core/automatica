import { TestBed, fakeAsync, flush, tick } from "@angular/core/testing";
import { HttpClient, HttpContext, HttpErrorResponse, provideHttpClient, withInterceptors } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { Router } from "@angular/router";

import { REQUEST_TIMEOUT, SKIP_LOGIN_REDIRECT, apiInterceptor, isApiRequest } from "./api.interceptor";
import { LoginService } from "./login.service";
import { HubConnectionService } from "../base/communication/hubs/hub-connection.service";
import { flushPromises, isolateLocalStorage } from "../testing/test-helpers";

describe("apiInterceptor", () => {
  let client: HttpClient;
  let http: HttpTestingController;
  let router: { url: string, navigate: jasmine.Spy };
  let login: { logout: jasmine.Spy };
  let hubs: { stop: jasmine.Spy };
  let restore: () => void;

  beforeEach(() => {
    restore = isolateLocalStorage("s1server", "jwt");
    router = { url: "/admin/home", navigate: jasmine.createSpy("navigate").and.resolveTo(true) };
    login = { logout: jasmine.createSpy("logout") };
    hubs = { stop: jasmine.createSpy("stop").and.resolveTo(undefined) };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([apiInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
        { provide: LoginService, useValue: login },
        { provide: HubConnectionService, useValue: hubs }
      ]
    });
    client = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    restore();
  });

  function result(observable: any) {
    const r: { value?: any, error?: any } = {};
    observable.subscribe({ next: (v: any) => r.value = v, error: (e: any) => r.error = e });
    return r;
  }

  // ---------------------------------------------------------------------------------------------
  describe("isApiRequest", () => {
    it("isApiRequest_RelativeWebApiPaths_AreApiRequests", () => {
      expect(isApiRequest("/webapi/satellite")).toBeTrue();
      expect(isApiRequest("webapi/satellite")).toBeTrue();
      expect(isApiRequest("./webapi/satellite")).toBeTrue();
      expect(isApiRequest("/webapi")).toBeTrue();
      expect(isApiRequest("/webapi?x=1")).toBeTrue();
      expect(isApiRequest("/WEBAPI/version")).toBeTrue();
    });

    it("isApiRequest_OtherPaths_AreNot", () => {
      expect(isApiRequest("/assets/locale/locale-en.json")).toBeFalse();
      expect(isApiRequest("/webapifoo/x")).toBeFalse();
      expect(isApiRequest("/other/webapi/x")).toBeFalse();
      expect(isApiRequest("https://example.invalid/webapi/x")).toBeFalse();
    });

    it("isApiRequest_StoredServer_OnlyThatServersWebApi", () => {
      localStorage.setItem("s1server", "http://127.0.0.1:5001");

      expect(isApiRequest("http://127.0.0.1:5001/webapi/version")).toBeTrue();
      expect(isApiRequest("http://127.0.0.1:5001/assets/x.json")).toBeFalse();
      expect(isApiRequest("http://127.0.0.2:5001/webapi/version")).toBeFalse();
      expect(isApiRequest("/webapi/version")).toBeFalse();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("auth header", () => {
    it("token_ApiRequest_GetsTheBearerHeader", () => {
      localStorage.setItem("jwt", "made-up-test-token");

      client.get("/webapi/version").subscribe();
      const req = http.expectOne("/webapi/version");

      expect(req.request.headers.get("Authorization")).toBe("Bearer made-up-test-token");
      req.flush({});
    });

    it("token_OtherRequest_HasNoHeader", () => {
      localStorage.setItem("jwt", "made-up-test-token");

      client.get("/assets/locale/locale-en.json").subscribe();
      const req = http.expectOne("/assets/locale/locale-en.json");

      expect(req.request.headers.has("Authorization")).toBeFalse();
      req.flush({});
    });

    it("token_HeaderAlreadySet_IsKept", () => {
      localStorage.setItem("jwt", "made-up-test-token");

      client.get("/webapi/version", { headers: { Authorization: "Bearer other-test-token" } }).subscribe();
      const req = http.expectOne("/webapi/version");

      expect(req.request.headers.get("Authorization")).toBe("Bearer other-test-token");
      req.flush({});
    });

    it("noToken_NoHeader", () => {
      client.get("/webapi/version").subscribe();
      const req = http.expectOne("/webapi/version");

      expect(req.request.headers.has("Authorization")).toBeFalse();
      req.flush({});
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("401", () => {
    it("status401_EndsTheSession", async () => {
      const r = result(client.get("/webapi/satellite"));
      http.expectOne("/webapi/satellite").flush(null, { status: 401, statusText: "Unauthorized" });
      await flushPromises();

      expect(r.error instanceof HttpErrorResponse).toBeTrue();
      expect(r.error.status).toBe(401);
      expect(login.logout).toHaveBeenCalledTimes(1);
      expect(hubs.stop).toHaveBeenCalledTimes(1);
      expect(router.navigate).toHaveBeenCalledOnceWith(["/login"]);
    });

    it("status401_SeveralAtOnce_EndTheSessionOnce", async () => {
      result(client.get("/webapi/a"));
      result(client.get("/webapi/b"));
      http.expectOne("/webapi/a").flush(null, { status: 401, statusText: "Unauthorized" });
      http.expectOne("/webapi/b").flush(null, { status: 401, statusText: "Unauthorized" });
      await flushPromises();

      expect(login.logout).toHaveBeenCalledTimes(1);
      expect(router.navigate).toHaveBeenCalledTimes(1);
    });

    it("status401_AlreadyOnTheLogin_DoesNotNavigate", async () => {
      router.url = "/login";

      result(client.get("/webapi/a"));
      http.expectOne("/webapi/a").flush(null, { status: 401, statusText: "Unauthorized" });
      await flushPromises();

      expect(login.logout).toHaveBeenCalled();
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it("status401_HubStopFails_StillGoesToTheLogin", async () => {
      spyOn(console, "error");
      hubs.stop.and.rejectWith(new Error("already stopped"));

      result(client.get("/webapi/a"));
      http.expectOne("/webapi/a").flush(null, { status: 401, statusText: "Unauthorized" });
      await flushPromises();

      expect(router.navigate).toHaveBeenCalledOnceWith(["/login"]);
    });

    it("status401_OfTheLogin_IsOnlyWrongCredentials", async () => {
      const r = result(client.post("/webapi/auth/login", {}));
      http.expectOne("/webapi/auth/login").flush(null, { status: 401, statusText: "Unauthorized" });
      await flushPromises();

      expect(r.error.status).toBe(401);
      expect(login.logout).not.toHaveBeenCalled();
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it("status401_SkipLoginRedirect_KeepsTheSession", async () => {
      const r = result(client.post("/webapi/account/password", {}, { context: new HttpContext().set(SKIP_LOGIN_REDIRECT, true) }));
      http.expectOne("/webapi/account/password").flush(null, { status: 401, statusText: "Unauthorized" });
      await flushPromises();

      expect(r.error.status).toBe(401);
      expect(login.logout).not.toHaveBeenCalled();
    });

    it("status401_OtherHost_IsNotTouched", async () => {
      const r = result(client.get("/assets/x.json"));
      http.expectOne("/assets/x.json").flush(null, { status: 401, statusText: "Unauthorized" });
      await flushPromises();

      expect(r.error.status).toBe(401);
      expect(login.logout).not.toHaveBeenCalled();
    });

    it("status403_KeepsTheSession", async () => {
      const r = result(client.get("/webapi/a"));
      http.expectOne("/webapi/a").flush(null, { status: 403, statusText: "Forbidden" });
      await flushPromises();

      expect(r.error.status).toBe(403);
      expect(login.logout).not.toHaveBeenCalled();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("timeout", () => {
    it("get_NoAnswerFor60s_FailsWithStatus0", fakeAsync(() => {
      const r = result(client.get("/webapi/slow"));
      const req = http.expectOne("/webapi/slow");

      tick(59999);
      expect(r.error).toBeUndefined();
      tick(1);

      expect(r.error instanceof HttpErrorResponse).toBeTrue();
      expect(r.error.status).toBe(0);
      expect(r.error.statusText).toBe("Timeout");
      expect(r.error.url).toBe("/webapi/slow");
      expect(req.cancelled).toBeTrue();
      flush();
    }));

    it("post_HasNoDefaultTimeout", fakeAsync(() => {
      const r = result(client.post("/webapi/scan", {}));
      const req = http.expectOne("/webapi/scan");

      tick(10 * 60000);
      expect(r.error).toBeUndefined();
      req.flush({ ok: true });

      expect(r.value).toEqual({ ok: true });
      flush();
    }));

    it("requestTimeout_Context_OverridesTheDefault", fakeAsync(() => {
      const r = result(client.post("/webapi/x", {}, { context: new HttpContext().set(REQUEST_TIMEOUT, 500) }));
      http.expectOne("/webapi/x");

      tick(500);

      expect(r.error.status).toBe(0);
      flush();
    }));

    it("requestTimeout_Zero_DisablesTheGetTimeout", fakeAsync(() => {
      const r = result(client.get("/webapi/x", { context: new HttpContext().set(REQUEST_TIMEOUT, 0) }));
      const req = http.expectOne("/webapi/x");

      tick(120000);
      expect(r.error).toBeUndefined();
      req.flush(1);
      expect(r.value).toBe(1);
      flush();
    }));

    it("timeout_OnlyForApiRequests", fakeAsync(() => {
      const r = result(client.get("/assets/big.json"));
      const req = http.expectOne("/assets/big.json");

      tick(120000);
      expect(r.error).toBeUndefined();
      req.flush({});
      flush();
    }));
  });
});
