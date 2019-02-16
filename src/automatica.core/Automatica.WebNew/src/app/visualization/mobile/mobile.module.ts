import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { MobileContainerComponent } from "./mobile-container/mobile-container.component";
import { GridsterModule } from "angular-gridster2";
import { DndModule } from "ngx-dnd";
import { DxLoadPanelModule, DxButtonModule } from "devextreme-angular";
import { RouterModule } from "@angular/router";
import { LocalizationModule, TranslationModule } from "angular-l10n";
import { ControlsModule } from "src/app/base/visu/controls/controls.module";


@NgModule({
  imports: [
    CommonModule,
    RouterModule,
    GridsterModule,
    ControlsModule,
    DndModule,
    DxLoadPanelModule,
    LocalizationModule,
    TranslationModule,
    DxButtonModule
  ],
  declarations: [
    MobileContainerComponent
  ],
  exports: [
    MobileContainerComponent
  ]
})
export class MobileModule { }
