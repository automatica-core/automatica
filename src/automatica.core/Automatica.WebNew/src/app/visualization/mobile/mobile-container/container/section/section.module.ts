import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { SectionComponent } from "./section.component";
import { ElementModule } from "./element/element.module";
import { L10nTranslationModule } from "angular-l10n";



@NgModule({
  declarations: [SectionComponent],
  imports: [
    CommonModule,
    ElementModule,
    L10nTranslationModule
  ],
  exports: [
    SectionComponent
  ]
})
export class SectionModule { }
