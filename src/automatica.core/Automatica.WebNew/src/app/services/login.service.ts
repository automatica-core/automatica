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

  logout() {
    localStorage.removeItem("automtica.user");
    localStorage.removeItem("jwt");
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

    const needsRole = route.data.requiresRole;

    if (!this.userService.hasPermission(needsRole)) {
      return false;
    }
    return true;
  }
}

@Injectable()
export class HomepageRouteGuard {

  constructor(private router: Router, private loginService: LoginService) {
  }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {
    if (!this.loginService.getUserFromLocalStore()) {
      // not logged in
      this.router.navigate(["/login"]);
      return false;
    }

    this.router.navigateByUrl("/visualization/page");
  }

}

