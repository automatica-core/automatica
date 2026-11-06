import { Injectable } from "@angular/core";
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";

@Injectable({
    providedIn: 'root'
})
export class L10nLazyResolver implements Resolve<Promise<any>> {

    constructor(private translation: L10nTranslationService) { }

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<any> {
        // addProviders skips providers already registered (by name), the route resolves on every navigation
        this.translation.addProviders(route.data.l10nProviders ?? []);
        return this.translation.init();
    }
}