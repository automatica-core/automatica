import { Model, BaseModel, JsonFieldInfo, JsonProperty, JsonPropertyName } from "./base-model";
import { PropertyTemplate } from "./property-template";

@Model()
export class VisuObjectTemplate extends BaseModel {

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    Key: string;

    @JsonProperty()
    Group: string;

    @JsonProperty()
    Width: number;

    @JsonProperty()
    Height: number;

    @JsonProperty()
    This2VisuPageType: number;

    @JsonPropertyName("PropertyTemplate")
    Properties: PropertyTemplate[] = [];

    @JsonProperty()
    IsVisibleForUser: boolean;

    constructor() {
        super();

    }
    public typeInfo(): string {
        return "VisuObjectTemplate";
    }


    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

}
