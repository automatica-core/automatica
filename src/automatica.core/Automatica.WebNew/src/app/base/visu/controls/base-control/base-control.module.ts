import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { BaseControlComponent } from "./base-control.component";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import { L10nTranslationModule } from "angular-l10n";



@NgModule({
  declarations: [BaseControlComponent],
  imports: [
    CommonModule,
    FontAwesomeModule,
    L10nTranslationModule
  ],
  exports: [
    BaseControlComponent
  ]
})
export class BaseControlModule { }
