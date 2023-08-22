import { Component, OnInit, OnDestroy } from "@angular/core";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { NotifyService } from "src/app/services/notify.service";
import { L10nTranslationService } from "angular-l10n";
import { ConfigService } from "src/app/services/config.service";
import { LogicInstanceVisuService } from "src/app/services/logic-visu.service";
import { AppService } from "src/app/services/app.service";
import { RuleInterfaceType } from "src/app/base/model/rule-interface-template";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";


interface SliderData {
  lastValue?: number;
  minValue?: number;
  maxValue?: number;
}

@Component({
  selector: "visu-slider",
  templateUrl: "./slider.component.html",
  styleUrls: ["./slider.component.scss"]
})
export class SliderComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

  private _state: number;
  
  valueType: RuleInterfaceInstance;
  valueMinType: RuleInterfaceInstance;
  valueMaxType: RuleInterfaceInstance;
  
  outputType: RuleInterfaceInstance;

  public get state(): number {
    return this._state;
  }
  public set state(v: number) {
    this._state = v;
  }

  
  private _valueMax? : number;
  public get valueMax() : number {
    return this._valueMax;
  }
  public set valueMax(v : number) {
    this._valueMax = v;
  }
  
  private _valueMin? : number;
  public get valueMin() : number {
    return this._valueMin;
  }
  public set valueMin(v : number) {
    this._valueMin = v;
  }
  

  constructor(
    dataHubService: DataHubService,
    notify: NotifyService,
    translate: L10nTranslationService,
    configService: ConfigService,
    private logicInstanceVisuService: LogicInstanceVisuService,
    appService: AppService) {
    super(dataHubService, notify, translate, configService, logicInstanceVisuService, appService);
  }


  async ngOnInit() {
    this.baseOnInit();

    super.mobileRuleInit();

    this.valueType = this.getInterfaceByType(RuleInterfaceType.Status);
    this.outputType = this.getInterfaceByType(RuleInterfaceType.Output);

    this.valueMaxType = this.getInterfaceByTypeAndName(RuleInterfaceType.Unknown, "value_max");
    this.valueMinType = this.getInterfaceByTypeAndName(RuleInterfaceType.Unknown, "value_min");
    
    var data = await <SliderData>this.logicInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId);
    this.valueMax = data.maxValue;
    this.valueMin = data.minValue;


  }
  format(value) {
    return `${value}`;
  }

  onRuleInstanceValueChanged(interfaceId, value) {
    console.log("ruleinterfacevalue changed", interfaceId, value);

    if (this.outputType && this.outputType.ObjId === interfaceId) {
      this.state = value;
    }
  }

  onValueChanged($event) {
    this.switch($event.value);
  }

  switch($event) {
    if(this.valueType)
      this.dataHub.setValue(this.valueType.ObjId, this.state);
  }


  ngOnDestroy(): void {
    this.baseOnDestroy();
  }


}
