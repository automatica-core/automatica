import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { GaugeComponent } from "./gauge.component";
import { LinearGaugeModule } from "./linear-gauge/linear-gauge.module";
import { CircularGaugeModule } from "./circular-gauge/circular-gauge.module";



@NgModule({
  declarations: [
    GaugeComponent
  ],
  imports: [
    CommonModule,
    LinearGaugeModule,
    CircularGaugeModule
  ],
  exports: [
    GaugeComponent
  ]
})
export class GaugeModule { }
