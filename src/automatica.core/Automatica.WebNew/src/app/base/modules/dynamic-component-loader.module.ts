import {
    ANALYZE_FOR_ENTRY_COMPONENTS, ModuleWithProviders, NgModule, NgModuleFactoryLoader, SystemJsNgModuleLoader, Type,
} from "@angular/core";
import { ROUTES } from "@angular/router";

import { DynamicComponentLoader } from "./dynamic-component-loader.service";
import { DYNAMIC_COMPONENT, DynamicComponentManifest } from "./dynamic-component-manifest";

@NgModule()
export class DynamicComponentLoaderModule {
    static forRoot(): ModuleWithProviders {
        return {
            ngModule: DynamicComponentLoaderModule,
            providers: [
                DynamicComponentLoader,
                SystemJsNgModuleLoader
            ],
        };
    }
    static forChild(component: Type<any>): ModuleWithProviders {
        return {
            ngModule: DynamicComponentLoaderModule,
            providers: [
                { provide: ANALYZE_FOR_ENTRY_COMPONENTS, useValue: component, multi: true },
                // provider for @angular/router to parse
                { provide: ROUTES, useValue: [], multi: true },
                // provider for DynamicComponentLoader to analyze
                { provide: DYNAMIC_COMPONENT, useValue: component },
            ],
        };
    }
}

export { DynamicComponentManifest } from "./dynamic-component-manifest";
