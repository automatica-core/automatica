import { Injectable } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { HttpClient, HttpErrorResponse, HttpInterceptorFn, provideHttpClient, withInterceptors } from "@angular/common/http";
import { catchError, throwError } from "rxjs";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";

import { BaseService } from "./base-service";
import { BaseServiceHelper } from "./base-server-helper";
import { WebApiException, ExceptionSeverity } from "../base/model/web-api-exception";
import { Satellite } from "../base/model/satellites/satellite";
import { createTranslateSpy, isolateLocalStorage } from "../testing/test-helpers";

@Injectable()
class TestApiService extends BaseService {
  constructor(http: HttpClient, router: Router, translate: L10nTranslationService) {
    super(http, router, translate);
  }
}

function satelliteJson(name: string) {
  return { TypeInfo: "Satellite", ObjId: "sat-" + name, Name: name, Description: "", ClientId: "", ClientKey: "", Connected: false };
}

function exceptionJson(errorText: string, severity: ExceptionSeverity, typeInfoKey = "TypeInfo") {
  return { [typeInfoKey]: "WebApiException", errorText, severity };
}

function arrayBufferOf(text: string): ArrayBuffer {
  const bytes = new Uint8Array(text.length);
  for (let i = 0; i < text.length; i++) {
    bytes[i] = text.charCodeAt(i);
  }
  return bytes.buffer;
}

/**
 * Error bodies of arraybuffer requests (file downloads) arrive as ArrayBuffer. The testing backend cannot answer a json request
 * with one, so this interceptor turns a body "ARRAYBUFFER:<text>" into the ArrayBuffer of <text>.
 */
const arrayBufferErrors: HttpInterceptorFn = (req, next) => next(req).pipe(catchError(error => {
  if (error instanceof HttpErrorResponse && typeof error.error === "string" && error.error.startsWith("ARRAYBUFFER:")) {
    return throwError(() => new HttpErrorResponse({
      error: arrayBufferOf(error.error.substring("ARRAYBUFFER:".length)), status: error.status, statusText: error.statusText, url: error.url
    }));
  }
  return throwError(() => error);
}));

