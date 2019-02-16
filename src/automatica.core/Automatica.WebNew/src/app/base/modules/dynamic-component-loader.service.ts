import { ComponentFactory, Inject, Injectable, Injector, NgModuleFactoryLoader, SystemJsNgModuleLoader, Compiler } from "@angular/core";

import { DYNAMIC_COMPONENT, DynamicComponentManifest } from "./dynamic-component-manifest";
import { Observable } from "rxjs";
import { VisuObjectTemplate } from "../model/visu-object-template";


declare const SystemJS;


@Injectable()
export class DynamicComponentLoader {

  constructor(private loader: SystemJsNgModuleLoader, private injector: Injector, private compiler: Compiler) { }

  /** Retrieve a ComponentFactory, based on the specified componentId (defined in the DynamicComponentManifest array). */
  getComponentFactory<T>(template: VisuObjectTemplate, injector?: Injector): Promise<ComponentFactory<T>> {
    const manifest = template;
    if (!manifest) {
      throw new Error((`DynamicComponentLoader: Unknown componentId "${template.Key}"`));
    }

    const path = `Visu/CompareVisuComponents/compare-components.module`;

    SystemJS.import(path).then((module) => {
      this.compiler.compileModuleAndAllComponentsAsync(module.TModule)
        .then((compiled) => {
          const m = compiled.ngModuleFactory.create(this.injector);
          const factory = compiled.componentFactories[0];
          const cmp = factory.create(this.injector, [], null, m);
        })
    });

    const p = this.loader.load(path)
      .then(ngModuleFactory => {
        const moduleRef = ngModuleFactory.create(injector || this.injector);
        const dynamicComponentType = moduleRef.injector.get(DYNAMIC_COMPONENT);
        if (!dynamicComponentType) {
          throw new Error(
            `DynamicComponentLoader: Dynamic module for componentId "${template.Key}" does not contain DYNAMIC_COMPONENT as a provider.`,
          );
        }

        return moduleRef.componentFactoryResolver.resolveComponentFactory<T>(dynamicComponentType);
      });
    return p;
  }
}
