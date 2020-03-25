import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { BaseControlComponent } from "./base-control.component";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import { TranslationModule } from "angular-l10n";



@NgModule({
  declarations: [BaseControlComponent],
  imports: [
    CommonModule,
    FontAwesomeModule,
    TranslationModule
  ],
  exports: [
    BaseControlComponent
  ]
})
export class BaseControlModule { }
