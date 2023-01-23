import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"
import { RuleInterfaceInstance } from "./rule-interface-instance"
import { NodeInstance2RulePage } from "./node-instance-2-rule-page"
import { EventEmitter } from "@angular/core";

export interface LinkChangeData {
    link: Link;
    type: "from" | "fromPort" | "to" | "toPort";
    value: any;
}

@Model()
export class Link extends BaseModel {

    public toPortChange = new EventEmitter<any>();
    public toChange = new EventEmitter<any>();
    public fromPortChange = new EventEmitter<any>();
    public fromChange = new EventEmitter<any>();

    public linkChange = new EventEmitter<LinkChangeData>();

    @JsonProperty()
    ObjId: string;
    @JsonProperty()
    This2NodeInstance2RulePageOutput: string;
    @JsonProperty()
    This2NodeInstance2RulePageInput: string;

    @JsonProperty()
    This2RuleInterfaceInstanceInput: string;
    @JsonProperty()
    This2RuleInterfaceInstanceOutput: string;

    @JsonProperty()
    This2RulePage: string;

    @JsonPropertyName("This2RuleInterfaceInstanceOutputNavigation")
    FromRuleInstance: RuleInterfaceInstance;
    @JsonPropertyName("This2RuleInterfaceInstanceInputNavigation")
    ToRuleInstance: RuleInterfaceInstance;
    @JsonPropertyName("This2NodeInstance2RulePageOutputNavigation") // output is from -> input is to
    FromNodeInstance: NodeInstance2RulePage;
    @JsonPropertyName("This2NodeInstance2RulePageInputNavigation")
    ToNodeInstance: NodeInstance2RulePage;

    constructor() {
        super();
        this.This2NodeInstance2RulePageOutput = void 0;
        this.This2NodeInstance2RulePageInput = void 0;
        this.This2RuleInterfaceInstanceInput = void 0;
        this.This2RuleInterfaceInstanceOutput = void 0;

    }

    get isValid() {
        if (this.This2NodeInstance2RulePageOutput && this.This2NodeInstance2RulePageInput) {
            return true;
        }
        if (this.This2RuleInterfaceInstanceOutput && this.This2RuleInterfaceInstanceInput) {
            return true;
        }
        if ( this.This2RuleInterfaceInstanceOutput && this.This2NodeInstance2RulePageInput) {
            return true;
        }
        if (this.This2NodeInstance2RulePageOutput && this.This2RuleInterfaceInstanceInput) {
            return true;
        }
        return false;
    }


    get fromPort() {
        if (this.This2RuleInterfaceInstanceOutput) {
            return this.This2RuleInterfaceInstanceOutput;
        }

        if (this.This2NodeInstance2RulePageOutput) {
            return this.This2NodeInstance2RulePageOutput + "O";
        }
        return undefined;
    }

    set fromPort(value) {
        this.fromPortChange.emit(value);
        this.linkChange.emit({
            link: this,
            type: "fromPort",
            value: value
        });
    }

    get from() {
        if (this.This2RuleInterfaceInstanceOutput) {
            return this.FromRuleInstance.RuleInstance.key;
        }

        if (this.This2NodeInstance2RulePageOutput) {
            return this.FromNodeInstance.key;
        }
        return undefined;
    }

    set from(value) {
        this.fromChange.emit(value);
        this.linkChange.emit({
            link: this,
            type: "from",
            value: value
        });
    }

    get to() {
        if (this.This2RuleInterfaceInstanceInput) {
            return this.ToRuleInstance.RuleInstance.key;
        }

        if (this.This2NodeInstance2RulePageInput) {
            return this.ToNodeInstance.key;
        }
        return undefined;
    }


    set to(value) {
        this.toChange.emit(value);
        this.linkChange.emit({
            link: this,
            type: "to",
            value: value
        });
    }

    get toPort() {
        if (this.This2RuleInterfaceInstanceInput) {
            return this.This2RuleInterfaceInstanceInput;
        }

        if (this.This2NodeInstance2RulePageInput) {
            return this.This2NodeInstance2RulePageInput + "I";
        }
        return undefined;
    }

    set toPort(value) {
        this.toPortChange.emit(value);
        this.linkChange.emit({
            link: this,
            type: "toPort",
            value: value
        });
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "Link";
    }
}
