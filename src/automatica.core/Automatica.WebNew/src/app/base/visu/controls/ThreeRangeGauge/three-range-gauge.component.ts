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
  selector: "visu-three-range-gauge",
  templateUrl: "./three-range-gauge.component.html",
  styleUrls: ["./three-range-gauge.component.scss"]
})
export class ThreeRangeGaugeComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {
  GaugeType = GaugeType;

  private valueInterface: RuleInterfaceInstance;

  private firstScaleStartInterface: RuleInterfaceInstance;
  private firstScaleEndInterface: RuleInterfaceInstance;
  private firstColorInterface: RuleInterfaceInstance;

  private secondScaleStartInterface: RuleInterfaceInstance;
  private secondScaleEndInterface: RuleInterfaceInstance;
  private secondColorInterface: RuleInterfaceInstance;

  private thirdScaleStartInterface: RuleInterfaceInstance;
  private thirdScaleEndInterface: RuleInterfaceInstance;
  private thirdColorInterface: RuleInterfaceInstance;

  private ticksInterface: RuleInterfaceInstance;

  firstScaleStart: number = 0;
  firstScaleEnd: number = 33;
  firstColor: string = "red";

  secondScaleStart: number = 34;
  secondScaleEnd: number = 66;
  secondColor: string = "orange";

  thirdScaleStart: number = 67;
  thirdScaleEnd: number = 100;
  thirdColor: string = "green";

  ticks: number = 1;

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

    this.firstScaleStartInterface = this.getInterfaceByKey("first_scale_start");
    this.firstScaleEndInterface = this.getInterfaceByKey("first_scale_end");
    this.firstColorInterface = this.getInterfaceByKey("first_color");

    this.secondScaleStartInterface = this.getInterfaceByKey("second_scale_start");
    this.secondScaleEndInterface = this.getInterfaceByKey("second_scale_end");
    this.secondColorInterface = this.getInterfaceByKey("second_color");

    this.thirdScaleStartInterface = this.getInterfaceByKey("third_scale_start");
    this.thirdScaleEndInterface = this.getInterfaceByKey("third_scale_end");
    this.thirdColorInterface = this.getInterfaceByKey("third_color");

    this.ticksInterface = this.getInterfaceByKey("ticks");

    if (dictionary.hasOwnProperty("first_scale_start")) {
      this.firstScaleStart = dictionary["first_scale_start"];
    }
    if (dictionary.hasOwnProperty("first_scale_end")) {
      this.firstScaleEnd = dictionary["first_scale_end"];
    }
    if (dictionary.hasOwnProperty("first_color")) {
      this.firstColor = dictionary["first_color"];
    }

    if (dictionary.hasOwnProperty("second_scale_start")) {
      this.secondScaleStart = dictionary["second_scale_start"];
    }
    if (dictionary.hasOwnProperty("second_scale_end")) {
      this.secondScaleEnd = dictionary["second_scale_end"];
    }
    if (dictionary.hasOwnProperty("second_color")) {
      this.secondColor = dictionary["second_color"];
    }

    if (dictionary.hasOwnProperty("third_scale_start")) {
      this.thirdScaleStart = dictionary["third_scale_start"];
    }
    if (dictionary.hasOwnProperty("third_scale_end")) {
      this.thirdScaleEnd = dictionary["third_scale_end"];
    }
    if (dictionary.hasOwnProperty("third_color")) {
      this.thirdColor = dictionary["third_color"];
    }
    if (dictionary.hasOwnProperty("ticks")) {
      this.ticks = dictionary["ticks"];
    }
    
    this.value = this.dataHub.getCurrentValue(this.valueInterface.ObjId)?.value;
  }

  protected onRuleInstanceValueChanged(ruleInterfaceId: any, value: any) {
    switch (ruleInterfaceId) {
      case this.valueInterface.ObjId:
        this.value = value;
        break;
      case this.firstScaleStartInterface.ObjId:
        this.firstScaleStart = value;
        break;
      case this.firstScaleEndInterface.ObjId:
        this.firstScaleEnd = value;
        break;
      case this.firstColorInterface.ObjId:
        this.firstColor = value;
        break;
        
      case this.secondScaleStartInterface.ObjId:
        this.secondScaleStart = value;
        break;
      case this.secondScaleEndInterface.ObjId:
        this.secondScaleEnd = value;
        break;
      case this.secondColorInterface.ObjId:
        this.secondColor = value;
        break;
        
      case this.thirdScaleStartInterface.ObjId:
        this.thirdScaleStart = value;
        break;
      case this.thirdScaleEndInterface.ObjId:
        this.thirdScaleEnd = value;
        break;
      case this.thirdColorInterface.ObjId:
        this.thirdColor = value;
        break;

      case this.ticksInterface.ObjId:
        this.ticks = value;
        break;
    }
  }

  async ngOnDestroy() {
    super.baseOnDestroy();
  }

}
