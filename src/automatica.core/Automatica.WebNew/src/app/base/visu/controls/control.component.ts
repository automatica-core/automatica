import { Component, OnInit, Input, Type, ViewEncapsulation, HostBinding, OnDestroy, ElementRef } from "@angular/core";
import { LabelComponent } from "./label/label.component";
import { ClockComponent } from "./clock/clock.component";
import { DefaultComponent } from "./default/default.component";
import { LinkComponent } from "./link/link.component";
import { SliderComponent } from "./slider/slider.component";
import { ToggleComponent } from "./buttons/toggle/toggle.component";
import { NumberBoxComponent } from "./number-box/number-box.component";
import { PropertyInstance } from "../../model/property-instance";
import { DataHubService } from "../../communication/hubs/data-hub.service";
import { TranslationService } from "angular-l10n";
import { WindowMonitorComponent } from "./window-monitor/window-monitor.component";
import { NotifyService } from "src/app/services/notify.service";
import { BaseComponent } from "../../base-component";
import { VisuObjectMobileInstance } from "../../model/visu/visu-object-mobile-instance";
import { BaseMobileComponent } from "../base-mobile-component";
import { RgbComponent } from "./rgb/rgb.component";
import { ChartsComponent } from "./charts/charts.component";

@Component({
  selector: "visu-component",
  templateUrl: "./control.component.html",
  styleUrls: ["./control.component.scss"],
  encapsulation: ViewEncapsulation.None
})
export class ControlComponent extends BaseComponent implements OnInit, OnDestroy {



  @HostBinding("class.mobile-control") true;

  @Input()
  item: VisuObjectMobileInstance;

  @Input()
  editMode: boolean = false;

  inputs: any;

  outputs: any;

  component: Type<BaseMobileComponent>;

  constructor(private elementRef: ElementRef, notify: NotifyService, translate: TranslationService) {
    super(notify, translate);
  }

  ngOnInit() {
    super.baseOnInit();

    this.inputs = {
      item: this.item,
      parent: this.elementRef,
      editMode: this.editMode
    };

    switch (this.item.VisuObjectTemplate.Key) {
      case "label": {
        this.component = LabelComponent;
        break;
      }
      case "clock": {
        this.component = ClockComponent;
        break;
      }
      case "link": {
        this.component = LinkComponent;
        break;
      }
      case "slider": {
        this.component = SliderComponent;
        break;
      }
      case "toggle-button": {
        this.component = ToggleComponent;
        break;
      }
      case "number-box": {
        this.component = NumberBoxComponent;
        break;
      }
      case "window-monitor": {
        this.component = WindowMonitorComponent;
        break;
      }
      case "rgba": {
        this.component = RgbComponent;
        break;
      }
      case "chart": {
        this.component = ChartsComponent;
        break;
      }
      default: {
        this.component = DefaultComponent;
        break;
      }
    }
  }

  ngOnDestroy(): void {
    super.baseOnDestroy();
  }

}
