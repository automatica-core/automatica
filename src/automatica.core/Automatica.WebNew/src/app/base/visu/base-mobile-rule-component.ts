import { BaseMobileComponent, VisuObjectType } from "./base-mobile-component";
import { L10nTranslationService } from "angular-l10n";
import { RuleInstanceVisuService } from "src/app/services/rule-visu.service";
import { NotifyService } from "src/app/services/notify.service";
import { DataHubService } from "../communication/hubs/data-hub.service";
import { ConfigService } from "src/app/services/config.service";
import { AppService } from "src/app/services/app.service";
import { RuleInterfaceType } from "../model/rule-interface-template";
import { RuleInstance } from "../model/rule-instance";
import { Directive } from "@angular/core";

@Directive()
export abstract class BaseMobileRuleComponent extends BaseMobileComponent {

    protected get ruleInstance(): RuleInstance {
        return <RuleInstance>this.item.objectType;
    }

    constructor(
        private dataHubService: DataHubService,
        notify: NotifyService,
        translate: L10nTranslationService,
        configService: ConfigService,
        public ruleInstanceVisuService: RuleInstanceVisuService,
        appService: AppService) {
        super(dataHubService, notify, translate, configService, appService);
    }

    protected getInterfaceByType(type: RuleInterfaceType) {
        if (this.ruleInstance && this.ruleInstance.Interfaces) {
            for (const interf of this.ruleInstance.Interfaces) {
                if (interf.Template.InterfaceType === type) {
                    return interf;
                }
            }
        }
        return void 0;
    }

    protected async mobileRuleInit() {
        this.registerEvent(this.dataHubService.ruleInstanceValueChanged, (data) => {
            if (this.ruleInstance && this.ruleInstance.Interfaces && this.ruleInstance.Interfaces.filter(a => a.ObjId === data[0]).length > 0) {
                this.onRuleInstanceValueChanged(data[0], data[1]);
            }
        });
        super.baseOnInit();
    }

    protected abstract onRuleInstanceValueChanged(ruleInterfaceId, value);

}
