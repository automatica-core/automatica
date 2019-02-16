import { VirtualPropertyInstance } from "../virtual-property-instance"
import { User } from "../../user/user";
import { User2Group } from "../../user/user2group";
import { UserGroup } from "../../user/user-group";
import { User2Role } from "../../user/user2role";
import { PropertyTemplateType } from "../../property-template";

export class UserToRolesProperty extends VirtualPropertyInstance {
    private _dropDownOpened: boolean;

    private _valueIds: string[] = [];
    private _gridBoxValue: string[] = [];

    constructor(private user: User) {
        super(user);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.USER2ROLE.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.USER2ROLE.DESCRIPTION"
        this.PropertyTemplate.Key = "user2role";

        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.User2Roles;

        this.PropertyTemplate.Group = "COMMON.CATEGORY.USER2ROLE";

        for (const x of this.user.InverseThis2Roles) {
            this._valueIds.push(x.This2Role);
        }
    }

    private syncUser2RolesArray() {
        if (!this.ValueIds) {
            return;
        }
        this.user.InverseThis2Roles = [];
        for (const x of this.ValueIds) {
            const this2Role = new User2Role();
            this2Role.This2Role = x;
            this2Role.This2User = this.user.ObjId;

            this.user.InverseThis2Roles.push(this2Role);
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

    get Value(): User2Role[] {
        return this.user.InverseThis2Roles;
    }
    set Value(value: User2Role[]) {
        if (!value) {
            this.user.InverseThis2Roles = [];
            return;
        }
        this.user.InverseThis2Roles = value;
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
