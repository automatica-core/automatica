import { Component, OnInit, OnDestroy } from "@angular/core";
import { DataHubService } from "../../../communication/hubs/data-hub.service";
import { L10nTranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { BaseMobileComponent } from "../../base-mobile-component";
import { ConfigService } from "src/app/services/config.service";
import { AppService } from "src/app/services/app.service";
import { HyperSeriesService } from "src/app/services/hyperseries.service";
import { AggregatedValueRecord, AggregationType } from "src/app/base/model/aggregated-record-value";

@Component({
  selector: "visu-default",
  templateUrl: "./default.component.html",
  styleUrls: ["./default.component.scss"]
})
export class DefaultComponent extends BaseMobileComponent implements OnInit, OnDestroy {

  hyperSeriesValues: AggregatedValueRecord[] = [];

  constructor(
    dataHub: DataHubService,
    notify: NotifyService,
    translate: L10nTranslationService,
    configService: ConfigService,
    appService: AppService,
    private hyperSeries: HyperSeriesService) {
    super(dataHub, notify, translate, configService, appService);
  }

  public get displayValue() {
    if (this.value === void 0) {
      return void 0;
    }
    return this.value;
  }

  async ngOnInit() {
    await super.baseOnInit();

    await super.propertyChanged();

    if(this.nodeInstanceModel && this.nodeInstanceModel.Trending) {
      this.hyperSeriesValues = await this.hyperSeries.getAggregatedValues(AggregationType.Raw, this.nodeInstanceModel.Id);
      console.log(this.hyperSeriesValues);
    }

  }
  public onItemResized() {

  }


  async ngOnDestroy() {
    super.baseOnDestroy();
  }
}
