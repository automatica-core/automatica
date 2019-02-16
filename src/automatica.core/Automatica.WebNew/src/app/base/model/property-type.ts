import { BaseModel, JsonFieldInfo, JsonProperty, Model } from "./base-model"

@Model()
export class PropertyType extends BaseModel {

    @JsonProperty()
    Type: number;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    Meta: string;

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "PropertyType";
    }
}