describe("BaseService", () => {
  let service: TestApiService;
  let http: HttpTestingController;
  let restore: () => void;

  beforeEach(() => {
    restore = isolateLocalStorage("s1server");
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([arrayBufferErrors])),
        provideHttpClientTesting(),
        TestApiService,
        { provide: Router, useValue: {} },
        { provide: L10nTranslationService, useValue: createTranslateSpy() }
      ]
    });
    service = TestBed.inject(TestApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    restore();
  });

  /** the error the promise rejected with */
  async function failure(promise: Promise<any>): Promise<any> {
    try {
      await promise;
    } catch (e) {
      return e;
    }
    fail("expected the request to fail");
    return void 0;
  }

  // ---------------------------------------------------------------------------------------------
  describe("urls and verbs", () => {
    it("getApiBaseUrl_NoStoredServer_IsRelative", () => {
      expect(BaseServiceHelper.getApiBaseUrl()).toBe("/webapi");
      expect(BaseServiceHelper.getBaseUrl()).toBe("");
    });

    it("getApiBaseUrl_StoredServer_IsUsed", () => {
      localStorage.setItem("s1server", "http://127.0.0.1:5001");

      expect(BaseServiceHelper.getApiBaseUrl()).toBe("http://127.0.0.1:5001/webapi");
      expect(BaseServiceHelper.getSignalRBaseUrl()).toBe("http://127.0.0.1:5001");
    });

    it("requests_StoredServer_GoToThatServer", async () => {
      localStorage.setItem("s1server", "http://127.0.0.1:5001");

      const done = service.getJson("version");
      http.expectOne("http://127.0.0.1:5001/webapi/version").flush({ version: "1.0" });

      expect(await done).toEqual({ version: "1.0" });
    });

    const verbs: [string, (s: TestApiService) => Promise<any>, string, boolean][] = [
      ["get", s => s.get("a"), "GET", true],
      ["getMultiple", s => s.getMultiple("a"), "GET", true],
      ["getJson", s => s.getJson("a"), "GET", true],
      ["post", s => s.post("a", { x: 1 }), "POST", true],
      ["post without credentials", s => s.post("a", { x: 1 }, false), "POST", false],
      ["postRaw", s => s.postRaw("a", { x: 1 }), "POST", true],
      ["postMultiple", s => s.postMultiple("a", { x: 1 }), "POST", true],
      ["postJson", s => s.postJson("a", { x: 1 }), "POST", true],
      ["put", s => s.put("a", { x: 1 }), "PUT", true],
      ["putJson", s => s.putJson("a", { x: 1 }), "PUT", true],
      ["patch", s => s.patch("a", { x: 1 }), "PATCH", true],
      ["patchJson", s => s.patchJson("a", { x: 1 }), "PATCH", true],
      ["deleteJson", s => s.deleteJson("a"), "DELETE", true]
    ];

    for (const [name, call, method, credentials] of verbs) {
      it(`${name}_SendsAJsonRequestToTheWebApi`, async () => {
        const done = call(service);
        const req = http.expectOne("/webapi/a");

        expect(req.request.method).toBe(method);
        expect(req.request.withCredentials).toBe(credentials);
        expect(req.request.headers.get("Content-Type")).toBe("application/json");
        expect(req.request.headers.get("Accept")).toBe("application/json");
        if (method !== "GET" && method !== "DELETE") {
          expect(req.request.body).toEqual({ x: 1 });
        }
        req.flush(null);
        await done;
      });
    }

    it("getRaw_ReturnsTheText", async () => {
      const done = service.getRaw("logs/file");
      const req = http.expectOne("/webapi/logs/file");
      expect(req.request.responseType).toBe("text");
      req.flush("line 1\nline 2");

      expect(await done).toBe("line 1\nline 2");
    });

    it("getRaw_EmptyText_IsUndefined", async () => {
      const done = service.getRaw("logs/file");
      http.expectOne("/webapi/logs/file").flush("");

      expect(await done).toBeUndefined();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("models", () => {
    it("get_ModelJson_IsMappedToItsClass", async () => {
      const done = service.get<Satellite>("satellite/1");
      http.expectOne("/webapi/satellite/1").flush(satelliteJson("Garage"));

      const result = await done;
      expect(result instanceof Satellite).toBeTrue();
      expect(result.Name).toBe("Garage");
    });

    it("getMultiple_Array_IsMappedToModels", async () => {
      const done = service.getMultiple<Satellite>("satellite");
      http.expectOne("/webapi/satellite").flush([satelliteJson("A"), satelliteJson("B")]);

      const result = await done;
      expect(result.map(a => a.Name)).toEqual(["A", "B"]);
      expect(result.every(a => a instanceof Satellite)).toBeTrue();
    });

    it("getMultiple_EntriesWithoutTypeInfo_AreDropped", async () => {
      spyOn(console, "error");
      const done = service.getMultiple<Satellite>("satellite");
      http.expectOne("/webapi/satellite").flush([satelliteJson("A"), { Name: "no type" }]);

      expect((await done).map(a => a.Name)).toEqual(["A"]);
    });

    it("getMultiple_NotAnArray_Throws", async () => {
      spyOn(console, "error");
      const done = service.getMultiple<Satellite>("satellite");
      http.expectOne("/webapi/satellite").flush(satelliteJson("A"));

      const error = await failure(done);
      expect(error.message).toBe("array expected at response");
    });

    it("emptyResponses_AreUndefined", async () => {
      const a = service.get("a");
      const b = service.getMultiple("b");
      const c = service.getJson("c");
      http.expectOne("/webapi/a").flush(null);
      http.expectOne("/webapi/b").flush(null);
      http.expectOne("/webapi/c").flush(null);

      expect(await a).toBeUndefined();
      expect(await b).toBeUndefined();
      expect(await c).toBeUndefined();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("errors", () => {
    it("status401_IsAnUnauthorisedWebApiException", async () => {
      const done = service.getJson("a");
      http.expectOne("/webapi/a").flush(exceptionJson("SOMETHING_ELSE", ExceptionSeverity.Info), { status: 401, statusText: "Unauthorized" });

      const error = await failure(done);
      expect(error instanceof WebApiException).toBeTrue();
      expect(error.ErrorText).toBe("UNAUTHORISED");
      expect(error.Severity).toBe(ExceptionSeverity.Error);
      expect(error.status).toBe(401);
    });

    const statuses = [400, 403, 404, 409, 422, 429, 500, 502, 503];
    for (const status of statuses) {
      it(`status${status}_WithAnExceptionBody_IsAWebApiExceptionWithTheStatus`, async () => {
        const done = service.postJson("a", {});
        http.expectOne("/webapi/a").flush(exceptionJson("SOME_ERROR", ExceptionSeverity.Warning), { status, statusText: "Error" });

        const error = await failure(done);
        expect(error instanceof WebApiException).toBeTrue();
        expect(error.ErrorText).toBe("SOME_ERROR");
        expect(error.Severity).toBe(ExceptionSeverity.Warning);
        expect(error.status).toBe(status);
      });

      it(`status${status}_WithoutAnExceptionBody_IsTheHttpError`, async () => {
        const done = service.postJson("a", {});
        http.expectOne("/webapi/a").flush({ message: "plain" }, { status, statusText: "Error" });

        const error = await failure(done);
        expect(error instanceof HttpErrorResponse).toBeTrue();
        expect(error.status).toBe(status);
      });
    }

    it("lowerCaseTypeInfo_IsAlsoAWebApiException", async () => {
      const done = service.getJson("a");
      http.expectOne("/webapi/a").flush(exceptionJson("LOWER", ExceptionSeverity.Dead, "typeInfo"), { status: 500, statusText: "Error" });

      const error = await failure(done);
      expect(error instanceof WebApiException).toBeTrue();
      expect(error.ErrorText).toBe("LOWER");
      expect(error.Severity).toBe(ExceptionSeverity.Dead);
    });

    it("arrayBufferBody_IsDecodedBeforeItIsParsed", async () => {
      const done = service.getJson("a");
      http.expectOne("/webapi/a").flush("ARRAYBUFFER:" + JSON.stringify(exceptionJson("FROM_BUFFER", ExceptionSeverity.Error)),
        { status: 400, statusText: "Bad Request" });

      const error = await failure(done);
      expect(error instanceof WebApiException).toBeTrue();
      expect(error.ErrorText).toBe("FROM_BUFFER");
      expect(error.status).toBe(400);
    });

    it("emptyArrayBufferBody_IsTheHttpError", async () => {
      const done = service.getJson("a");
      http.expectOne("/webapi/a").flush("ARRAYBUFFER:", { status: 500, statusText: "Error" });

      const error = await failure(done);
      expect(error instanceof HttpErrorResponse).toBeTrue();
      expect(error.status).toBe(500);
    });

    it("networkError_IsTheHttpErrorWithStatus0", async () => {
      const done = service.getJson("a");
      http.expectOne("/webapi/a").error(new ProgressEvent("error"));

      const error = await failure(done);
      expect(error instanceof HttpErrorResponse).toBeTrue();
      expect(error.status).toBe(0);
    });

    it("getRaw_Error_GoesThroughTheSameMapping", async () => {
      const done = service.getRaw("a");
      http.expectOne("/webapi/a").flush(JSON.stringify(exceptionJson("X", ExceptionSeverity.Error)), { status: 401, statusText: "Unauthorized" });

      const error = await failure(done);
      expect(error instanceof WebApiException).toBeTrue();
      expect(error.ErrorText).toBe("UNAUTHORISED");
    });
  });
});
