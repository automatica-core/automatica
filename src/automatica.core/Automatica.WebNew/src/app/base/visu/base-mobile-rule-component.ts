import { BaseMobileComponent } from "./base-mobile-component";
import { TranslationService } from "angular-l10n";
import { RuleInstanceVisuService } from "src/app/services/rule-visu.service";
import { NotifyService } from "src/app/services/notify.service";
import { RuleInstance } from "../model/rule-instance";
import { DataHubService } from "../communication/hubs/data-hub.service";
import { ConfigService } from "src/app/services/config.service";

export abstract class BaseMobileRuleComponent extends BaseMobileComponent {


    public get ruleInstance(): RuleInstance {
        return this.item.RuleInstance;
    }


    constructor(private dataHubService: DataHubService, notify: NotifyService, translate: TranslationService, configService: ConfigService, public ruleInstanceVisuService: RuleInstanceVisuService) {
        super(dataHubService, notify, translate, configService);
    }

    protected async mobileRuleInit() {
        const ruleData = await this.ruleInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId);

        this.onRuleInstanceValueChanged(ruleData);

        this.registerEvent(this.dataHubService.ruleInstanceValueChanged, (data) => {
            if (data[0] === this.ruleInstance.ObjId) {
                this.onRuleInstanceValueChanged(data[1]);
            }
        });
        super.baseOnInit();
    }

    protected onRuleInstanceValueChanged(value) {

    }

}
