import { BaseModel, JsonFieldInfo, Model, JsonProperty } from "../base-model";

@Model()
export class Trending extends BaseModel {

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    This2NodeInstance: string;

    @JsonProperty()
    Value: number;

    @JsonProperty()
    Timestamp: Date;

    @JsonProperty()
    Source: string;


    public typeInfo(): string {
        return "Trending";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return new Map<string, JsonFieldInfo>();
    }

}