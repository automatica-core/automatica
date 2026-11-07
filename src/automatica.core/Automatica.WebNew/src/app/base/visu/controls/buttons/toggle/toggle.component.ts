import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";
import { BaseMobileRuleComponent } from "../../../base-mobile-rule-component";
import { RuleInterfaceType } from "src/app/base/model/rule-interface-template";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";


@Component({
    selector: "visu-toggle",
    templateUrl: "./toggle.component.html",
    styleUrls: ["./toggle.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ToggleComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

  stateType: RuleInterfaceInstance;
  inputType: RuleInterfaceInstance;
  outputType: RuleInterfaceInstance;

  readOnly: boolean = false;

  async ngOnInit() {
    this.baseOnInit();

    super.mobileRuleInit();

    this.stateType = this.getInterfaceByType(RuleInterfaceType.Status);
    this.inputType = this.getInterfaceByType(RuleInterfaceType.Input);
    this.outputType = this.getInterfaceByType(RuleInterfaceType.Output);
    
    super.registerObservable(this.dataHub.nodeValues(this.interfaceIds), async (args) => {
      const nodeId = args[1];

      this.onRuleInstanceValueChanged(nodeId, args[2].value);
    });

    // without an input interface the tile can only show the state
    this.readOnly = (this.getReadOnly() ?? false) || !this.inputType;
    this.value = this.dataHub.getCurrentValue(this.outputType.ObjId)?.value;
    

    const data = (await this.ruleInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId));
    const map = new Map(Object.entries(data));

    map.forEach((value, key) => {
      this.onRuleInstanceValueChanged(key, value);
    });
  }

  onRuleInstanceValueChanged(interfaceId, value) {
    if (interfaceId == this.outputType.ObjId || interfaceId == this.stateType.ObjId) {
      this.value = value;
    }
  }

  public get displayValue() {
    if (this.value === void 0) {
      return void 0;
    }

    if (this.inputType) {
      return this.translate.translate(this.value ? "COMMON.TOGGLE.ON" : "COMMON.TOGGLE.OFF");;
    }
    else {
      return this.value;
    }
  }


  public get isOn(): boolean {
    return this.realValue === true || this.realValue === 1 || this.realValue === "true";
  }

  onValueChanged($event) {
    this.switch($event.value);
  }

  /** the whole tile is the switch */
  toggle() {
    if (this.readOnly) {
      return;
    }
    this.switch(!this.isOn);
  }

  ngOnDestroy(): void {
    this.baseOnDestroy();
  }


  switch(value) {

    if (this.inputType) {
      if (this.value != value) {
        this.dataHub.setValue(this.inputType.ObjId, value);
      }
    }
  }

}
