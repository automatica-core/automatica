import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { MobileContainer2Component } from "./mobile-container2.component";
import { ContainerModule } from "./container/container.module";


@NgModule({
  declarations: [MobileContainer2Component],
  imports: [
    CommonModule,
    ContainerModule
  ],
  exports: [
    MobileContainer2Component
  ]
})
export class MobileContainer2Module { }
