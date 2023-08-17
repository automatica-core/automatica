import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { LabelComponent } from "./label/label.component";
import { DynamicModule } from "ng-dynamic-component";
import { VisuItemComponent } from "./visu-item.component";
import { DefaultComponent } from "./default/default.component";
import { LinkComponent } from "./link/link.component";
import { DxSliderModule, DxSwitchModule, DxBoxModule, DxNumberBoxModule, DxLoadIndicatorModule, DxColorBoxModule, DxChartModule, DxButtonModule, DxResponsiveBoxModule, DxTooltipModule } from "devextreme-angular";
import { ToggleComponent } from "./buttons/toggle/toggle.component";
import { FormsModule } from "@angular/forms";
import { NgColorModule } from "../../color";
import { BaseControlModule } from "./base-control/base-control.module";
import { DimmerComponent } from "./dimmer/dimmer.component";
import { L10nTranslationModule } from "angular-l10n";
import { ComponentsModule } from "../components/components.module";
import { LogicDefaultComponent } from "./logic-default/logic-default.component";
import { ToggleNodeComponent } from "./buttons/toggle/toggle.node.component";
import { MediaPlayerComponent } from './media-player/media-player.component';
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import { DxoLabelModule } from "devextreme-angular/ui/nested";
import { SliderComponent } from "./slider/slider.component";

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
    DxSliderModule,
    L10nTranslationModule,
    DxButtonModule,
    DxTooltipModule,
    DxoLabelModule,
    ComponentsModule,
    DynamicModule,
    DxResponsiveBoxModule,
    FontAwesomeModule
  ],
  declarations: [
    VisuItemComponent,
    LabelComponent,
    DefaultComponent,
    LinkComponent,
    ToggleComponent,
    SliderComponent,
    ToggleNodeComponent,
    DimmerComponent,
    LogicDefaultComponent,
    MediaPlayerComponent
  ],
  exports: [
    VisuItemComponent
  ]
})
export class ControlsModule { }
