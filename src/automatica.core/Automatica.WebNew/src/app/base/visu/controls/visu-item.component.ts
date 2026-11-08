import { Component, OnInit, Input, Type, ViewEncapsulation, HostBinding, OnDestroy, ElementRef, ChangeDetectionStrategy } from "@angular/core";
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
import { CameraComponent } from "./camera/camera.component";
import { SliderComponent } from "./slider/slider.component";
import { PushComponent } from "./buttons/push/push.component";
import { GaugeComponent } from "./gauge/gauge.component";
import { ThreeRangeGaugeComponent } from "./ThreeRangeGauge/three-range-gauge.component";
import { ShutterComponent } from "./shutter/shutter.component";
import { LightingControllerComponent } from "./lighting-controller/lighting-controller.component";
import { SecurityComponent } from "./security/security.component";
import { EnergyFlowComponent } from "./energy-flow/energy-flow.component";
import { RoomControllerComponent } from "./room-controller/room-controller.component";
import { ClimateUnitComponent } from "./climate/climate-unit.component";
import { ClimateCentralComponent } from "./climate/climate-central.component";
import { StatusComponent } from "./text-status/status/status.component";
import { StatusMonitorComponent } from "./text-status/status-monitor/status-monitor.component";
import { TrackerComponent } from "./text-status/tracker/tracker.component";

/** tiles of the status, status monitor and tracker logics; they need the values of that logic */
const TEXT_STATUS_TILES: { [key: string]: Type<BaseMobileComponent> } = {
  "status": StatusComponent,
  "status-monitor": StatusMonitorComponent,
  "tracker": TrackerComponent
};
import { GarageComponent } from "./garage/garage.component";
import { MediaControllerComponent } from "./media-controller/media-controller.component";
import { IrrigationComponent } from "./irrigation/irrigation.component";

@Component({
    selector: "visu-component",
    templateUrl: "./visu-item.component.html",
    styleUrls: ["./visu-item.component.scss"],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
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
      case "camera": {
        this.component = CameraComponent;
        break;
      }
      case "energy-flow": {
        // the tile of the energy monitor logic, it needs the values of that logic
        this.component = this.item.type === VisuObjectSourceType.RuleInstance ? EnergyFlowComponent : LabelComponent;
        break;
      }
      case "status":
      case "status-monitor":
      case "tracker": {
        this.component = this.item.type === VisuObjectSourceType.RuleInstance ? TEXT_STATUS_TILES[this.item.VisuObjectTemplate.Key] : LabelComponent;
        break;
      }
      case "room-controller": {
        this.component = RoomControllerComponent;
        break;
      }
      case "climate-unit": {
        this.component = ClimateUnitComponent;
        break;
      }
      case "climate-central": {
        this.component = ClimateCentralComponent;
        break;
      }
      case "security": {
        this.component = SecurityComponent;
        break;
      }
      case "lighting-controller": {
        this.component = LightingControllerComponent;
        break;
      }
      // tiles of logic blocks: they need the values of their logic
      case "garage": {
        this.component = this.item.type === VisuObjectSourceType.RuleInstance ? GarageComponent : LabelComponent;
        break;
      }
      case "media-controller": {
        this.component = this.item.type === VisuObjectSourceType.RuleInstance ? MediaControllerComponent : LabelComponent;
        break;
      }
      case "irrigation": {
        this.component = this.item.type === VisuObjectSourceType.RuleInstance ? IrrigationComponent : LabelComponent;
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
