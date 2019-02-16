import { Model, BaseModel, JsonFieldInfo, JsonProperty } from "../base-model";
import { AreaTemplate } from "./area-template";
import { Guid } from "../../utils/Guid";
import { IPropertyModel } from "../interfaces/ipropertyModel";
import { INameModel } from "../INameModel";
import { IDescriptionModel } from "../IDescriptionModel";
import { PropertyInstance } from "../property-instance";
import { VirtualNamePropertyInstance } from "../virtual-props/virtual-name-property-instance";
import { VirtualDescriptionPropertyInstance } from "../virtual-props/virtual-description-property-instance";
import { VirtualIconProperty } from "../virtual-props/virtual-icon-property";
import { IIconModel } from "../IIconMode";
import { VirtualUserGroupPropertyInstance } from "../virtual-props/virtual-usergroup-property-instance";

@Model()
export class AreaInstance extends BaseModel implements IPropertyModel, INameModel, IDescriptionModel, IIconModel {

    Properties: PropertyInstance[] = [];


    public get DisplayName(): string {
        return this.Name;
    }
    public set DisplayName(v: string) {
        this.Name = v;
    }

    public get DisplayDescription(): string {
        return this.Description;
    }
    public set DisplayDescription(v: string) {
        this.Description = v;
    }

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    This2Parent: string;

    This2ParentNavigation: AreaInstance;

    @JsonProperty()
    This2AreaTemplate: string;

    @JsonProperty()
    This2AreaTemplateNavigation: AreaTemplate;

    @JsonProperty()
    InverseThis2ParentNavigation: AreaInstance[] = [];

    @JsonProperty()
    IsFavorite: boolean;

    @JsonProperty()
    Rating: number;

    @JsonProperty()
    This2UserGroup: string;
    private _Icon: string;

    @JsonProperty()
    public get Icon(): string {
        if (!this._Icon && this.This2AreaTemplateNavigation) {
            return this.This2AreaTemplateNavigation.Icon;
        }
        return this._Icon;
    }
    public set Icon(v: string) {
        this._Icon = v;
    }

    public static createFromTemplate(template: AreaTemplate, parent: AreaInstance) {
        const instance = new AreaInstance(parent);

        instance.Name = template.Name;
        instance.Description = "";
        instance.This2AreaTemplateNavigation = template;
        instance.This2AreaTemplate = template.ObjId;
        instance.ObjId = Guid.MakeNew().ToString();
        parent.InverseThis2ParentNavigation = [...parent.InverseThis2ParentNavigation, instance];
        instance.This2Parent = parent.ObjId;
        instance.Icon = template.Icon;

        instance.addVirtualProperties();
        return instance;
    }

    public static getSupportedAreaTemplats(instance: AreaInstance, templates: AreaTemplate[]): AreaTemplate[] {
        const template = instance.This2AreaTemplateNavigation;
        const supported: AreaTemplate[] = [];

        for (const x of templates) {
            if (x.This2AreaType === template.ProvidesThis2AreayType) {
                supported.push(x);
            }
        }

        return supported;
    }

    /**
     *
     */
    constructor(private parent?: AreaInstance) {
        super();
        this.This2ParentNavigation = parent;

    }

    private addVirtualProperties() {
        this.Properties.push(new VirtualNamePropertyInstance(this, false));
        this.Properties.push(new VirtualDescriptionPropertyInstance(this, false));

        if (this.parent) {
            this.Properties.push(new VirtualIconProperty(this));
        }
        this.Properties.push(new VirtualUserGroupPropertyInstance(this));
    }

    afterFromJson() {
        this.addVirtualProperties();
    }

    public typeInfo(): string {
        return "AreaInstance";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

}
