import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { L10nTranslationModule } from "angular-l10n";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import { DxButtonModule, DxDataGridModule } from "devextreme-angular";
import { RadioStationsPageComponent } from "./radio-stations-page/radio-stations-page.component";

/** the radio stations page (admin): built-in stations and the stations the administrator adds */
@NgModule({
  declarations: [
    RadioStationsPageComponent
  ],
  imports: [
    CommonModule,
    L10nTranslationModule,
    FontAwesomeModule,
    DxButtonModule,
    DxDataGridModule
  ],
  exports: [
    RadioStationsPageComponent
  ]
})
export class RadioStationsModule { }
