import { Component, OnInit, OnDestroy } from "@angular/core";
import { DataHubService } from "../../../communication/hubs/data-hub.service";
import { L10nTranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { BaseMobileComponent } from "../../base-mobile-component";
import { ConfigService } from "src/app/services/config.service";
import { NodeDataTypeEnum } from "src/app/base/model/node-data-type";
import * as moment from "moment";
import { AppService } from "src/app/services/app.service";
import { VisuObjectMobileInstance } from "src/app/base/model/visu";
import { DefaultComponent } from "../default/default.component";
import { HyperSeriesService } from "src/app/services/hyperseries.service";
import { AggregatedValueRecord, AggregationType } from "src/app/base/model/aggregated-record-value";

@Component({
  selector: "visu-label",
  templateUrl: "./label.component.html",
  styleUrls: ["./label.component.scss"]
})
export class LabelComponent extends DefaultComponent implements OnInit, OnDestroy {

  hyperSeriesChartValues: AggregatedValueRecord[] = [];

  aggregations: AggregationType[] = [AggregationType.Raw, AggregationType.Hourly, AggregationType.Daily, AggregationType.Weekly, AggregationType.Monthly, AggregationType.Yearly];
  dateRangeValue: [Date, Date] = void 0;
  aggregationTypeValue: AggregationType;
  countValues = 100;

  enablePopup = false;
  argumentAxisFormatString: any;

  public get displayValue() {
    if (this.value === void 0) {
      return void 0;
    }

    if (this.nodeInstanceModel && this.nodeInstanceModel.NodeTemplate && this.nodeInstanceModel.NodeTemplate.NodeType) {
      switch (this.nodeInstanceModel.NodeTemplate.NodeType.Type) {
        case NodeDataTypeEnum.Date:
          return moment(this.value).format(this.translate.translate("COMMON.DATETIMEFORMAT.DATE"));
        case NodeDataTypeEnum.DateTime:
          return moment(this.value).format(this.translate.translate("COMMON.DATETIMEFORMAT.DATETIME"));
        case NodeDataTypeEnum.Time:
          return moment(this.value, ["HH:mm:ss.SSS"]).format(this.translate.translate("COMMON.DATETIMEFORMAT.TIME"));

        default:
          return this.value;
      }
    }
    return this.value;
  }

  constructor(
    dataHub: DataHubService,
    notify: NotifyService,
    translate: L10nTranslationService,
    configService: ConfigService,
    appService: AppService,
    private hyperSeriesService: HyperSeriesService) {
    super(dataHub, notify, translate, configService, appService, hyperSeriesService);

    const msInDay = 1000 * 60 * 60 * 24;
    const now = new Date();
    this.dateRangeValue = [
      new Date(now.getTime() - msInDay * 5),
      new Date(now.getTime()),
    ];

    this.aggregationTypeValue = AggregationType.Hourly;

    this.argumentAxisFormatString = "shortTime";
  }

  async ngOnInit() {
    await super.ngOnInit();

    this.enablePopup = this.nodeInstanceModel && this.nodeInstanceModel.Trending;

  }

  private async fetchValues() {

    switch (this.aggregationTypeValue) {
      case AggregationType.Raw:
      case AggregationType.Hourly:
        this.argumentAxisFormatString = "shortTime"; 
        break;

      case AggregationType.Weekly:
      case AggregationType.Monthly:
      case AggregationType.Yearly:
        this.argumentAxisFormatString = "shortDate";
      default:
        this.argumentAxisFormatString = "shortDateShortTime";
        break;
    }

    this.hyperSeriesChartValues = await this.hyperSeriesService.getAggregatedValues(this.aggregationTypeValue, this.nodeInstanceModel.Id, this.dateRangeValue[0], this.dateRangeValue[1], this.countValues);
  }

  async onPopupShowing($event) {
    await this.fetchValues();
  }

  async onValueChanged($event) {
    await this.fetchValues();
  }

  async ngOnDestroy() {
    super.baseOnDestroy();
  }

}
