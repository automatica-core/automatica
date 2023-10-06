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
import { ThemeService } from "src/app/services/theme.service";


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
