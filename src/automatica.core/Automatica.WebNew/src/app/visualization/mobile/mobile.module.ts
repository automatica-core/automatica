import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { DndModule } from "p3root-angular-dnd";
import { DxLoadPanelModule, DxButtonModule } from "devextreme-angular";
import { RouterModule } from "@angular/router";
import { L10nIntlModule, L10nTranslationModule } from "angular-l10n";
import { ControlsModule } from "src/app/base/visu/controls/controls.module";


@NgModule({
  imports: [
    CommonModule,
    RouterModule,
    ControlsModule,
    DndModule,
    DxLoadPanelModule,
    L10nIntlModule,
    L10nTranslationModule,
    DxButtonModule
  ]
})
export class MobileModule { }
