import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { LinearGaugeComponent } from "./linear-gauge.component";
import { DxLinearGaugeModule } from "devextreme-angular";



@NgModule({
  declarations: [LinearGaugeComponent],
  imports: [
    CommonModule,
    DxLinearGaugeModule
  ],
  exports: [
    LinearGaugeComponent
  ]
})
export class LinearGaugeModule { }
