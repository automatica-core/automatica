import { Component, OnInit, OnDestroy } from "@angular/core";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { NotifyService } from "src/app/services/notify.service";
import { L10nTranslationService } from "angular-l10n";
import { ConfigService } from "src/app/services/config.service";
import { RuleInstanceVisuService } from "src/app/services/rule-visu.service";
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
  valueType: RuleInterfaceInstance;
  stateType: RuleInterfaceInstance;
  outputType: RuleInterfaceInstance;
  displayValue: any = "0%";

  public get state(): boolean {
    return this._state;
  }
  public set state(v: boolean) {
    this._state = v;
  }


  constructor(
    dataHubService: DataHubService,
    notify: NotifyService,
    translate: L10nTranslationService,
    configService: ConfigService,
    ruleInstanceVisuService: RuleInstanceVisuService,
    appService: AppService) {
    super(dataHubService, notify, translate, configService, ruleInstanceVisuService, appService);
  }


  async ngOnInit() {
    this.baseOnInit();

    super.mobileRuleInit();

    this.valueType = this.getInterfaceByType(RuleInterfaceType.Status);
    this.stateType = this.getInterfaceByType(RuleInterfaceType.Input);
    this.outputType = this.getInterfaceByType(RuleInterfaceType.Output);


  }

  onRuleInstanceValueChanged(interfaceId, value) {
    console.log("ruleinterfacevalue changed", interfaceId, value);

    if (this.outputType.ObjId === interfaceId) {
      this.state = value;
      this.displayValue = `${value}%`;
    }
  }

  onValueChanged($event) {
    this.switch($event.value);
  }

  switch($event) {
    if(this.stateType)
      this.dataHub.setValue(this.stateType.ObjId, $event);
  }

  sliderUpdate($event) {
    if(this.valueType)
      this.dataHub.setValue(this.valueType.ObjId, $event);
  }

  ngOnDestroy(): void {
    this.baseOnDestroy();
  }


}
