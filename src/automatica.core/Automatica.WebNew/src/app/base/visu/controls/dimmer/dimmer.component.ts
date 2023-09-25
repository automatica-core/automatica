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

interface IDimmerComponent {
  state: boolean;
  value: number;
}

@Component({
  selector: "visu-dimmer",
  templateUrl: "./dimmer.component.html",
  styleUrls: ["./dimmer.component.scss"]
})
export class DimmerComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy, IDimmerComponent {

  private _state: boolean;
  valueInput: RuleInterfaceInstance;
  stateInput: RuleInterfaceInstance;
  outputType: RuleInterfaceInstance;


  outputValue: RuleInterfaceInstance;
  outputState: RuleInterfaceInstance;

  displayValue: any = "0%";

  public get state(): boolean {
    return this._state;
  }
  public set state(v: boolean) {
    this._state = v;
  }


  private _stateValue: number;
  public get stateValue(): number {
    return this._stateValue;
  }
  public set stateValue(v: number) {
    this._stateValue = v;
  }



  constructor(
    dataHubService: DataHubService,
    notify: NotifyService,
    translate: L10nTranslationService,
    configService: ConfigService,
    ruleInstanceVisuService: LogicInstanceVisuService,
    appService: AppService) {
    super(dataHubService, notify, translate, configService, ruleInstanceVisuService, appService);
  }


  async ngOnInit() {
    this.baseOnInit();

    super.mobileRuleInit();

    this.valueInput = this.getInterfaceByType(RuleInterfaceType.Status);
    this.stateInput = this.getInterfaceByType(RuleInterfaceType.Input);

    this.outputValue = this.getInterfaceByKey("outputValue");
    this.outputState = this.getInterfaceByKey("outputState");

    
    this.state = this.dataHub.getCurrentValue(this.stateInput.ObjId)?.value;
    this.stateValue = this.dataHub.getCurrentValue(this.valueInput.ObjId)?.value;


  }

  onRuleInstanceValueChanged(interfaceId, value) {

    if (this.stateInput && this.stateInput.ObjId === interfaceId) {
      this.state = value;
    }
    else if (this.valueInput && this.valueInput.ObjId == interfaceId) {
      this.displayValue = `${Math.round(value)}%`;
      this.stateValue = value;
    }
    else if (this.outputState && this.outputState.ObjId === interfaceId) {
      this.state = value;
    }
    else if (this.outputValue && this.outputValue.ObjId == interfaceId) {
      this.displayValue = `${Math.round(value)}%`;
      this.stateValue = value;
    }
  }

  onValueChanged($event) {
    this.switch($event.value);
  }

  switch(value) {
    if (this.stateInput)
      if (this.state != value)
        this.dataHub.setValue(this.stateInput.ObjId, value);
  }

  sliderUpdate(value) {
    if (this.valueInput)
      if (this.stateValue != value)
        this.dataHub.setValue(this.valueInput.ObjId, value);
  }

  ngOnDestroy(): void {
    this.baseOnDestroy();
  }


}
