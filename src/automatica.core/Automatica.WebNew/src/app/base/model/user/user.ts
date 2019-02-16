import { BaseModel, Model, JsonFieldInfo, JsonProperty } from "../base-model";
import { User2Role } from "./user2role";
import { IPropertyModel } from "../interfaces";
import { PropertyInstance } from "../property-instance";
import { VirtualGenericPropertyInstance } from "../virtual-props/virtual-generic-property-instance";
import { PropertyTemplateType } from "../property-template";
import { User2Group } from "./user2group";
import { UserToGroupProperty } from "../virtual-props/user/user-to-groups-property";
import { UserToRolesProperty } from "../virtual-props/user/user-to-roles-property";

@Model()
export class User extends BaseModel implements IPropertyModel {

    Properties: PropertyInstance[] = [];

    @JsonProperty()
    FirstName: string;

    @JsonProperty()
    LastName: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    UserName: string;

    @JsonProperty()
    Password: string;

    @JsonProperty()
    PasswordConfirm: string;

    @JsonProperty()
    InverseThis2Roles: User2Role[] = [];

    @JsonProperty()
    InverseThis2UserGroups: User2Group[] = [];

    @JsonProperty()
    Token: string;

    public typeInfo(): string {
        return "User";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return new Map<string, JsonFieldInfo>();
    }

    addVirtualProperties(): any {
        this.Properties.push(new VirtualGenericPropertyInstance("USERNAME", 1, this, () => this.UserName, (value) => this.UserName = value, !this.isNewObject));
        this.Properties.push(new VirtualGenericPropertyInstance("FIRSTNAME", 2, this, () => this.FirstName, (value) => this.FirstName = value));
        this.Properties.push(new VirtualGenericPropertyInstance("LASTNAME", 3, this, () => this.LastName, (value) => this.LastName = value));
        this.Properties.push(new VirtualGenericPropertyInstance("DESCRIPTION", 3, this, () => this.Description, (value) => this.Description = value));

        this.Properties.push(new VirtualGenericPropertyInstance("PASSWORD", 5, this, () => this.Password, (value) => this.Password = value, false, PropertyTemplateType.Password));
        this.Properties.push(new VirtualGenericPropertyInstance("PASSWORD_CONFIRM", 6, this, () => this.PasswordConfirm, (value) => this.PasswordConfirm = value, false, PropertyTemplateType.Password));

        this.Properties.push(new UserToGroupProperty(this));
        this.Properties.push(new UserToRolesProperty(this));


    }

    afterFromJson() {
        this.addVirtualProperties();
    }

    validate(): boolean {
        if (this.Password !== this.PasswordConfirm) {
            return false;
        }
        if (!this.UserName) {
            return false;
        }
        return true;
    }

}
