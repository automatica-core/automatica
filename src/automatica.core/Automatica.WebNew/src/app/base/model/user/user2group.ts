import { BaseModel, Model, JsonFieldInfo, JsonProperty } from "../base-model";
import { UserGroup } from "./user-group";

@Model()
export class User2Group extends BaseModel {

    @JsonProperty()
    This2UserGroup: string;

    @JsonProperty()
    This2User: string;

    // @JsonProperty()
    // This2UserGroupNavigation: UserGroup;

    public typeInfo(): string {
        return "User2Group";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return new Map<string, JsonFieldInfo>();
    }
}
