import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ElementComponent } from "./element.component";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import { DxSwitchModule } from "devextreme-angular";
import { ControlsModule } from "src/app/base/visu/controls/controls.module";

@NgModule({
  declarations: [ElementComponent],
  imports: [
    CommonModule,
    FontAwesomeModule,
    DxSwitchModule,
    ControlsModule
  ],
  exports: [
    ElementComponent
  ]
})
export class ElementModule { }
