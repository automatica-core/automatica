import { Model, BaseModel, JsonFieldInfo, JsonProperty } from "../base-model";

@Model()
export class AreaTemplate extends BaseModel {


    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    Icon: string;

    @JsonProperty()
    This2AreaType: string;

    @JsonProperty()
    ProvidesThis2AreayType: string;

    @JsonProperty()
    NeedsThis2AreaType: string;

    @JsonProperty()
    This2AreaTypeNavigation: AreaTemplate;

    @JsonProperty()
    ProvidesThis2AreayTypeNavigation: AreaTemplate;

    @JsonProperty()
    NeedsThis2AreaTypeNavigation: AreaTemplate;

    @JsonProperty()
    IsDeleteable: boolean;

    public typeInfo(): string {
        return "AreaTemplate";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

}
