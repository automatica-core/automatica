import { VirtualPropertyInstance } from "../virtual-property-instance"
import { User } from "../../user/user";
import { User2Group } from "../../user/user2group";
import { PropertyTemplateType } from "../../property-template";

export class UserToGroupProperty extends VirtualPropertyInstance {
    private _dropDownOpened: boolean;

    private _valueIds: string[] = [];
    private _gridBoxValue: string[] = [];

    constructor(private user: User) {
        super(user);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.USER2GROUP.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.USER2GROUP.DESCRIPTION"
        this.PropertyTemplate.Key = "user2group";

        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.User2Groups;

        this.PropertyTemplate.Group = "COMMON.CATEGORY.USER2GROUP";

        for (const x of this.user.InverseThis2UserGroups) {
            this._valueIds.push(x.This2UserGroup);
        }
    }

    private syncUser2GroupsArray() {
        if (!this.ValueIds) {
            return;
        }
        this.user.InverseThis2UserGroups = [];
        for (const x of this.ValueIds) {
            const this2UserGroup = new User2Group();
            this2UserGroup.This2UserGroup = x;
            this2UserGroup.This2User = this.user.ObjId;

            this.user.InverseThis2UserGroups.push(this2UserGroup);
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

    get Value(): User2Group[] {
        return this.user.InverseThis2UserGroups;
    }
    set Value(value: User2Group[]) {
        if (!value) {
            this.user.InverseThis2UserGroups = [];
            return;
        }
        this.user.InverseThis2UserGroups = value;
        super.notifyChange("Value");
    }


    public get dropDownOpened(): boolean {
        return this._dropDownOpened;
    }
    public set dropDownOpened(v: boolean) {
        this._dropDownOpened = v;
        if (!v) {
            this.syncUser2GroupsArray();
        }
    }


}
