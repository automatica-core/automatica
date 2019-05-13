import { BaseModel, JsonFieldInfo, JsonProperty, Model } from "./base-model"

@Model()
export class InterfaceType extends BaseModel {
    @JsonProperty()
    Type: string;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    MaxChilds: number;

    @JsonProperty()
    MaxInstances: number;

    @JsonProperty()
    CanProvideBoardType: boolean;

    @JsonProperty()
    IsDriverInterface: boolean;


    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "InterfaceType";
    }
}
