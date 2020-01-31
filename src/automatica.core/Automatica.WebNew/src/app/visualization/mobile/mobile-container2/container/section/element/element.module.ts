import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ElementComponent } from "./element.component";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import { DxSwitchModule } from "devextreme-angular";

@NgModule({
  declarations: [ElementComponent],
  imports: [
    CommonModule,
    FontAwesomeModule,
    DxSwitchModule
  ],
  exports: [
    ElementComponent
  ]
})
export class ElementModule { }
