import { EventEmitter, Injectable } from "@angular/core";
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { BaseService } from "./base-service";
import { L10nTranslationService } from "angular-l10n";
import { User } from "../base/model/user/user";
import { Role } from "../base/model/user/role";
import { BaseModel } from "../base/model/base-model";


@Injectable()
export class LoginService extends BaseService {
 
  private currentUser: User = void 0;

  public userLoggedIn: EventEmitter<User> = new EventEmitter<User>();

  constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService) {
    super(http, pRouter, translationService);
  }

  login(username: string, password: string) {
    return super.post<User>("auth/login", {
      Username: username,
      Password: password
    }, false);
  }

  /**
   * Changes the password of the signed in user. The token of the login is sent (also the one of a user who must change the
   * password, which allows nothing else), the answer is the user with a new token.
   */
  async changePassword(token: string, oldPassword: string, newPassword: string) {
    localStorage.setItem("jwt", token);
    return await super.post<User>("account/password", {
      OldPassword: oldPassword,
      NewPassword: newPassword
    });
  }

  logout() {
    localStorage.removeItem("automatica.user");
    localStorage.removeItem("jwt");
    // older versions saved the password in plain text
    localStorage.removeItem("s1pw");
    // otherwise the guards still see the user after the logout
    this.currentUser = void 0;
  }

  saveToLocalStorage(user: User) {
    const json = JSON.stringify(user.toJson());
    this.currentUser = user;
    localStorage.setItem("automatica.user", json);
    localStorage.setItem("jwt", user.Token);
  }

  getCurrentUser(): User {
    return this.currentUser;
  } 
  
  serverUrl() {
    return super.getS1Server();
  }


  hasPermission(role: string) {
    const user = this.getUserFromLocalStore();

    if (!user) {
      return;
    }
    for (const x of user.InverseThis2Roles) {
      if (x.This2RoleNavigation.Key === role) {
        return true;
      } else if (x.This2RoleNavigation.Key === Role.ADMIN_ROLE) {
        return true;
      }
    }
    return false;
  }

  getUserFromLocalStore() {
    if (!localStorage.getItem("jwt")) {
      // no token, no session (logout, expired token, login page)
      this.currentUser = void 0;
      return void 0;
    }
    if (!this.currentUser) {
      const str = localStorage.getItem("automatica.user");
      if (str) {
        const json = JSON.parse(str);
        const user = BaseModel.getBaseModelFromJson<User>(json);
        this.currentUser = user;
      }
    }
    return this.getCurrentUser();
  }


}

@Injectable()
export class HasRoleGuard {

  constructor(private router: Router, private userService: LoginService) {

  }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {
    if (!this.userService.getUserFromLocalStore()) {
      // not logged in
      this.router.navigate(["/login"]);
      return false;
    }

    // routes without a role (the admin pages) need the administrator role
    const needsRole = route.data?.requiresRole ?? Role.ADMIN_ROLE;

    if (!this.userService.hasPermission(needsRole)) {
      return false;
    }
    return true;
  }

  canActivateChild(childRoute: ActivatedRouteSnapshot, state: RouterStateSnapshot) {
    return this.canActivate(childRoute, state);
  }
}

