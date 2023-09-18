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


@Component({
  selector: "visu-shutter",
  templateUrl: "./shutter.component.html",
  styleUrls: ["./shutter.component.scss"]
})
export class ShutterComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

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

  


  }

  onRuleInstanceValueChanged(interfaceId, value) {

  }

 
  ngOnDestroy(): void {
    this.baseOnDestroy();
  }


}
