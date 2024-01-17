import { BaseModel, JsonFieldInfo, JsonProperty, Model } from "./base-model"

export enum LogicInterfaceDirection
{
    Input  = 1,
    Output = 2,
    Param = 3
}

@Model()
export class RuleInterfaceDirection extends BaseModel {

    @JsonProperty()
    ObjId: number;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    Key: string;

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }
    public typeInfo(): string {
        return "RuleInterfaceDirection";
    }
}
