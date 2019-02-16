import { BaseModel, Model, JsonFieldInfo, JsonProperty } from "../base-model";
import { Role } from "./role";

@Model()
export class UserGroup2Role extends BaseModel {

    @JsonProperty()
    This2Role: string;

    @JsonProperty()
    This2UserGroup: string;

    @JsonProperty()
    This2RoleNavigation: Role;

    public typeInfo(): string {
        return "UserGroup2Role";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return new Map<string, JsonFieldInfo>();
    }
}
