import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { BaseControlComponent } from "./base-control.component";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import { L10nTranslationModule } from "angular-l10n";
import { DxChartModule, DxPopupModule, DxScrollViewModule, DxTemplateModule } from "devextreme-angular";



@NgModule({
  declarations: [BaseControlComponent],
  imports: [
    CommonModule,
    FontAwesomeModule,
    L10nTranslationModule,
    DxPopupModule,
    DxScrollViewModule,
    DxTemplateModule,
    DxChartModule
  ],
  exports: [
    BaseControlComponent
  ]
})
export class BaseControlModule { }
