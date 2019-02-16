import { BaseModel, Model, JsonFieldInfo, JsonProperty } from "../base-model";
import { Role } from "./role";

@Model()
export class User2Role extends BaseModel {

    @JsonProperty()
    This2Role: string;

    @JsonProperty()
    This2User: string;

    @JsonProperty()
    This2RoleNavigation: Role;

    public typeInfo(): string {
        return "User2Role";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return new Map<string, JsonFieldInfo>();
    }
}
