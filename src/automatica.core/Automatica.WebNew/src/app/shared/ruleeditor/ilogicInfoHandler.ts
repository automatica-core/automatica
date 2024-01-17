import { RuleInstance } from "src/app/base/model/rule-instance";

export interface ILogicInfoHandler {
    showInfoForLogic(logic: RuleInstance);
}