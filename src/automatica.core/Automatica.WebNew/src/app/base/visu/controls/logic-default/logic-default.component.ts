import { Component, OnInit, OnDestroy } from "@angular/core";
import { DataHubService } from "../../../communication/hubs/data-hub.service";
import { L10nTranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { ConfigService } from "src/app/services/config.service";
import { AppService } from "src/app/services/app.service";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { LogicInstanceVisuService } from "src/app/services/logic-visu.service";
import { RuleInterfaceType } from "src/app/base/model/rule-interface-template";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";

@Component({
  selector: "visu-logic-default",
  templateUrl: "./logic-default.component.html",
  styleUrls: ["./logic-default.component.scss"]
})
export class LogicDefaultComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {
  stateType: RuleInterfaceInstance;

  constructor(
    dataHubService: DataHubService,
    notify: NotifyService,
    translate: L10nTranslationService,
    configService: ConfigService,
    ruleInstanceVisuService: LogicInstanceVisuService,
    appService: AppService) {
    super(dataHubService, notify, translate, configService, ruleInstanceVisuService, appService);
  }

  public get displayValue() {
    if (this.value === void 0) {
      return void 0;
    }
    return this.value;
  }

  async ngOnInit() {
    super.baseOnInit();

    super.mobileRuleInit();
    this.stateType = this.getInterfaceByType(RuleInterfaceType.Status);

    super.propertyChanged();

    this.value = this.dataHub.getCurrentValue(this.stateType.ObjId);

  }

  onRuleInstanceValueChanged(interfaceId, value) {

    if (this.stateType) {
      if (this.stateType.ObjId === interfaceId) {
        this.value = value;
      }
    }
  }

  public onItemResized() {

  }


  async ngOnDestroy() {
    super.baseOnDestroy();
  }
}
