import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { VisualizationComponent } from "./visualization.component";
import { VisualizationRoutingModule } from "./visualization.routing.module";
import { SideNavInnerToolbarModule } from "../layouts";
import { RouterModule } from "@angular/router";
import { L10nTranslationModule } from "angular-l10n";

@NgModule({
  declarations: [VisualizationComponent],
  imports: [
    CommonModule,
    VisualizationRoutingModule,
    SideNavInnerToolbarModule,
    RouterModule,
    L10nTranslationModule
  ],
  exports: [
    VisualizationComponent
  ]
})
export class VisualizationModule { }
