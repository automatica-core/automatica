import { NodeInstance2RulePage } from "src/app/base/model/node-instance-2-rule-page";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";
import { RulePage } from "src/app/base/model/rule-page";
import { Link } from "src/app/base/model/link";
import { Guid } from "src/app/base/utils/Guid";
import { NodeInstance } from "src/app/base/model/node-instance";
import { L10nTranslationService } from "angular-l10n";
import { LogicEngineService } from "src/app/services/logicengine.service";

declare var draw2d: any;

export class LinkService {


    private _isInit: boolean = true;

    static async handleOnDisconnection(linkService: LinkService, connection: any, isInput: boolean) {
        await linkService.removeLink(connection.connection.getUserData(), isInput);
    }

    static async handleOnConnection(linkService: LinkService, port: any, connection: any, isInput: boolean,
        element: RuleInterfaceInstance | NodeInstance2RulePage, ruleEngineService: LogicEngineService) {
        let userData: Link = connection.connection.getUserData();

        if (!userData) {
            userData = linkService.addLink(element);
            port.setUserData(userData);
            connection.connection.setUserData(userData);
        }

        if (userData) {

            connection.connection.setRouter(new draw2d.layout.connection.ManhattanBridgedConnectionRouter());
            const existingLink: Link = userData;

            if (element instanceof RuleInterfaceInstance) {
                if (isInput) {
                    existingLink.ToRuleInstance = element;
                } else {
                    existingLink.FromRuleInstance = element;
                }
            } else if (element instanceof NodeInstance2RulePage) {
                if (isInput) {
                    existingLink.ToNodeInstance = element;
                } else {
                    existingLink.FromNodeInstance = element;
                }
            }

            if (userData.isNewObject) { // set only if the link is new
                if (existingLink.FromRuleInstance && existingLink.ToRuleInstance) { // from and to rule
                    existingLink.This2NodeInstance2RulePageOutput = void 0;
                    existingLink.This2RuleInterfaceInstanceOutput = existingLink.FromRuleInstance.ObjId;

                    existingLink.This2NodeInstance2RulePageInput = void 0;
                    existingLink.This2RuleInterfaceInstanceInput = existingLink.ToRuleInstance.ObjId;

                } else if (existingLink.FromRuleInstance && existingLink.ToNodeInstance) {  // from rule to node
                    existingLink.This2NodeInstance2RulePageOutput = void 0;
                    existingLink.This2RuleInterfaceInstanceOutput = existingLink.FromRuleInstance.ObjId;

                    existingLink.This2NodeInstance2RulePageInput = existingLink.ToNodeInstance.ObjId;
                    existingLink.This2RuleInterfaceInstanceInput = void 0;

                } else if (existingLink.FromNodeInstance && existingLink.ToRuleInstance) { // from node to rule
                    existingLink.This2NodeInstance2RulePageOutput = existingLink.FromNodeInstance.ObjId;
                    existingLink.This2RuleInterfaceInstanceOutput = void 0;

                    existingLink.This2NodeInstance2RulePageInput = void 0;
                    existingLink.This2RuleInterfaceInstanceInput = existingLink.ToRuleInstance.ObjId;

                } else if (existingLink.FromNodeInstance && existingLink.ToNodeInstance) {  // from node to node
                    existingLink.This2NodeInstance2RulePageOutput = existingLink.FromNodeInstance.ObjId;
                    existingLink.This2RuleInterfaceInstanceOutput = void 0;

                    existingLink.This2NodeInstance2RulePageInput = existingLink.ToNodeInstance.ObjId;
                    existingLink.This2RuleInterfaceInstanceInput = void 0;
                }
            }
        }

        if (!linkService.isInit && userData.isValid && isInput) {
            await ruleEngineService.addOrUpdateLink(userData);
        }

    }

    public get isInit(): boolean {
        return this._isInit;
    }
    public set isInit(v: boolean) {
        this._isInit = v;
    }

    constructor(private page: RulePage, public translate: L10nTranslationService, private ruleEngineService: LogicEngineService) {

    }

    private addLink(element: NodeInstance2RulePage | RuleInterfaceInstance): Link {
        if (this.isInit) {
            return void 0;
        }
        const linkData = new Link();

        linkData.ObjId = Guid.MakeNew().ToString();
        linkData.This2RulePage = this.page.ObjId;

        this.page.Links.push(linkData);


        return linkData;
    }

    async removeLink(link: Link, isInput: boolean) {
        if (this.isInit) {
            return;
        }

        this.page.removeLink(link);

        if (isInput) {
            await this.ruleEngineService.removeLink(link);
        }

    }


}
