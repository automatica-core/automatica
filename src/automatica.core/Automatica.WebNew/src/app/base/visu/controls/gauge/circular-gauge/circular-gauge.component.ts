import { Component, OnInit, OnDestroy } from "@angular/core";
import { DxCircularGaugeComponent } from "devextreme-angular";
import { NotifyService } from "src/app/services/notify.service";
import { TranslationService } from "angular-l10n";
import { BaseGaugeComponent } from "../base/base.gauge.component";

@Component({
  selector: "app-circular-gauge",
  templateUrl: "./circular-gauge.component.html",
  styleUrls: ["./circular-gauge.component.scss"]
})
export class CircularGaugeComponent extends BaseGaugeComponent<DxCircularGaugeComponent> implements OnInit, OnDestroy {

  public get scaleStart() {
    return this.ref.getPropertyValue("scale_start");
  }
  public get scaleEnd() {
    return this.ref.getPropertyValue("scale_end");
  }
  public get tickInterval() {
    return this.ref.getPropertyValue("ticks");
  }

  customizeText = (arg: any) => {
    if (!this.ref) {
      return arg.valueText;
    }

    const unit = this.ref.getPropertyValue("unit");

    if (!unit) {
      return arg.valueText;
    }

    return arg.valueText + " " + this.ref.getPropertyValue("unit");
  }

  constructor(notifyService: NotifyService, translate: TranslationService) {
    super(notifyService, translate)
  }

  ngOnInit() {
    this.initGauge();
  }

  ngOnDestroy(): void {
    this.destroyGauge();
  }


}
