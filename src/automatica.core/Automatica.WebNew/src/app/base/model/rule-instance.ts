import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"
import { RuleInterfaceInstance } from "./rule-interface-instance"
import { RuleTemplate } from "./rule-template"
import { RulePage } from "./rule-page"
import { RuleInterfaceParameterDataType } from "./rule-interface-template"

import { IKey } from "./IKey";
import { VirtualPropertyInstance } from "./virtual-props/virtual-property-instance";
import { VirtualNamePropertyInstance } from "./virtual-props/virtual-name-property-instance";
import { VirtualDescriptionPropertyInstance } from "./virtual-props/virtual-description-property-instance";
import { IDescriptionModel } from "./IDescriptionModel";
import { INameModel } from "./INameModel";
import { Guid } from "../utils/Guid";
import { IPropertyModel } from "./interfaces/ipropertyModel";
import { RuleInterfaceParamProperty } from "./virtual-props/rule-instance/rule-interface-param-property";
import { ICategoryInstanceModel } from "./ICategoryInstanceModel";
import { IAreaInstanceModel } from "./IAreaInstanceModel";
import { VirtualUseInVisuPropertyInstance } from "./virtual-props/virtual-use-in-visu-property-instance";
import { VirtualAreaPropertyInstance } from "./virtual-props/virtual-area-property-instance";
import { VirtualCategoryPropertyInstance } from "./virtual-props/virtual-category-property-instance";
import { VirtualUserGroupPropertyInstance } from "./virtual-props/virtual-usergroup-property-instance";
import { AreaInstance } from "./areas"
import { CategoryInstance } from "./categories"
import { VisuObjectType } from "../visu/base-mobile-component"
import { VirtualIsFavoriteVisuPropertyInstance } from "./virtual-props/virtual-is-fav-visu-property-instance"
import { L10nTranslationService } from "angular-l10n"
import { EventEmitter } from "@angular/core"
import { VirtualObjIdPropertyInstance } from "./virtual-props/virtual-objid-property-instance"
import { ITimestampModifiedTrackingModel } from "./ITimestampModifiedTrackingModel"
import { VirtualCreatedAtPropertyInstance } from "./virtual-props/virtual-created-at-property-instance"
import { VirtualModifedAtPropertyInstance } from "./virtual-props/virtual-modified-at-property-instance"
import { PropertyTemplateType } from "./property-template"
import { VirtualGenericPropertyInstance } from "./virtual-props/virtual-generic-property-instance"

function sortBySortOrder(a: RuleInterfaceInstance, b: RuleInterfaceInstance) {
    if (!a.Template) {
        return 0;
    }
    return a.Template.SortOrder - b.Template.SortOrder;
};


@Model()
export class RuleInstance extends BaseModel implements VisuObjectType, IKey, IDescriptionModel, INameModel, IPropertyModel, IAreaInstanceModel, ICategoryInstanceModel, ITimestampModifiedTrackingModel {

    public static KeyPrefix: string = "Rule";

    

    @JsonProperty()
    ObjId: string;

    public onNameChanged = new EventEmitter<string>();

    private _name : string;
    @JsonProperty()
    public get Name() : string {
        return this._name;
    }
    public set Name(v : string) {
        this._name = v;
        this.onNameChanged?.emit(v);
    }
    
    @JsonProperty()
    VisuName: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    This2RuleTemplate: string;

    public itemMoved = false;

    _x: number;
    public get X() {
        return Math.round(this._x);
    }
    @JsonProperty()
    public set X(value: number) {
        this._x = value;
        this.itemMoved = true;
    }

    _y: number;
    public get Y() {
        return Math.round(this._y);
    }
    @JsonProperty()
    public set Y(value: number) {
        this._y = value;
        this.itemMoved = true;
    }

    @JsonPropertyName("This2RuleTemplateNavigation")
    RuleTemplate: RuleTemplate;

    public get RuleTemplateName(): string {
        if (!this.RuleTemplate) {
            return "";
        }
        return this.RuleTemplate.Name;
    }

    @JsonProperty()
    RulePage: RulePage;

    @JsonPropertyName("RuleInterfaceInstance")
    Interfaces: RuleInterfaceInstance[];

    @JsonProperty()
    UseInVisu: boolean;

    @JsonProperty()
    IsFavorite: boolean;

    @JsonProperty()
    This2AreaInstance: string;

    @JsonProperty()
    This2CategoryInstance: string;

    @JsonProperty()
    This2AreaInstanceNavigation: AreaInstance;

    @JsonProperty()
    This2CategoryInstanceNavigation: CategoryInstance;

