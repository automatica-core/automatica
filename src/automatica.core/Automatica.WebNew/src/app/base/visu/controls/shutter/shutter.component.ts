import { Component, OnInit, OnDestroy } from "@angular/core";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";

@Component({
  selector: "visu-shutter",
  templateUrl: "./shutter.component.html",
  styleUrls: ["./shutter.component.scss"]
})
export class ShutterComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

  private directionOutput: RuleInterfaceInstance;
  private moveOutput: RuleInterfaceInstance;
  private stopOutput: RuleInterfaceInstance;
  private absolutePositionOutput: RuleInterfaceInstance;

  private directionInput: RuleInterfaceInstance;
  private moveInput: RuleInterfaceInstance;
  private stopInput: RuleInterfaceInstance;
  private absolutePositionInput: RuleInterfaceInstance;
  private upInput: RuleInterfaceInstance;
  private downInput: RuleInterfaceInstance;
  private isMovingOutput: RuleInterfaceInstance;

  private absolutePosition: number;
  private isMoving: boolean;
  displayValue: string;

  shutterIcon: any;

  async ngOnInit() {
    this.baseOnInit();

    super.mobileRuleInit();
    this.directionOutput = this.getInterfaceByKey("directionOutput");
    this.moveOutput = this.getInterfaceByKey("moveOutput");
    this.stopOutput = this.getInterfaceByKey("stopOutput");
    this.absolutePositionOutput = this.getInterfaceByKey("absolutePercentageOutput");


    this.directionInput = this.getInterfaceByKey("direction");
    this.moveInput = this.getInterfaceByKey("ruleInputMove");
    this.stopInput = this.getInterfaceByKey("stop");
    this.absolutePositionInput = this.getInterfaceByKey("absolutePercentage");
    this.upInput = this.getInterfaceByKey("up");
    this.downInput = this.getInterfaceByKey("down");
    this.isMovingOutput = this.getInterfaceByKey("ruleIsMoving");

    this.isMoving = this.dataHub.getCurrentValue(this.isMovingOutput?.ObjId)?.value;
    this.value = this.dataHub.getCurrentValue(this.absolutePositionInput.ObjId)?.value;
    this.updateIcon();

    const data = (await this.ruleInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId));
    const map = new Map(Object.entries(data));

    map.forEach((value, key) => {
      this.onRuleInstanceValueChanged(key, value);
    });
  }

  private updateIcon() {
    if (this.value >= 50 && this.value <= 100) {
      this.shutterIcon = ["fad", "blinds"];
    }
    else {
      this.shutterIcon = ["fad", "blinds-raised"];
    }
  }

  onRuleInstanceValueChanged(interfaceId, value) {
    if (this.absolutePositionOutput.ObjId == interfaceId || this.absolutePositionInput.ObjId == interfaceId) {
      if(value >= 100) {
        value = 100;
      }
      this.absolutePosition = value;
      this.value = value;
      this.displayValue = Math.round(value) + "%";

      this.updateIcon();
    }

    else if (this.isMovingOutput.ObjId == interfaceId) {
      this.isMoving = value;
    }
  }

  async moveUp($event) {
    if (this.isMoving) {
      await this.dataHub.setValue(this.stopInput.ObjId, true);
    }
    else {
      await this.dataHub.setValue(this.upInput.ObjId, true);
      this.isMoving = true;
    }
  }

  async moveDown($event) {
    if (this.isMoving) {
      await this.dataHub.setValue(this.stopInput.ObjId, true);
    }
    else {
      await this.dataHub.setValue(this.downInput.ObjId, true);
      this.isMoving = true;
    }
  }


  ngOnDestroy(): void {
    this.baseOnDestroy();
  }


}
