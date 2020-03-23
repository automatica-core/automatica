import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { DndModule } from "p3root-angular-dnd";
import { DxLoadPanelModule, DxButtonModule } from "devextreme-angular";
import { RouterModule } from "@angular/router";
import { LocalizationModule, TranslationModule } from "angular-l10n";
import { ControlsModule } from "src/app/base/visu/controls/controls.module";


@NgModule({
  imports: [
    CommonModule,
    RouterModule,
    ControlsModule,
    DndModule,
    DxLoadPanelModule,
    LocalizationModule,
    TranslationModule,
    DxButtonModule
  ]
})
export class MobileModule { }
