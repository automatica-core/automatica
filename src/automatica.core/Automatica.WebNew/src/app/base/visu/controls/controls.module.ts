import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { LabelComponent } from "./label/label.component";
import { DynamicModule } from "ng-dynamic-component";
import { VisuItemComponent } from "./visu-item.component";
import { DefaultComponent } from "./default/default.component";
import { LinkComponent } from "./link/link.component";
import { DxSliderModule, DxSwitchModule, DxBoxModule, DxNumberBoxModule, DxLoadIndicatorModule, DxColorBoxModule, DxChartModule, DxCircularGaugeModule } from "devextreme-angular";
import { ToggleComponent } from "./buttons/toggle/toggle.component";
import { FormsModule } from "@angular/forms";
import { NgColorModule } from "../../color";
import { BaseControlModule } from "./base-control/base-control.module";

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    DxSliderModule,
    DxSwitchModule,
    DxNumberBoxModule,
    DxBoxModule,
    DxLoadIndicatorModule,
    DxColorBoxModule,
    NgColorModule,
    DxChartModule,
    BaseControlModule,
    DynamicModule.withComponents([
      LabelComponent,
      DefaultComponent,
      LinkComponent,
      ToggleComponent
    ])
  ],
  declarations: [
    VisuItemComponent,
    LabelComponent,
    DefaultComponent,
    LinkComponent,
    ToggleComponent
  ],
  exports: [
    VisuItemComponent
  ]
})
export class ControlsModule { }
