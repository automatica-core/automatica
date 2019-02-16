import { BaseModel, JsonFieldInfo, JsonProperty, Model } from "./base-model"

@Model()
export class RulePageType extends BaseModel {
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
        return "RulePageType";
    }
}
