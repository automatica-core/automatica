import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ContainerModule } from "./container/container.module";
import { MobileContainerComponent } from "./mobile-container.component";


@NgModule({
  declarations: [MobileContainerComponent],
  imports: [
    CommonModule,
    ContainerModule
  ],
  exports: [
    MobileContainerComponent
  ]
})
export class MobileContainerModule { }
