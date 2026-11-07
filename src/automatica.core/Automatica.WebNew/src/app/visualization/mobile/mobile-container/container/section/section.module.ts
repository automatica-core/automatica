import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { SectionComponent } from "./section.component";
import { ElementModule } from "./element/element.module";
import { L10nTranslationModule } from "angular-l10n";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";



@NgModule({
  declarations: [SectionComponent],
  imports: [
    CommonModule,
    ElementModule,
    L10nTranslationModule,
    FontAwesomeModule
  ],
  exports: [
    SectionComponent
  ]
})
export class SectionModule { }
