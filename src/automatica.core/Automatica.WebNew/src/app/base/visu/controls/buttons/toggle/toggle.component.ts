import { Component, OnInit, OnDestroy } from "@angular/core";
import { BaseMobileRuleComponent } from "../../../base-mobile-rule-component";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { NotifyService } from "src/app/services/notify.service";
import { L10nTranslationService } from "angular-l10n";
import { ConfigService } from "src/app/services/config.service";
import { LogicInstanceVisuService } from "src/app/services/logic-visu.service";
import { AppService } from "src/app/services/app.service";
import { RuleInterfaceType } from "src/app/base/model/rule-interface-template";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";


@Component({
  selector: "visu-toggle",
  templateUrl: "./toggle.component.html",
  styleUrls: ["./toggle.component.scss"]
})
export class ToggleComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

  stateType: RuleInterfaceInstance;
  inputType: RuleInterfaceInstance;
  outputType: RuleInterfaceInstance;

  readOnly: boolean = false;

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

    this.stateType = this.getInterfaceByType(RuleInterfaceType.Status);
    this.inputType = this.getInterfaceByType(RuleInterfaceType.Input);
    this.outputType = this.getInterfaceByType(RuleInterfaceType.Output);
    super.registerEvent(this.dataHub.dispatchValue, async (args) => {
      const nodeId = args[1];

      this.onRuleInstanceValueChanged(nodeId, args[2].value);
    });

    this.readOnly = this.getReadOnly() ?? false;

    var cachedValue = this.dataHub.getCurrentValue(this.outputType.ObjId);
    if(cachedValue)
      this.value = cachedValue.value;
  }

  onRuleInstanceValueChanged(interfaceId, value) {
    if (interfaceId == this.outputType.ObjId) {
      this.value = value;
    }
    else if (interfaceId == this.stateType.ObjId) {
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


  onValueChanged($event) {
    this.switch($event.value);
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
