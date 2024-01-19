import { BaseModel, JsonFieldInfo, JsonProperty, Model } from "./base-model"

export enum NodeDataTypeEnum {
    NoAttribute = 0,
    Integer = 1,
    Double = 2,
    String = 3,
    Boolean = 4,
    DateTime = 5,
    Time = 6,
    Date = 7,
    WindowState = 8
}

@Model()
export class NodeDataType extends BaseModel {

    @JsonProperty()
    Type: number;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    NodeDataTypeEnum: NodeDataTypeEnum;

    protected afterFromJson(): void {
        this.NodeDataTypeEnum = <NodeDataTypeEnum>this.Type;
    }


    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "NodeDataType";
    }
}
