import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { CircularGaugeComponent } from "./circular-gauge.component";
import { DxCircularGaugeModule } from "devextreme-angular";



@NgModule({
  declarations: [CircularGaugeComponent],
  imports: [
    CommonModule,
    DxCircularGaugeModule
  ],
  exports: [
    CircularGaugeComponent
  ]
})
export class CircularGaugeModule { }