    @JsonProperty()
    This2UserGroup: string;

    @JsonProperty()
    CreatedAt: Date;

    @JsonProperty()
    ModifiedAt: Date;

    Properties: VirtualPropertyInstance[] = [];

    public get DisplayName(): string {
        return this.Name;
    }

    public get VisuDisplayName() {
        if (this.VisuName) {
            return this.VisuName;
        }
        if (this.DisplayName) {
            return `${this.DisplayName}`;
        }
        
        return this._name;
    }

    private _DisplayDescription: string;
    public get DisplayDescription(): string {
        if (!this._DisplayDescription) {
            return this.Description;
        }
        return this._DisplayDescription;
    }
    public set DisplayDescription(v: string) {
        this._DisplayDescription = v;
    }

    public setSaved(): void {
        this.itemMoved = false;
    }

    public static fromRuleTemplate(template: RuleTemplate, translationService: L10nTranslationService): RuleInstance {
        const instance = new RuleInstance();
        instance.This2RuleTemplate = template.ObjId;
        instance.RuleTemplate = template;
        instance.Name = translationService.translate(template.Name);
        instance.Description =  translationService.translate(template.Description);

        instance.ObjId = Guid.MakeNew().ToString();

        instance.Interfaces = [];
        template.Interfaces.forEach(element => {
            const intre = RuleInterfaceInstance.fromTemplate(element, instance, translationService);
            intre.RuleInstance = instance;
            instance.Interfaces.push(intre);
        });

        instance.addVirtualProperties();
        return instance;
    }


    constructor() {
        super();
        this.X = 0;
        this.Y = 0;
    }

    get Location() {
        return this.X + " " + this.Y;
    }

    get key(): any {
        return RuleInstance.KeyPrefix + "-" + this.ObjId;
    }

    private addVirtualProperties() {
        this.Properties.push(new VirtualNamePropertyInstance(this));
        this.Properties.push(new VirtualDescriptionPropertyInstance(this));
        this.Properties.push(new VirtualObjIdPropertyInstance(this));

        this.Properties.push(new VirtualCreatedAtPropertyInstance(this));
        this.Properties.push(new VirtualModifedAtPropertyInstance(this));

        this.Properties.push(new VirtualUseInVisuPropertyInstance(this));
        this.Properties.push(new VirtualAreaPropertyInstance(this));
        this.Properties.push(new VirtualCategoryPropertyInstance(this));
        this.Properties.push(new VirtualIsFavoriteVisuPropertyInstance(this));

        this.Properties.push(new VirtualUserGroupPropertyInstance(this)); 
        this.Properties.push(new VirtualGenericPropertyInstance("VISU_NAME", 5, this, () => this.VisuName, (value) => this.VisuName = value, false, PropertyTemplateType.Text, "COMMON.CATEGORY.VISU"));

        for (const x of this.Interfaces) {
            if (x.Template && x.Template.ParameterDataType === RuleInterfaceParameterDataType.NoParameter) {
                continue;
            }
            this.Properties.push(new RuleInterfaceParamProperty(x));
        }
    }

    protected afterFromJson() {
        this.Interfaces = this.Interfaces.sort(sortBySortOrder);

        this.addVirtualProperties();
    }


    set Location(value: string) {
        const split = value.split(" ");

        this.X = parseFloat(split[0]);
        this.Y = parseFloat(split[1]);
    }


    get category(): string {
        return RuleInstance.KeyPrefix;
    }

    get Inputs(): RuleInterfaceInstance[] {
        const interfaces = new Array<RuleInterfaceInstance>();
        this.Interfaces.forEach(element => {
            if (element.Template.InterfaceDirection.Key && element.Template.InterfaceDirection.Key === "I") {
                interfaces.push(element);
            }
        });

        return [...interfaces, ...this.LinkableParameters];
    }

    get Outputs(): RuleInterfaceInstance[] {
        const interfaces = new Array<RuleInterfaceInstance>();
        this.Interfaces.forEach(element => {
            if (element.Template.InterfaceDirection.Key && element.Template.InterfaceDirection.Key === "O") {
                interfaces.push(element);
            }
        });

        return interfaces;
    }

    get LinkableParameters(): RuleInterfaceInstance[] {
        const interfaces = new Array<RuleInterfaceInstance>();
        this.Interfaces.forEach(element => {
            if (element.Template.InterfaceDirection.Key === "P" && element.Template.IsLinkableParameter) {
                interfaces.push(element);
            }
        });

        return interfaces;
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }
    public typeInfo(): string {
        return "RuleInstance";
    }

    public validate() {
        return true;
    }
}
