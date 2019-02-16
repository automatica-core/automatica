import { Model, BaseModel, JsonFieldInfo, JsonProperty } from "../base-model";
import { Guid } from "../../utils/Guid";
import { IPropertyModel } from "../interfaces/ipropertyModel";
import { INameModel } from "../INameModel";
import { IDescriptionModel } from "../IDescriptionModel";
import { PropertyInstance } from "../property-instance";
import { VirtualNamePropertyInstance } from "../virtual-props/virtual-name-property-instance";
import { VirtualDescriptionPropertyInstance } from "../virtual-props/virtual-description-property-instance";
import { VirtualIconProperty } from "../virtual-props/virtual-icon-property";
import { VirtualDisplayNamePropertyInstance } from "../virtual-props/virtual-display-name-property-instance";
import { VirtualDisplayDescriptionPropertyInstance } from "../virtual-props/virtual-display-description-property-instance";
import { VirtualColorPropertyInstance } from "../virtual-props/virtual-color-property-instance";
import { IIconModel } from "../IIconMode";
import { VirtualUserGroupPropertyInstance } from "../virtual-props/virtual-usergroup-property-instance";

@Model()
export class CategoryInstance extends BaseModel implements IPropertyModel, INameModel, IDescriptionModel, IIconModel {

    Properties: PropertyInstance[] = [];

    public get DisplayName(): string {
        if (this.IsDeleteable) {
            return this.Name;
        }
        return this.translationService.translate(this.Name);
    }
    public set DisplayName(v: string) {
        if (this.IsDeleteable) {
            this.Name = v;
        }
    }

    public get DisplayDescription(): string {
        if (this.IsDeleteable) {
            return this.Description;
        }
        return this.translationService.translate(this.Description);
    }
    public set DisplayDescription(v: string) {
        if (this.IsDeleteable) {
            this.Description = v;
        }
    }

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    IsDeleteable: boolean;

    @JsonProperty()
    This2CategoryGroup: string;

    @JsonProperty()
    Color: string;

    @JsonProperty()
    IsFavorite: boolean;

    @JsonProperty()
    Rating: number;

    @JsonProperty()
    This2UserGroup: string;


    private _Icon: string;

    @JsonProperty()
    public get Icon(): string {
        return this._Icon;
    }
    public set Icon(v: string) {
        this._Icon = v;
    }


    constructor() {
        super();

        this.IsDeleteable = true;

        this.Name = "";
        this.Description = "";
        this.Icon = "fas question";
        this.Color = "rgba(255, 255, 255, 1)";

        this.This2CategoryGroup = "f61737b4-f948-4d41-9af0-42b93997ee0d";
    }

    addVirtualProperties() {
        this.Properties.push(new VirtualDisplayNamePropertyInstance(this, !this.IsDeleteable));
        this.Properties.push(new VirtualDisplayDescriptionPropertyInstance(this, !this.IsDeleteable));

        this.Properties.push(new VirtualColorPropertyInstance(this, () => this.Color, (value) => {
            this.Color = value;
        }));
        this.Properties.push(new VirtualIconProperty(this));

        this.Properties.push(new VirtualUserGroupPropertyInstance(this));
    }

    afterFromJson() {
        this.addVirtualProperties();
    }

    public typeInfo(): string {
        return "CategoryInstance";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

}
