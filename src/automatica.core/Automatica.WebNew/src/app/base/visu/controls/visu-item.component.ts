import { Component, OnInit, Input, Type, ViewEncapsulation, HostBinding, OnDestroy, ElementRef } from "@angular/core";
import { LabelComponent } from "./label/label.component";
import { DefaultComponent } from "./default/default.component";
import { LinkComponent } from "./link/link.component";
import { ToggleComponent } from "./buttons/toggle/toggle.component";
import { L10nTranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { BaseComponent } from "../../base-component";
import { VisuObjectMobileInstance } from "../../model/visu/visu-object-mobile-instance";
import { BaseMobileComponent } from "../base-mobile-component";
import { AppService } from "src/app/services/app.service";
import { VisuPageGroupType } from "../../model/visu-page";
import { DimmerComponent } from "./dimmer/dimmer.component";
import { VisuObjectSourceType } from "../../model/visu-object-instance";
import { LogicDefaultComponent } from "./logic-default/logic-default.component";
import { RuleInstance } from "../../model/rule-instance";
import { ToggleNodeComponent } from "./buttons/toggle/toggle.node.component";
import { MediaPlayerComponent } from "./media-player/media-player.component";
import { SliderComponent } from "./slider/slider.component";
import { PushComponent } from "./buttons/push/push.component";
import { GaugeComponent } from "./gauge/gauge.component";
import { ThreeRangeGaugeComponent } from "./ThreeRangeGauge/three-range-gauge.component";
import { ShutterComponent } from "./shutter/shutter.component";

@Component({
  selector: "visu-component",
  templateUrl: "./visu-item.component.html",
  styleUrls: ["./visu-item.component.scss"],
  encapsulation: ViewEncapsulation.None
})
export class VisuItemComponent extends BaseComponent implements OnInit, OnDestroy {



  @HostBinding("class.mobile-control") true;

  @Input()
  item: VisuObjectMobileInstance;

  @Input()
  editMode: boolean = false;

  @Input()
  pageGroupType: VisuPageGroupType = VisuPageGroupType.Favorites;

  inputs: any;

  outputs: any;

  component: Type<BaseMobileComponent>;

  constructor(
    private elementRef: ElementRef,
    notify: NotifyService,
    translate: L10nTranslationService,
    appService: AppService) {
    super(notify, translate, appService);
  }

  ngOnInit() {
    super.baseOnInit();

    this.inputs = {
      item: this.item,
      parent: this.elementRef,
      editMode: this.editMode,
      pageGroupType: this.pageGroupType
    };

    switch (this.item.VisuObjectTemplate.Key) {
      case "label": {
        if (this.item.type === VisuObjectSourceType.RuleInstance) {
          this.component = LogicDefaultComponent;
        } else {
          this.component = LabelComponent;
        }
        break;
      }
      case "link": {
        this.component = LinkComponent;
        break;
      }
      case "toggle-button": {
        if (this.item.objectType instanceof RuleInstance) {
          this.component = ToggleComponent;
        }
        else {
          this.component = ToggleNodeComponent;
        }
        break;
      }
      case "slider": {
        this.component = SliderComponent;
        break;
      }
      case "dimmer": {
        this.component = DimmerComponent;
        break;
      }
      case "media_player": {
        this.component = MediaPlayerComponent;
        break;
      }
      case "push-button": {
        this.component = PushComponent;
        break;
      }
      case "gauge":{
        this.component = GaugeComponent;
        break;
      }
      case "three-range-gauge": {
        this.component = ThreeRangeGaugeComponent;
        break;
      }
      case "shutter": {
        this.component = ShutterComponent;
        break;
      }
      
      default: {

        if (this.item.type === VisuObjectSourceType.RuleInstance) {
          this.component = LogicDefaultComponent;
        } else {
          this.component = LabelComponent;
        }
        break;
      }
    }
  }

  ngOnDestroy(): void {
    super.baseOnDestroy();
  }

}
