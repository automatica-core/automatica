import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { LabelComponent } from "./label/label.component";
import { DynamicModule } from "ng-dynamic-component";
import { ControlComponent } from "./control.component";
import { ClockComponent } from "./clock/clock.component";
import { DefaultComponent } from "./default/default.component";
import { LinkComponent } from "./link/link.component";
import { SliderComponent } from "./slider/slider.component";
import { DxSliderModule, DxSwitchModule, DxBoxModule, DxNumberBoxModule, DxLoadIndicatorModule, DxColorBoxModule, DxChartModule } from "devextreme-angular";
import { ToggleComponent } from "./buttons/toggle/toggle.component";
import { NumberBoxComponent } from "./number-box/number-box.component";
import { WindowMonitorComponent } from "./window-monitor/window-monitor.component";
import { RgbComponent } from "./rgb/rgb.component";

import { FormsModule } from "@angular/forms";
import { NgColorModule } from "../../color";
import { ChartsComponent } from './charts/charts.component';

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
    DynamicModule.withComponents([
      LabelComponent,
      ClockComponent,
      DefaultComponent,
      LinkComponent,
      SliderComponent,
      ToggleComponent,
      NumberBoxComponent,
      WindowMonitorComponent,
      RgbComponent,
      ChartsComponent
    ])
  ],
  declarations: [
    ControlComponent,
    LabelComponent,
    ClockComponent,
    DefaultComponent,
    LinkComponent,
    SliderComponent,
    ToggleComponent,
    NumberBoxComponent,
    WindowMonitorComponent,
    RgbComponent,
    ChartsComponent
  ],
  exports: [
    ControlComponent
  ]
})
export class ControlsModule { }
