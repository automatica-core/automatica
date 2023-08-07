import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot } from '@angular/router';
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { BaseService } from './base.service';

export interface IUser {
  user: string;
  avatarUrl?: string;
}

const defaultPath = '/';
const defaultUser = {
  email: 'sandra@example.com',
  avatarUrl: 'https://js.devexpress.com/Demos/WidgetsGallery/JSDemos/images/employees/06.png'
};

@Injectable()
export class AuthService extends BaseService {
  private _user: IUser | null = null;
  get loggedIn(): boolean {
    return !!this._user;
  }

  private _lastAuthenticatedPath: string = defaultPath;
  set lastAuthenticatedPath(value: string) {
    this._lastAuthenticatedPath = value;
  }


  private _isReady: boolean | undefined = undefined;
  public get isReady(): boolean | undefined {
    return this._isReady;
  }


  constructor(private router: Router, http: HttpClient) {
    super(http);
   }



  async loadIsReady() {
    const result = <any>await this.http.get("/webapi/ready", { headers: this.headers() }).toPromise();

    this._isReady = result.ready;

    this.router.navigate(['/login-form']);


    return this.isReady;
  }

  async logIn(user: string, password: string) {

    try {
      // Send request
      // this.router.navigate([this._lastAuthenticatedPath]);

      const response = await this.http.post("/webapi/login", { username: user, password: password }, { headers: this.headers() }).toPromise();

      this._user = { ...defaultUser, user };
      console.log(response);


      localStorage.setItem("token", response!.toString());

      this.router.navigate(['/']);

      return {
        isOk: true,
        data: this._user
      };
    }
    catch {
      return {
        isOk: false,
        message: "Authentication failed"
      };
    }
  }

  async getUser() {
    try {
      // Send request
      if (this._user) {
        return {
          isOk: true,
          data: this._user
        };
      }
      const result = <any>await this.http.get("/webapi/user", { headers: this.headers() }).toPromise();

      this._user = { user: result.user };

      this.router.navigate(['/']);

      return {
        isOk: true,
        data: this._user
      };
    }
    catch {
      return {
        isOk: false,
        data: null
      };
    }
  }

  async setupAccount(username: string, password: string) {
    try {
      // Send request
      const response = await this.http.post("/webapi/setup", { username, password }, { headers: this.headers() }).toPromise();


      this.router.navigate(['/login-form']);
      this._isReady = true;
      return {
        isOk: true
      };
    }
    catch {
      return {
        isOk: false,
        message: "Failed to create account"
      };
    }
  }

  async changePassword(email: string, recoveryCode: string) {
    try {
      // Send request

      return {
        isOk: true
      };
    }
    catch {
      return {
        isOk: false,
        message: "Failed to change password"
      }
    }
  }

  async resetPassword(email: string) {
    try {
      // Send request

      return {
        isOk: true
      };
    }
    catch {
      return {
        isOk: false,
        message: "Failed to reset password"
      };
    }
  }

  async logOut() {
    this._user = null;
    this.router.navigate(['/login-form']);
  }
}

@Injectable()
export class AuthGuardService implements CanActivate {
  constructor(private router: Router, private authService: AuthService) { }

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const isLoggedIn = this.authService.loggedIn;
    const isAuthForm = [
      'login-form',
      'reset-password',
      'create-account',
      'change-password/:recoveryCode'
    ].includes(route.routeConfig?.path || defaultPath);

    const token = localStorage.getItem("token");

    if (!!token) {
      this.authService.getUser();
    }

    if (isLoggedIn && isAuthForm) {
      this.authService.lastAuthenticatedPath = defaultPath;
      this.router.navigate([defaultPath]);
      return false;
    }

    const isReady = this.authService.isReady;

    if (isReady == false) {

      if (route.routeConfig?.path == "create-account")
        return true;

      this.router.navigate(['/create-account']);
      return false;
    }

    if (!isLoggedIn && !isAuthForm) {
      this.router.navigate(['/login-form']);

    }

    if (isLoggedIn) {
      this.authService.lastAuthenticatedPath = route.routeConfig?.path || defaultPath;
    }

    return isLoggedIn || isAuthForm;
  }
}
