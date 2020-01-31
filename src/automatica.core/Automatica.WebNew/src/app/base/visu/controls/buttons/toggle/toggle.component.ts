import { Component, OnInit, OnDestroy } from "@angular/core";
import { BaseMobileRuleComponent } from "../../../base-mobile-rule-component";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { NotifyService } from "src/app/services/notify.service";
import { TranslationService } from "angular-l10n";
import { ConfigService } from "src/app/services/config.service";
import { RuleInstanceVisuService } from "src/app/services/rule-visu.service";
import { AppService } from "src/app/services/app.service";
import { RuleInterfaceType } from "src/app/base/model/rule-interface-template";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";

interface IToggleComponent {
  state: boolean;
}

@Component({
  selector: "visu-toggle",
  templateUrl: "./toggle.component.html",
  styleUrls: ["./toggle.component.scss"]
})
export class ToggleComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy, IToggleComponent {

  private _state: boolean;
  stateType: RuleInterfaceInstance;
  inputType: RuleInterfaceInstance;
  outputType: RuleInterfaceInstance;

  public get state(): boolean {
    return this._state;
  }
  public set state(v: boolean) {
    this._state = v;
  }

  constructor(
    dataHubService: DataHubService,
    notify: NotifyService,
    translate: TranslationService,
    configService: ConfigService,
    ruleInstanceVisuService: RuleInstanceVisuService,
    appService: AppService) {
    super(dataHubService, notify, translate, configService, ruleInstanceVisuService, appService);
  }


  async ngOnInit() {
    this.baseOnInit();

    super.mobileRuleInit();

    this.stateType = this.getInterfaceByType(RuleInterfaceType.Status);
    this.inputType = this.getInterfaceByType(RuleInterfaceType.Input);
    this.outputType = this.getInterfaceByType(RuleInterfaceType.Output);

  }

  onRuleInstanceValueChanged(interfaceId, value) {
    console.log("ruleinterfacevalue changed", interfaceId, value);

    if (this.stateType.ObjId === interfaceId) {
      this.state = value;
    }
  }

  onValueChanged($event) {
    this.dataHub.setValue(this.outputType.ObjId, $event.value);
  }

  ngOnDestroy(): void {
    this.baseOnDestroy();
  }


}
