import { Component, OnInit, OnDestroy } from "@angular/core";
import { DataHubService } from "../../../communication/hubs/data-hub.service";
import { L10nTranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { ConfigService } from "src/app/services/config.service";
import { NodeDataTypeEnum } from "src/app/base/model/node-data-type";
import * as moment from "moment";
import { AppService } from "src/app/services/app.service";
import { DefaultComponent } from "../default/default.component";
import { HyperSeriesService } from "src/app/services/hyperseries.service";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { LogicInstanceVisuService } from "src/app/services/logic-visu.service";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";

enum GaugeType {
  Linear = 0,
  Circular = 1, 
  CircularThreeRange = 2
}

@Component({
  selector: "visu-gauge",
  templateUrl: "./gauge.component.html",
  styleUrls: ["./gauge.component.scss"]
})
export class GaugeComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {
  GaugeType = GaugeType;

  private valueInterface: RuleInterfaceInstance;
  private scaleStartInterface: RuleInterfaceInstance;
  private scaleEndInterface: RuleInterfaceInstance;
  private ticksInterface: RuleInterfaceInstance;
  private typeInterface: RuleInterfaceInstance;

  scaleStart: number = 0;
  scaleEnd: number = 10;
  ticks: number = 1;
  type: GaugeType = GaugeType.Circular;


  constructor(
    dataHub: DataHubService,
    notify: NotifyService,
    translate: L10nTranslationService,
    configService: ConfigService,
    appService: AppService,
    ruleInstanceVisuService: LogicInstanceVisuService) {
    super(dataHub, notify, translate, configService, ruleInstanceVisuService, appService);
  }

  async ngOnInit() {
    await super.baseOnInit();
    await super.mobileRuleInit();

    var dictionary = await this.ruleInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId);

    this.valueInterface = this.getInterfaceByKey("value");
    this.scaleStartInterface = this.getInterfaceByKey("scale_start");
    this.scaleEndInterface = this.getInterfaceByKey("scale_end");
    this.ticksInterface = this.getInterfaceByKey("ticks");
    this.typeInterface = this.getInterfaceByKey("type");
  
    if (dictionary.hasOwnProperty("scale_start")) {
      this.scaleStart = dictionary["scale_start"];
    }
    if (dictionary.hasOwnProperty("scale_end")) {
      this.scaleEnd = dictionary["scale_end"];
    }
    if (dictionary.hasOwnProperty("ticks")) {
      this.ticks = dictionary["ticks"];
    }
    if (dictionary.hasOwnProperty("gauge_type")) {
      this.type = <GaugeType>parseInt(dictionary["gauge_type"]);
    }

    this.value = this.dataHub.getCurrentValue(this.valueInterface.ObjId)?.value;
  }

  protected onRuleInstanceValueChanged(ruleInterfaceId: any, value: any) {
    switch (ruleInterfaceId) {
      case this.valueInterface.ObjId:
        this.value = value;
        break;
      case this.scaleStartInterface.ObjId:
        this.scaleStart = value;
        break;
      case this.scaleEndInterface.ObjId:
        this.scaleEnd = value;
        break;
      case this.ticksInterface.ObjId:
        this.ticks = value;
        break;
      case this.typeInterface.ObjId:
        this.typeInterface = value;
        break;
    }
  }

  async ngOnDestroy() {
    super.baseOnDestroy();
  }

}
