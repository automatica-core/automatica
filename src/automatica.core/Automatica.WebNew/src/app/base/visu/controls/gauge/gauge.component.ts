import { Component, OnInit, ViewChild } from "@angular/core";
import { BaseMobileComponent } from "../../base-mobile-component";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { NotifyService } from "src/app/services/notify.service";
import { TranslationService } from "angular-l10n";
import { ConfigService } from "src/app/services/config.service";
import { DataService } from "src/app/services/data.service";
import { PropertyInstance } from "src/app/base/model/property-instance";

@Component({
  selector: "app-gauge",
  templateUrl: "./gauge.component.html",
  styleUrls: ["./gauge.component.scss"]
})
export class GaugeComponent extends BaseMobileComponent implements OnInit {


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

  public get self() {
    return this;
  }

  gaugeType: number = 1;

  constructor(dataHub: DataHubService, notify: NotifyService, translate: TranslationService, configService: ConfigService, private dataService: DataService) {
    super(dataHub, notify, translate, configService);

  }

  ngOnInit() {
    this.baseOnInit();
    this.propertyChanged();

  }

  public onItemResized() {

  }


  protected propertyChanged() {

    this.gaugeType = this.getPropertyValue("gauge_type");

    const nodeProperty = this.getProperty("nodeInstance");

    if (!nodeProperty) {
      return;
    }
    this.nodeProperty = nodeProperty;

    super.propertyChanged();
  }

}
