import { TestBed } from "@angular/core/testing";
import { provideHttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";

import { HasRoleGuard, LoginService } from "./login.service";
import { User } from "../base/model/user/user";
import { Role } from "../base/model/user/role";
import { WebApiException } from "../base/model/web-api-exception";
import { createTranslateSpy, isolateLocalStorage } from "../testing/test-helpers";

const TOKEN = "made-up-test-token";

function userJson(roles: string[] = [], extra: any = {}) {
  return {
    TypeInfo: "User", ObjId: "user-1", UserName: "admin", FirstName: "Ada", LastName: "Admin", Token: TOKEN,
    InverseThis2Roles: roles.map(key => ({
      TypeInfo: "User2Role", This2User: "user-1", This2Role: "role-" + key,
      This2RoleNavigation: { TypeInfo: "Role", ObjId: "role-" + key, Key: key, Name: key, Description: "" }
    })),
    InverseThis2UserGroups: [],
    ...extra
  };
}

describe("LoginService", () => {
  let service: LoginService;
  let http: HttpTestingController;
  let router: { navigate: jasmine.Spy };
  let restore: () => void;

  beforeEach(() => {
    restore = isolateLocalStorage("s1server", "jwt", "automatica.user", "s1pw");
    router = { navigate: jasmine.createSpy("navigate") };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        LoginService,
        HasRoleGuard,
        { provide: Router, useValue: router },
        { provide: L10nTranslationService, useValue: createTranslateSpy() }
      ]
    });
    service = TestBed.inject(LoginService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    restore();
  });

  async function loggedIn(roles: string[]) {
    const done = service.login("admin", "made-up-test-password");
    http.expectOne("/webapi/auth/login").flush(userJson(roles));
    const user = await done;
    service.saveToLocalStorage(user);
    return user;
  }

  // ---------------------------------------------------------------------------------------------
  describe("login", () => {
    it("login_PostsTheCredentialsWithoutCookies", async () => {
      const done = service.login("admin", "made-up-test-password");
      const req = http.expectOne("/webapi/auth/login");

      expect(req.request.method).toBe("POST");
      expect(req.request.body).toEqual({ Username: "admin", Password: "made-up-test-password" });
      expect(req.request.withCredentials).toBeFalse();
      req.flush(userJson());

      const user = await done;
      expect(user instanceof User).toBeTrue();
      expect(user.UserName).toBe("admin");
      expect(user.Token).toBe(TOKEN);
    });

    it("login_MustChangePassword_IsPassedOn", async () => {
      const done = service.login("admin", "admin");
      http.expectOne("/webapi/auth/login").flush(userJson([], { MustChangePassword: true }));

      expect((await done).MustChangePassword).toBeTrue();
    });

    it("login_WrongCredentials_IsAnUnauthorisedException", async () => {
      const done = service.login("admin", "wrong");
      http.expectOne("/webapi/auth/login").flush(null, { status: 401, statusText: "Unauthorized" });

      await expectAsync(done).toBeRejectedWith(jasmine.objectContaining({ ErrorText: "UNAUTHORISED", status: 401 }));
    });

    it("login_DoesNotStoreAnything", async () => {
      const done = service.login("admin", "made-up-test-password");
      http.expectOne("/webapi/auth/login").flush(userJson());
      await done;

      expect(localStorage.getItem("jwt")).toBeNull();
      expect(localStorage.getItem("automatica.user")).toBeNull();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("changePassword", () => {
    it("changePassword_StoresThePendingTokenAndPostsBothPasswords", async () => {
      const done = service.changePassword("pending-test-token", "old-test-pw", "new-test-password");

      expect(localStorage.getItem("jwt")).toBe("pending-test-token");
      const req = http.expectOne("/webapi/account/password");
      expect(req.request.method).toBe("POST");
      expect(req.request.body).toEqual({ OldPassword: "old-test-pw", NewPassword: "new-test-password" });
      expect(req.request.withCredentials).toBeTrue();
      req.flush(userJson([], { Token: "new-test-token" }));

      const user = await done;
      expect(user instanceof User).toBeTrue();
      expect(user.Token).toBe("new-test-token");
    });

    it("changePassword_RejectedByTheServer_IsAWebApiException", async () => {
      const done = service.changePassword("pending-test-token", "old", "short");
      http.expectOne("/webapi/account/password").flush({ TypeInfo: "WebApiException", errorText: "PASSWORD_TOO_SHORT", severity: 2 },
        { status: 400, statusText: "Bad Request" });

      const error = await done.catch(e => e);
      expect(error instanceof WebApiException).toBeTrue();
      expect(error.ErrorText).toBe("PASSWORD_TOO_SHORT");
      expect(error.status).toBe(400);
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("session", () => {
    it("saveToLocalStorage_StoresTheUserAndTheToken", async () => {
      const user = await loggedIn([Role.ADMIN_ROLE]);

      expect(localStorage.getItem("jwt")).toBe(TOKEN);
      expect(JSON.parse(localStorage.getItem("automatica.user")).UserName).toBe("admin");
      expect(service.getCurrentUser()).toBe(user);
    });

    it("logout_RemovesTheUserTheTokenAndTheLegacyPassword", async () => {
      await loggedIn([]);
      localStorage.setItem("s1pw", "legacy-test-password");

      service.logout();

      expect(localStorage.getItem("jwt")).toBeNull();
      expect(localStorage.getItem("automatica.user")).toBeNull();
      expect(localStorage.getItem("s1pw")).toBeNull();
      expect(service.getCurrentUser()).toBeUndefined();
      expect(service.getUserFromLocalStore()).toBeUndefined();
    });

    it("getUserFromLocalStore_NoToken_IsNoSession", async () => {
      await loggedIn([]);
      localStorage.removeItem("jwt");

      expect(service.getUserFromLocalStore()).toBeUndefined();
      expect(service.getCurrentUser()).toBeUndefined();
    });

    it("getUserFromLocalStore_AfterAReload_ReadsTheStoredUser", async () => {
      await loggedIn([Role.VISU_ROLE]);
      // a new service instance is what a page reload gives
      const fresh = new LoginService(<any>{}, <any>{}, <any>createTranslateSpy());

      const user = fresh.getUserFromLocalStore();

      expect(user instanceof User).toBeTrue();
      expect(user.UserName).toBe("admin");
      expect(fresh.hasPermission(Role.VISU_ROLE)).toBeTrue();
    });

    it("serverUrl_IsTheApiBase", () => {
      expect(service.serverUrl()).toBe("/webapi");
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("permissions", () => {
    it("hasPermission_UserHasTheRole_IsTrue", async () => {
      await loggedIn([Role.VISU_ROLE]);

      expect(service.hasPermission(Role.VISU_ROLE)).toBeTrue();
    });

    it("hasPermission_Administrator_HasEveryRole", async () => {
      await loggedIn([Role.ADMIN_ROLE]);

      expect(service.hasPermission(Role.VISU_ROLE)).toBeTrue();
      expect(service.hasPermission("anything")).toBeTrue();
    });

    it("hasPermission_RoleMissing_IsFalse", async () => {
      await loggedIn([Role.VISU_ROLE]);

      expect(service.hasPermission(Role.ADMIN_ROLE)).toBeFalse();
    });

    it("hasPermission_NotLoggedIn_IsFalsy", () => {
      expect(service.hasPermission(Role.VISU_ROLE)).toBeFalsy();
    });

    it("guard_NotLoggedIn_GoesToTheLogin", () => {
      const guard = TestBed.inject(HasRoleGuard);

      expect(guard.canActivate(<any>{ data: {} }, <any>{})).toBeFalse();
      expect(router.navigate).toHaveBeenCalledOnceWith(["/login"]);
    });

    it("guard_RouteWithoutRole_NeedsTheAdministrator", async () => {
      await loggedIn([Role.VISU_ROLE]);
      const guard = TestBed.inject(HasRoleGuard);

      expect(guard.canActivate(<any>{ data: {} }, <any>{})).toBeFalse();
      expect(guard.canActivate(<any>{}, <any>{})).toBeFalse();
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it("guard_RouteWithTheUsersRole_IsAllowed", async () => {
      await loggedIn([Role.VISU_ROLE]);
      const guard = TestBed.inject(HasRoleGuard);

      expect(guard.canActivate(<any>{ data: { requiresRole: Role.VISU_ROLE } }, <any>{})).toBeTrue();
      expect(guard.canActivateChild(<any>{ data: { requiresRole: Role.VISU_ROLE } }, <any>{})).toBeTrue();
    });

    it("guard_Administrator_IsAllowedEverywhere", async () => {
      await loggedIn([Role.ADMIN_ROLE]);
      const guard = TestBed.inject(HasRoleGuard);

      expect(guard.canActivate(<any>{ data: {} }, <any>{})).toBeTrue();
      expect(guard.canActivate(<any>{ data: { requiresRole: Role.VISU_ROLE } }, <any>{})).toBeTrue();
    });
  });
});
