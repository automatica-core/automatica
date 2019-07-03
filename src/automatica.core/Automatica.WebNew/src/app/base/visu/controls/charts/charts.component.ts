import { Component, OnInit, ViewChild } from "@angular/core";
import { BaseMobileComponent } from "../../base-mobile-component";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { NotifyService } from "src/app/services/notify.service";
import { TranslationService } from "angular-l10n";
import { ConfigService } from "src/app/services/config.service";
import { DxChartComponent } from "devextreme-angular";
import DataSource from "devextreme/data/data_source";
import { DataService } from "src/app/services/data.service";
import { PropertyInstance } from "src/app/base/model/property-instance";

@Component({
  selector: "app-charts",
  templateUrl: "./charts.component.html",
  styleUrls: ["./charts.component.scss"]
})
export class ChartsComponent extends BaseMobileComponent implements OnInit {

  @ViewChild("chart", {static: false}) chart: DxChartComponent;

  private _visualRange: any = {};
  HALFDAY: number = 43200000;
  packetsLock: number = 0;
  chartDataSource: any;
  bounds: any;



  private _nodeProperty: PropertyInstance;
  public get nodeProperty(): PropertyInstance {
    return this._nodeProperty;
  }
  public set nodeProperty(v: PropertyInstance) {
    this._nodeProperty = v;
  }

  constructor(dataHub: DataHubService, notify: NotifyService, translate: TranslationService, configService: ConfigService, private dataService: DataService) {
    super(dataHub, notify, translate, configService);

    this.chartDataSource = new DataSource({
      store: [],
      sort: "date",
      paginate: false
    });

    this.bounds = {
      startValue: new Date().setMonth(new Date().getMonth() - 1),
      endValue: new Date()
    };

    this._visualRange = {
      startValue: new Date().setMonth(new Date().getMonth() - 1),
      length: {
        weeks: 8
      }
    };
  }

  ngOnInit() {
    this.baseOnInit();
    this.propertyChanged();
  }

  public onItemResized() {
    if (this.chart && this.chart.instance) {
      this.chart.instance.render();
    }
  }


  protected propertyChanged() {

    const nodeProperty = this.getProperty("nodeInstance");

    if (!nodeProperty) {
      return;
    }
    this.nodeProperty = nodeProperty;

    super.propertyChanged();
  }

  get currentVisualRange(): any {
    return this._visualRange;
  }

  set currentVisualRange(range: any) {
    this._visualRange = range;
    this.onVisualRangeChanged();
  }

  onVisualRangeChanged() {

    if (!this.chart || !this.chart.instance) {
      setTimeout(() => this.onVisualRangeChanged(), 300);
      return;
    }

    const items = this.chart.instance.getDataSource().items();
    if (!items.length ||
      items[0].date - this._visualRange.startValue >= this.HALFDAY ||
      this._visualRange.endValue - items[items.length - 1].date >= this.HALFDAY) {
      this.uploadDataByVisualRange();
    }
  }

  async uploadDataByVisualRange() {
    const dataSource = this.chart.instance.getDataSource();
    const storage = dataSource.items();

    if (!this.packetsLock && this.nodeProperty && this.nodeProperty.Value) {
      this.packetsLock++;
      this.chart.instance.showLoadingIndicator();

      const values = await this.dataService.getTrendings(this.nodeProperty.Value, this._visualRange.startValue, this._visualRange.endValue);
      console.log(values);
      const componentStorage = dataSource.store();
      values.forEach(item => componentStorage.insert(item));
      dataSource.reload();

      this.onVisualRangeChanged();

    }
  }


  getDateString(dateTime: Date) {
    return dateTime ? dateTime.toLocaleDateString("en-US") : "";
  }

}
