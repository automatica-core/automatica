import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"
import { RuleInterfaceDirection } from "./rule-interface-direction"

export enum RuleInterfaceParameterDataType {
    NoParameter,
    Integer,
    Double,
    Text,
    Timer
}

export enum RuleInterfaceDirectionEnum {
    Input = 1,
    Output,
    Param
}

@Model()
export class RuleInterfaceTemplate extends BaseModel {

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    This2RuleTemplate: string;

    @JsonProperty()
    This2RuleInterfaceDirection: number;

    @JsonProperty()
    MaxLinks: number;

    @JsonProperty()
    SortOrder: number;

    @JsonPropertyName("This2RuleInterfaceDirectionNavigation")
    InterfaceDirection: RuleInterfaceDirection;

    @JsonProperty()
    ParameterDataType: RuleInterfaceParameterDataType;

    @JsonProperty()
    DefaultValue: string;

    @JsonProperty()
    IsLinkableParameter: boolean;


    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "RuleInterfaceTemplate";
    }
}
