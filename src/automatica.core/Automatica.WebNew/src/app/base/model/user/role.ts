import { BaseModel, Model, JsonFieldInfo, JsonProperty } from "../base-model";
import { INameModel } from "../INameModel";
import { IDescriptionModel } from "../IDescriptionModel";

@Model()
export class Role extends BaseModel implements INameModel, IDescriptionModel {
    public static ADMIN_ROLE = "administrator";
    public static VISU_ROLE = "visu";

    @JsonProperty()
    Key: string;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    InverseThis2Roles: any[];

    public get DisplayName() {
        return this.translationService.translate(this.Name);
    }

    public get DisplayDescription() {
        return this.translationService.translate(this.Description);
    }

    public typeInfo(): string {
        return "Role";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return new Map<string, JsonFieldInfo>();
    }
}
