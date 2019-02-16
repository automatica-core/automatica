import { BaseModel, JsonFieldInfo, JsonProperty, Model } from "./base-model"

export enum NodeDataTypeEnum {
    NoAttribute = 0,
    Integer,
    Double,
    String,
    Boolean,
    DateTime,
    Time,
    Date
}

@Model()
export class NodeDataType extends BaseModel {

    @JsonProperty()
    Type: number;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;


    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "NodeDataType";
    }
}
