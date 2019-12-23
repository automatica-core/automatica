import { Component, OnInit, Input, OnDestroy } from "@angular/core";
import { BaseGaugeComponent } from "../base/base.gauge.component";
import { DxLinearGaugeComponent } from "devextreme-angular";
import { NotifyService } from "src/app/services/notify.service";
import { TranslationService } from "angular-l10n";

@Component({
  selector: "app-linear-gauge",
  templateUrl: "./linear-gauge.component.html",
  styleUrls: ["./linear-gauge.component.scss"]
})
export class LinearGaugeComponent extends BaseGaugeComponent<DxLinearGaugeComponent> implements OnInit, OnDestroy {

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
