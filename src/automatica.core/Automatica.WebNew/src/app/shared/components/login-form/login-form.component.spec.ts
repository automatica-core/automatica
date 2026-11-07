import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideHttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { Router } from "@angular/router";
import { L10N_LOCALE, L10nTranslationService } from "angular-l10n";

import { LoginFormComponent } from "./login-form.component";
import { LoginService } from "src/app/services/login.service";
import { AppService } from "src/app/services/app.service";
import { NotifyService } from "src/app/services/notify.service";
import { DeviceService } from "src/app/services/device/device.service";
import { ServerDiscoveryService } from "src/app/services/device/server-discovery.service";
import { ConfigService } from "src/app/services/config.service";
import { User } from "src/app/base/model/user/user";
import { FakeTranslateAsyncPipe, createNotifySpy, createTranslateSpy, flushPromises, isolateLocalStorage } from "src/app/testing/test-helpers";

const LOGIN_URL = "/webapi/auth/login";
const PASSWORD_URL = "/webapi/account/password";

function userJson(extra: any = {}) {
  return {
    TypeInfo: "User", ObjId: "user-1", UserName: "admin", Token: "made-up-session-token",
    InverseThis2Roles: [], InverseThis2UserGroups: [], ...extra
  };
}

describe("LoginFormComponent", () => {
  let fixture: ComponentFixture<LoginFormComponent>;
  let component: LoginFormComponent;
  let http: HttpTestingController;
  let router: { navigate: jasmine.Spy };
  let device: { isWeb: boolean };
  let discovery: { isAvailable: boolean, start: jasmine.Spy };
  let config: { getVersion: jasmine.Spy };
  let validate: jasmine.Spy;
  let loginService: LoginService;
  let restore: () => void;

  beforeEach(() => {
    restore = isolateLocalStorage("s1server", "s1user", "jwt", "automatica.user", "s1pw");
    router = { navigate: jasmine.createSpy("navigate").and.resolveTo(true) };
    device = { isWeb: true };
    discovery = { isAvailable: false, start: jasmine.createSpy("start") };
    config = { getVersion: jasmine.createSpy("getVersion").and.resolveTo({ version: "9.1.0" }) };
    validate = jasmine.createSpy("validate").and.returnValue({ isValid: true });

    TestBed.configureTestingModule({
      declarations: [LoginFormComponent],
      imports: [FakeTranslateAsyncPipe],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        LoginService,
        AppService,
        { provide: Router, useValue: router },
        { provide: NotifyService, useValue: createNotifySpy() },
        { provide: L10N_LOCALE, useValue: { language: "en" } },
        { provide: L10nTranslationService, useValue: createTranslateSpy({ "UI.LOGIN.USER_REQUIRED": "User required" }) },
        { provide: DeviceService, useValue: device },
        { provide: ServerDiscoveryService, useValue: discovery },
        { provide: ConfigService, useValue: config }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    });
    http = TestBed.inject(HttpTestingController);
    loginService = TestBed.inject(LoginService);
  });

  afterEach(() => {
    http.verify();
    restore();
  });

  async function create() {
    fixture = TestBed.createComponent(LoginFormComponent);
    component = fixture.componentInstance;
    // the DevExtreme validation group is not rendered here, its result is what the tests decide
    component.validationGroup = <any>{ instance: { validate } };
    fixture.detectChanges();
    await flushPromises();
    fixture.detectChanges();
  }

  /** the texts of the dx-buttons (an input of the widget, not DOM text here) */
  function buttons(): string[] {
    return Array.from(fixture.nativeElement.querySelectorAll("dx-button")).map((a: any) => a.text);
  }

  function text(selector: string): string {
    return fixture.nativeElement.querySelector(selector)?.textContent.trim();
  }

  async function login(user = "admin", password = "made-up-test-password") {
    component.login = user;
    component.password = password;
    const done = component.onLoginClick(void 0);
    await flushPromises();
    return done;
  }

  /** a login that answers with MustChangePassword */
  async function pendingChange() {
    const done = login("admin", "admin");
    http.expectOne(LOGIN_URL).flush(userJson({ MustChangePassword: true, Token: "pending-test-token" }));
    await done;
    fixture.detectChanges();
  }

  async function changePassword(newPassword = "new-test-password-1", confirm = newPassword) {
    component.newPassword = newPassword;
    component.newPasswordConfirm = confirm;
    const done = component.onChangePasswordClick(void 0);
    await flushPromises();
    return done;
  }

  // ---------------------------------------------------------------------------------------------
  describe("start", () => {
    it("constructor_EndsTheSessionAndPrefillsTheUserName", async () => {
      localStorage.setItem("jwt", "old-test-token");
      localStorage.setItem("s1pw", "legacy-test-password");
      localStorage.setItem("s1user", "remembered");

      await create();

      expect(localStorage.getItem("jwt")).toBeNull();
      expect(localStorage.getItem("s1pw")).toBeNull();
      expect(component.login).toBe("remembered");
    });

    it("ngOnInit_ResetsTheLoadingStateOfTheOldSession", async () => {
      const app = TestBed.inject(AppService);
      app.beginLoading();
      app.isStarting = true;

      await create();

      expect(app.isLoading).toBeFalse();
      expect(app.isStarting).toBeFalse();
    });

    it("web_ShowsNoServerFieldButTheVersion", async () => {
      await create();

      expect(text("h1.title")).toBe("UI.LOGIN.TITLE");
      expect(fixture.nativeElement.textContent).not.toContain("LOGIN.SERVER_IP");
      expect(buttons()).toEqual(["UI.LOGIN.SUBMIT"]);
      expect(text(".foot .mono")).toBe("9.1.0");
      expect(fixture.nativeElement.querySelector(".login-error")).toBeNull();
    });

    it("version_Unreachable_IsNotShown", async () => {
      config.getVersion.and.rejectWith(new Error("offline"));

      await create();

      expect(component.serverVersion).toBeUndefined();
      expect(fixture.nativeElement.querySelector(".foot .mono")).toBeNull();
    });

    it("app_NoServerYet_ShowsTheServerFieldAndSkipsTheVersion", async () => {
      device.isWeb = false;

      await create();

      expect(fixture.nativeElement.textContent).toContain("LOGIN.SERVER_IP");
      expect(buttons()).toEqual(["UI.LOGIN.SUBMIT", "LOGIN.DEMO"]);
      expect(config.getVersion).not.toHaveBeenCalled();
    });

    it("app_Discovery_TakesTheFirstServerFound", async () => {
      device.isWeb = false;
      discovery.isAvailable = true;
      const stop = jasmine.createSpy("stop").and.resolveTo(undefined);
      let report: (servers: any[]) => void;
      discovery.start.and.callFake((cb: any) => { report = cb; return Promise.resolve(stop); });

      await create();
      report([
        { name: "Home", host: "192.0.2.10", port: 5001, url: "http://192.0.2.10:5001" },
        { name: "Lab", host: "192.0.2.11", port: 5001, url: "http://192.0.2.11:5001" }
      ]);

      expect(component.serverIp).toBe("http://192.0.2.10:5001");
      const buttons = fixture.nativeElement.querySelectorAll(".discovered-server");
      expect(buttons.length).toBe(2);
      expect(buttons[0].classList).toContain("selected");

      buttons[1].click();
      expect(component.serverIp).toBe("http://192.0.2.11:5001");

      fixture.destroy();
      expect(stop).toHaveBeenCalled();
    });

    it("requiredMessage_IsTranslated", async () => {
      await create();

      expect(component.requiredMessage("UI.LOGIN.USER_REQUIRED")).toBe("User required");
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("login", () => {
    beforeEach(async () => {
      await create();
    });

    it("login_Success_StoresTheSessionAndGoesHome", async () => {
      const loggedIn: User[] = [];
      loginService.userLoggedIn.subscribe(a => loggedIn.push(a));

      const done = login();
      const req = http.expectOne(LOGIN_URL);
      expect(req.request.body).toEqual({ Username: "admin", Password: "made-up-test-password" });
      expect(component.isBusy).toBeTrue();
      req.flush(userJson());
      await done;

      expect(localStorage.getItem("jwt")).toBe("made-up-session-token");
      expect(loggedIn.length).toBe(1);
      expect(loggedIn[0].UserName).toBe("admin");
      expect(router.navigate).toHaveBeenCalledOnceWith(["/"]);
      expect(component.isBusy).toBeFalse();
      expect(component.errorKey).toBeUndefined();
      // the web version never remembers anything
      expect(localStorage.getItem("s1user")).toBeNull();
      expect(localStorage.getItem("s1server")).toBeNull();
      expect(TestBed.inject(AppService).isLoading).toBeFalse();
    });

    it("login_Invalid_SendsNothing", async () => {
      validate.and.returnValue({ isValid: false });

      await login("", "");

      http.expectNone(LOGIN_URL);
      expect(component.isBusy).toBeFalse();
      expect(TestBed.inject(AppService).isLoading).toBeFalse();
    });

    it("login_NoContent_IsWrongCredentialsWithHint", async () => {
      const done = login();
      http.expectOne(LOGIN_URL).flush(null, { status: 204, statusText: "No Content" });
      await done;
      fixture.detectChanges();

      expect(component.errorKey).toBe("UI.LOGIN.ERROR_CREDENTIALS");
      expect(text(".login-error b")).toBe("UI.LOGIN.ERROR_CREDENTIALS");
      expect(text(".login-error span")).toBe("UI.LOGIN.ERROR_CREDENTIALS_HINT");
      expect(router.navigate).not.toHaveBeenCalled();
    });

    const failures: [string, (req: any) => void, string, boolean][] = [
      ["401", r => r.flush(null, { status: 401, statusText: "Unauthorized" }), "UI.LOGIN.ERROR_CREDENTIALS", true],
      ["unreachable", r => r.error(new ProgressEvent("error")), "UI.LOGIN.ERROR_SERVER", true],
      ["429", r => r.flush(null, { status: 429, statusText: "Too Many Requests" }), "ERROR.TOO_MANY_LOGIN_ATTEMPTS", false],
      ["500", r => r.flush("boom", { status: 500, statusText: "Server Error" }), "UI.LOGIN.ERROR_FAILED", true]
    ];

    for (const [name, answer, key, hint] of failures) {
      it(`login_${name}_Shows${key.split(".").pop()}`, async () => {
        spyOn(console, "error");
        const done = login();
        answer(http.expectOne(LOGIN_URL));
        await done;
        fixture.detectChanges();

        expect(component.errorKey).toBe(key);
        expect(text(".login-error b")).toBe(key);
        if (hint) {
          expect(text(".login-error span")).toBe(key + "_HINT");
        } else {
          // the error hint is only shown when there is one
          expect(component.errorHint).toBeUndefined();
          expect(fixture.nativeElement.querySelector(".login-error span")).toBeNull();
        }
        expect(localStorage.getItem("jwt")).toBeNull();
        expect(router.navigate).not.toHaveBeenCalled();
        expect(component.isBusy).toBeFalse();
      });
    }

    it("clearError_RemovesTheMessage", async () => {
      const done = login();
      http.expectOne(LOGIN_URL).flush(null, { status: 401, statusText: "Unauthorized" });
      await done;

      component.clearError();
      fixture.detectChanges();

      expect(component.errorKey).toBeUndefined();
      expect(component.errorHint).toBeUndefined();
      expect(fixture.nativeElement.querySelector(".login-error")).toBeNull();
    });

    it("login_NextAttempt_ClearsTheOldError", async () => {
      let done = login();
      http.expectOne(LOGIN_URL).flush(null, { status: 401, statusText: "Unauthorized" });
      await done;

      done = login();
      expect(component.errorKey).toBeUndefined();
      http.expectOne(LOGIN_URL).flush(userJson());
      await done;
    });

    it("enter_NoPendingChange_Logs_In", async () => {
      const done = component.onEnterPressed(void 0);
      await flushPromises();
      http.expectOne(LOGIN_URL).flush(userJson());
      await done;

      expect(router.navigate).toHaveBeenCalledWith(["/"]);
    });

    it("formSubmit_LogsIn", async () => {
      component.login = "admin";
      component.password = "made-up-test-password";

      fixture.nativeElement.querySelector("form").dispatchEvent(new Event("submit", { cancelable: true }));
      await flushPromises();

      http.expectOne(LOGIN_URL).flush(userJson());
      await flushPromises();
      expect(router.navigate).toHaveBeenCalledWith(["/"]);
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("app login", () => {
    beforeEach(async () => {
      device.isWeb = false;
      await create();
    });

    it("login_StoresTheServerAndOnRequestTheUserName", async () => {
      component.serverIp = "http://127.0.0.1:5001";
      component.saveLogin = true;

      const done = login("operator");
      http.expectOne("http://127.0.0.1:5001/webapi/auth/login").flush(userJson());
      await done;

      expect(localStorage.getItem("s1server")).toBe("http://127.0.0.1:5001");
      expect(localStorage.getItem("s1user")).toBe("operator");
      // the password is never stored
      expect(localStorage.getItem("s1pw")).toBeNull();
    });

    it("login_WithoutSaveLogin_DoesNotRememberTheUser", async () => {
      component.serverIp = "http://127.0.0.1:5001";

      const done = login("operator");
      http.expectOne("http://127.0.0.1:5001/webapi/auth/login").flush(userJson());
      await done;

      expect(localStorage.getItem("s1user")).toBeNull();
    });

    it("demoLogin_UsesTheDemoServer", async () => {
      const done = component.onDemoLogin(void 0);
      await flushPromises();
      const req = http.expectOne("https://demo.automaticacore.com/webapi/auth/login");
      expect(req.request.body).toEqual({ Username: "sa", Password: "sa" });
      req.flush(userJson());
      await done;

      expect(localStorage.getItem("s1server")).toBe("https://demo.automaticacore.com");
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("must change password", () => {
    beforeEach(async () => {
      await create();
      await pendingChange();
    });

    it("login_MustChangePassword_ShowsTheChangeFormInsteadOfLoggingIn", () => {
      expect(component.mustChangePassword).toBeTrue();
      expect(text("h1.title")).toBe("LOGIN.CHANGE_PASSWORD");
      expect(text(".must-change-hint")).toBe("LOGIN.MUST_CHANGE_PASSWORD");
      expect(buttons()).toEqual(["LOGIN.CHANGE_PASSWORD", "LOGIN.CANCEL"]);
      expect(router.navigate).not.toHaveBeenCalled();
      // the pending token allows nothing but the change, it is not the session
      expect(localStorage.getItem("jwt")).toBeNull();
      expect(localStorage.getItem("automatica.user")).toBeNull();
    });

    it("passwordsMatch_ComparesBothFields", () => {
      component.newPassword = "new-test-password-1";
      component.newPasswordConfirm = "new-test-password-1";
      expect(component.passwordsMatch()).toBeTrue();

      component.newPasswordConfirm = "new-test-password-2";
      expect(component.passwordsMatch()).toBeFalse();
    });

    it("passwordsMatch_IsBoundToTheValidator", () => {
      // an arrow function, DevExtreme calls the callback without `this`
      const callback = component.passwordsMatch;
      component.newPassword = "a-test-password";
      component.newPasswordConfirm = "a-test-password";

      expect(callback()).toBeTrue();
    });

    it("changePassword_SendsThePendingTokenAndBothPasswords_ThenLogsIn", async () => {
      const done = changePassword();
      const req = http.expectOne(PASSWORD_URL);
      expect(localStorage.getItem("jwt")).toBe("pending-test-token");
      expect(req.request.body).toEqual({ OldPassword: "admin", NewPassword: "new-test-password-1" });
      req.flush(userJson({ Token: "new-session-token" }));
      await done;

      expect(component.mustChangePassword).toBeFalse();
      expect(localStorage.getItem("jwt")).toBe("new-session-token");
      expect(router.navigate).toHaveBeenCalledOnceWith(["/"]);
      expect(component.isBusy).toBeFalse();
      expect(TestBed.inject(AppService).isLoading).toBeFalse();
    });

    it("changePassword_Invalid_SendsNothing", async () => {
      validate.and.returnValue({ isValid: false });

      await changePassword("short", "other");

      http.expectNone(PASSWORD_URL);
      expect(component.mustChangePassword).toBeTrue();
    });

    it("enter_WhilePending_ChangesThePassword", async () => {
      spyOn(component, "onLoginClick").and.callThrough();
      component.newPassword = "new-test-password-1";
      component.newPasswordConfirm = "new-test-password-1";

      const done = component.onEnterPressed(void 0);
      await flushPromises();
      http.expectOne(PASSWORD_URL).flush(userJson());
      await done;

      expect(component.onLoginClick).not.toHaveBeenCalled();
      http.expectNone(LOGIN_URL);
    });

    it("changePassword_429_ShowsTooManyAttemptsWithoutHint", async () => {
      const done = changePassword();
      http.expectOne(PASSWORD_URL).flush(null, { status: 429, statusText: "Too Many Requests" });
      await done;
      fixture.detectChanges();

      expect(component.errorKey).toBe("ERROR.TOO_MANY_LOGIN_ATTEMPTS");
      expect(component.errorHint).toBeUndefined();
      expect(text(".login-error b")).toBe("ERROR.TOO_MANY_LOGIN_ATTEMPTS");
      expect(fixture.nativeElement.querySelector(".login-error span")).toBeNull();
      expect(component.mustChangePassword).toBeTrue();
    });

    for (const status of [401, 403]) {
      it(`changePassword_${status}_BackToTheLoginWithCredentialsError`, async () => {
        const done = changePassword();
        http.expectOne(PASSWORD_URL).flush(null, { status, statusText: "Denied" });
        await done;
        fixture.detectChanges();

        expect(component.mustChangePassword).toBeFalse();
        expect(component.password).toBe("");
        expect(component.newPassword).toBe("");
        expect(localStorage.getItem("jwt")).toBeNull();
        expect(component.errorKey).toBe("UI.LOGIN.ERROR_CREDENTIALS");
        expect(text(".login-error span")).toBe("UI.LOGIN.ERROR_CREDENTIALS_HINT");
        expect(text("h1.title")).toBe("UI.LOGIN.TITLE");
      });
    }

    it("changePassword_ServerKeyAsText_IsTranslatedAsError", async () => {
      const done = changePassword();
      http.expectOne(PASSWORD_URL).flush("PASSWORD_TOO_SHORT", { status: 400, statusText: "Bad Request" });
      await done;

      expect(component.errorKey).toBe("ERROR.PASSWORD_TOO_SHORT");
      expect(component.errorHint).toBeUndefined();
    });

    it("changePassword_ServerKeyInAnObject_IsTranslatedAsError", async () => {
      const done = changePassword();
      http.expectOne(PASSWORD_URL).flush({ error: "PASSWORD_SAME_AS_OLD" }, { status: 400, statusText: "Bad Request" });
      await done;

      expect(component.errorKey).toBe("ERROR.PASSWORD_SAME_AS_OLD");
    });

    it("changePassword_TextThatIsNoKey_IsTheGenericError", async () => {
      spyOn(console, "error");
      const done = changePassword();
      http.expectOne(PASSWORD_URL).flush("Something went wrong", { status: 400, statusText: "Bad Request" });
      await done;

      expect(component.errorKey).toBe("ERROR.PASSWORD_CHANGE_FAILED");
      expect(component.mustChangePassword).toBeTrue();
    });

    it("changePassword_Unreachable_IsTheGenericError", async () => {
      spyOn(console, "error");
      const done = changePassword();
      http.expectOne(PASSWORD_URL).error(new ProgressEvent("error"));
      await done;

      expect(component.errorKey).toBe("ERROR.PASSWORD_CHANGE_FAILED");
      expect(component.isBusy).toBeFalse();
    });

    it("cancel_DropsThePendingChange", () => {
      component.newPassword = "x";
      component.newPasswordConfirm = "y";
      component.errorKey = "ERROR.SOMETHING";

      component.cancelPasswordChange();

      expect(component.mustChangePassword).toBeFalse();
      expect(component.password).toBe("");
      expect(component.newPassword).toBe("");
      expect(component.newPasswordConfirm).toBe("");
      expect(component.errorKey).toBeUndefined();
      expect(text("h1.title")).toBe("UI.LOGIN.TITLE");
    });

    it("cancel_ThenLogin_StartsOver", async () => {
      component.cancelPasswordChange();

      const done = login();
      http.expectOne(LOGIN_URL).flush(userJson());
      await done;

      expect(router.navigate).toHaveBeenCalledOnceWith(["/"]);
      http.expectNone(PASSWORD_URL);
    });
  });
});
