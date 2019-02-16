import { BaseModel, Model, JsonFieldInfo, JsonProperty } from "../base-model";
import { IPropertyModel } from "../interfaces";
import { PropertyInstance } from "../property-instance";
import { VirtualNamePropertyInstance, VirtualDescriptionPropertyInstance } from "../virtual-props";
import { INameModel } from "../INameModel";
import { UserGroup2Role } from "./usergroup2role";
import { UserGroupToRolesProperty } from "../virtual-props/usergroup/usergroup-to-roles-property";
import { IDescriptionModel } from "../IDescriptionModel";

@Model()
export class UserGroup extends BaseModel implements IPropertyModel, INameModel, IDescriptionModel {

    public get DisplayDescription(): string {
        return this.Description;
    }
    public set DisplayDescription(val: string) {
        this.Description = val;
    }
    public get DisplayName(): string {
        return this.Name;
    }
    public set DisplayName(val: string) {
        this.Name = val;
    }

    Properties: PropertyInstance[] = [];

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    InverseThis2Roles: UserGroup2Role[] = [];

    public addVirtualProperties() {
        this.Properties.push(new VirtualNamePropertyInstance(this, false));
        this.Properties.push(new VirtualDescriptionPropertyInstance(this, false));

        this.Properties.push(new UserGroupToRolesProperty(this));
    }

    afterFromJson() {
        this.addVirtualProperties();
    }

    public typeInfo(): string {
        return "UserGroup";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return new Map<string, JsonFieldInfo>();
    }
}
