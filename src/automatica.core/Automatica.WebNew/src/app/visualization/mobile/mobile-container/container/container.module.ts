import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ContainerComponent } from "./container.component";
import { SectionModule } from "./section/section.module";
import { DxScrollViewModule } from "devextreme-angular";

@NgModule({
  declarations: [ContainerComponent],
  imports: [
    CommonModule,
    SectionModule,
    DxScrollViewModule
  ],
  exports: [
    ContainerComponent
  ]
})
export class ContainerModule { }
