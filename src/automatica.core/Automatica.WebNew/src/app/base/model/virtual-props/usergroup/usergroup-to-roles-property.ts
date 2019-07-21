import { VirtualPropertyInstance } from "../virtual-property-instance"
import { UserGroup2Role } from "../../user/usergroup2role";
import { PropertyTemplateType } from "../../property-template";
import { IUserGroup } from "../../user/iuser-group";

export class UserGroupToRolesProperty extends VirtualPropertyInstance {
    private _dropDownOpened: boolean;

    private _valueIds: string[] = [];
    private _gridBoxValue: string[] = [];

    constructor(private userGroup: IUserGroup) {
        super(userGroup);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.USERGROUP2ROLE.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.USERGROUP2ROLE.DESCRIPTION"
        this.PropertyTemplate.Key = "user2role";

        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.User2Roles;

        this.PropertyTemplate.Group = "COMMON.CATEGORY.USERGROUP2ROLE";

        for (const x of this.userGroup.InverseThis2Roles) {
            this._valueIds.push(x.This2Role);
        }
    }

    private syncUser2RolesArray() {
        if (!this.ValueIds) {
            return;
        }
        this.userGroup.InverseThis2Roles = [];
        for (const x of this.ValueIds) {
            const this2Role = new UserGroup2Role();
            this2Role.This2Role = x;
            this2Role.This2UserGroup = this.userGroup.ObjId;

            this.userGroup.InverseThis2Roles.push(this2Role);
        }
    }

    get ValueIds(): string[] {
        return this._valueIds;
    }

    set ValueIds(value: string[]) {
        this._valueIds = value;
    }

    get ValueGrid(): string[] {
        return this._gridBoxValue;
    }

    set ValueGrid(value: string[]) {
        this._gridBoxValue = value;
    }

    get Value(): UserGroup2Role[] {
        return this.userGroup.InverseThis2Roles;
    }
    set Value(value: UserGroup2Role[]) {
        if (!value) {
            this.userGroup.InverseThis2Roles = [];
            return;
        }
        this.userGroup.InverseThis2Roles = value;
        super.notifyChange("Value");
    }


    public get dropDownOpened(): boolean {
        return this._dropDownOpened;
    }
    public set dropDownOpened(v: boolean) {
        this._dropDownOpened = v;
        if (!v) {
            this.syncUser2RolesArray();
        }
    }


}
