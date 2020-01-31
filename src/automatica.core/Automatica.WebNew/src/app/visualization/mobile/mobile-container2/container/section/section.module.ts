import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { SectionComponent } from "./section.component";
import { ElementModule } from "./element/element.module";



@NgModule({
  declarations: [SectionComponent],
  imports: [
    CommonModule,
    ElementModule
  ],
  exports: [
    SectionComponent
  ]
})
export class SectionModule { }
