import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { StartingOverlayComponent } from "./starting-overlay.component";
import { DxPopupModule } from "devextreme-angular";
import { TranslationModule } from "angular-l10n";

@NgModule({
  declarations: [StartingOverlayComponent],
  imports: [
    CommonModule,
    DxPopupModule,
    TranslationModule
  ],
  exports: [
    StartingOverlayComponent
  ]
})
export class StartingOverlayModule { }
