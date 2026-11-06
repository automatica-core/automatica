import { HttpContextToken, HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from "@angular/common/http";
import { Injector, inject } from "@angular/core";
import { Router } from "@angular/router";
import { Observable, TimeoutError, catchError, throwError, timeout } from "rxjs";
import { BaseServiceHelper } from "./base-server-helper";
import { LoginService } from "./login.service";
import { HubConnectionService } from "../base/communication/hubs/hub-connection.service";

const DEFAULT_GET_TIMEOUT = 60000;

/**
 * Max time without any response event (progress events of uploads count) before a request fails with status 0.
 * 0 disables the timeout. Default: 60s for GET, none for writes (scan, ETS import or reload legitimately take long,
 * and giving up on the client would not stop the server anyway).
 */
export const REQUEST_TIMEOUT = new HttpContextToken<number | null>(() => null);

/**
 * Set for requests whose 401 is an expected answer (wrong credentials) and must not end the session.
 */
export const SKIP_LOGIN_REDIRECT = new HttpContextToken<boolean>(() => false);

/**
 * True for requests to the automatica server's web api, everything else (assets, other hosts) is passed through untouched.
 */
export function isApiRequest(url: string): boolean {
    const base = BaseServiceHelper.getBaseUrl() ?? "";

    let path = url;
    if (base) {
        if (!url.startsWith(base)) {
            return false;
        }
        path = url.substring(base.length);
    }
    return /^\/*(\.\/)?webapi(\/|\?|$)/i.test(path);
}

function isLoginRequest(req: HttpRequest<unknown>) {
    return /\/webapi\/auth\/login\/?$/i.test(req.url.split("?")[0]);
}

/**
 * Auth header, timeout and 401 handling for every web api request (the services extend BaseService).
 */
export const apiInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> => {
    if (!isApiRequest(req.url)) {
        return next(req);
    }

    const injector = inject(Injector);
    const router = inject(Router);

    const jwt = localStorage.getItem("jwt");
    if (jwt && !req.headers.has("Authorization")) {
        req = req.clone({ setHeaders: { Authorization: "Bearer " + jwt } });
    }

    let response = next(req);

    const timeoutMs = req.context.get(REQUEST_TIMEOUT) ?? (req.method === "GET" ? DEFAULT_GET_TIMEOUT : 0);
    if (timeoutMs > 0) {
        response = response.pipe(timeout({ each: timeoutMs }));
    }

    return response.pipe(
        catchError((error) => {
            if (error instanceof TimeoutError) {
                // same as an unreachable server, callers already handle status 0
                return throwError(() => new HttpErrorResponse({
                    error,
                    status: 0,
                    statusText: "Timeout",
                    url: req.url
                }));
            }

            if (error instanceof HttpErrorResponse && error.status === 401
                && !req.context.get(SKIP_LOGIN_REDIRECT) && !isLoginRequest(req)) {
                endSession(injector, router);
            }
            return throwError(() => error);
        })
    );
};

let endingSession = false;

/**
 * The token expired or was revoked: drop it, stop the hubs and go to the login (the router respects the base href).
 */
function endSession(injector: Injector, router: Router) {
    if (endingSession) {
        return;
    }
    endingSession = true;

    // resolved lazily, both depend on HttpClient and with that on this interceptor
    injector.get(LoginService, null)?.logout();
    const hubs = injector.get(HubConnectionService, null);

    Promise.resolve(hubs?.stop())
        .catch((e) => console.error(e))
        .then(() => {
            if (!router.url.startsWith("/login")) {
                return router.navigate(["/login"]);
            }
            return true;
        })
        .catch((e) => console.error(e))
        .finally(() => endingSession = false);
}
