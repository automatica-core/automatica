import { BaseModel, JsonFieldInfo, JsonProperty, Model } from "./base-model";
import { RuleInstance } from "./rule-instance";
import { NodeInstance } from "./node-instance";

@Model()
export class AutomaticVisualizationData extends BaseModel {

    private _ruleInstances: RuleInstance[] = [];
    private _nodeInstances: NodeInstance[] = [];

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "AutomaticVisualizationData";
    }

    @JsonProperty()
    public get RuleInstances(): RuleInstance[] {
        return this._ruleInstances;
    }
    public set RuleInstances(v: RuleInstance[]) {
        this._ruleInstances = v;
    }

    public get NodeInstances(): NodeInstance[] {
        return this._nodeInstances;
    }

    @JsonProperty()
    public set NodeInstances(v: NodeInstance[]) {
        this._nodeInstances = v;
    }

}
