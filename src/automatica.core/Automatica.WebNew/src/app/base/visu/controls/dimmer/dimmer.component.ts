import { Component, OnInit, OnDestroy } from "@angular/core";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
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


  async ngOnInit() {
    this.baseOnInit();

    super.mobileRuleInit();

    this.valueInput = this.getInterfaceByType(RuleInterfaceType.Status);
    this.stateInput = this.getInterfaceByType(RuleInterfaceType.Input);

    this.outputValue = this.getInterfaceByKey("outputValue");
    this.outputState = this.getInterfaceByKey("outputState");


    this.state = this.dataHub.getCurrentValue(this.valueInput.ObjId)?.value;
    this.stateValue = this.dataHub.getCurrentValue(this.valueInput.ObjId)?.value;

    const data = (await this.ruleInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId));
    const map = new Map(Object.entries(data));

    map.forEach((value, key) => {
      this.onRuleInstanceValueChanged(key, value);
    });
    if(this.stateValue > 0) {
      this.state = true;
    }
  }

  onRuleInstanceValueChanged(interfaceId, value) {
     if (this.outputState && this.outputState.ObjId === interfaceId) {
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
