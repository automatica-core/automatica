import { BaseModel, JsonFieldInfo, Model, JsonProperty, JsonPropertyName } from "../base-model";

@Model()
export class Trending extends BaseModel {

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    This2NodeInstance: string;

    @JsonProperty()
    Value: number;

    @JsonProperty()
    TimestampIso: Date = new Date();

    @JsonProperty()
    Source: string;


    public typeInfo(): string {
        return "Trending";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return new Map<string, JsonFieldInfo>();
    }

}