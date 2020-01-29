import { Component, OnInit, Input, OnDestroy } from "@angular/core";
import { BaseGaugeComponent } from "../base/base.gauge.component";
import { DxLinearGaugeComponent } from "devextreme-angular";
import { NotifyService } from "src/app/services/notify.service";
import { TranslationService } from "angular-l10n";
import { AppService } from "src/app/services/app.service";

@Component({
  selector: "app-linear-gauge",
  templateUrl: "./linear-gauge.component.html",
  styleUrls: ["./linear-gauge.component.scss"]
})
export class LinearGaugeComponent extends BaseGaugeComponent<DxLinearGaugeComponent> implements OnInit, OnDestroy {

  constructor(
    notifyService: NotifyService, 
    translate: TranslationService,
    appService: AppService) {
    super(notifyService, translate, appService)
  }

  ngOnInit() {
    this.initGauge();
  }

  ngOnDestroy(): void {
    this.destroyGauge();
  }

}
