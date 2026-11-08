import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ContainerModule } from "./container/container.module";
import { MobileContainerComponent } from "./mobile-container.component";
import { SkeletonModule } from "src/app/shared/skeleton/skeleton.module";


@NgModule({
  declarations: [MobileContainerComponent],
  imports: [
    CommonModule,
    ContainerModule,
    SkeletonModule
  ],
  exports: [
    MobileContainerComponent
  ]
})
export class MobileContainerModule { }
