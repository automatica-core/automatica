import { Model, BaseModel, JsonFieldInfo, JsonProperty } from "../base-model";

@Model()
export class AreaType extends BaseModel {

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    public typeInfo(): string {
        return "AreaType";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }
}
