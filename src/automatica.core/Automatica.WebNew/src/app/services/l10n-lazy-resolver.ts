import { Injectable, Inject } from "@angular/core";
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import { L10N_CONFIG, L10nConfig, L10nTranslationService } from "angular-l10n";

@Injectable({
    providedIn: 'root'
})
export class L10nLazyResolver implements Resolve<Promise<any>> {

    constructor(@Inject(L10N_CONFIG) private config: L10nConfig, private translation: L10nTranslationService) { }

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<any> {
        this.config.providers = [
            ...this.config.providers,
            ...route.data.l10nProviders
        ];
        return this.translation.init();
    }
}