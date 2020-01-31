import { BaseModel, JsonFieldInfo, JsonProperty, JsonPropertyName, Model } from "./base-model";
import { VisuObjectTemplate } from "./visu-object-template";
import { PropertyInstance } from "./property-instance";
;
import { Guid } from "../utils/Guid";
import { VirtualUserGroupPropertyInstance } from "./virtual-props/virtual-usergroup-property-instance";
import { IPropertyModel } from "./interfaces/ipropertyModel";
import { RuleInstance } from "./rule-instance";
import { NodeInstance } from "./node-instance";

@Model()
export class VisuObjectInstance extends BaseModel implements IPropertyModel {
    isNewObject: boolean;
    private _X: number;
    private _Y: number;
    private _Height: number
    private _Width: number;


    @JsonProperty()
    public ObjId: string;

    private _Name: string;
    @JsonProperty()
    public get Name(): string {
        return this._Name;
    }
    public set Name(v: string) {
        this._Name = v;
        this.notifyChange("Name");
    }


    private _Description: string;
    @JsonProperty()
    public get Description(): string {
        return this._Description;
    }
    public set Description(v: string) {
        this._Description = v;
        this.notifyChange("Description");
    }


    @JsonProperty()
    public This2VisuObjectTemplate: string;

    @JsonPropertyName("This2VisuObjectTemplateNavigation")
    VisuObjectTemplate: VisuObjectTemplate;

    @JsonPropertyName("PropertyInstance")
    ObjectProperties: PropertyInstance[] = [];

    @JsonProperty()
    IsFavorite: boolean;

    @JsonProperty()
    Rating: number;

    @JsonProperty()
    This2UserGroup: string;

    Properties: PropertyInstance[] = [];


    StateTextValueTrue: string;
    StateTextValueFalse: string;
    StateColorValueTrue: string;
    StateColorValueFalse: string;

    RuleInstance: RuleInstance;


    private _VisuName: string;
    public get VisuName(): string {
        return this._VisuName;
    }
    public set VisuName(v: string) {
        this._VisuName = v;
    }


    public static CreateFromTemplate(template: VisuObjectTemplate): VisuObjectInstance {
        const instance = new VisuObjectInstance();

        return this.FillNewInstance(instance, template);
    }

    protected static FillNewInstance(instance: VisuObjectInstance, template: VisuObjectTemplate): any {
        instance.Name = template.Name;
        instance.Description = template.Description;
        instance.ObjectProperties = [];
        instance.VisuObjectTemplate = template;
        instance.This2VisuObjectTemplate = template.ObjId;
        instance.ObjId = Guid.MakeNew().ToString();

        for (const p of template.Properties) {
            const prop: PropertyInstance = PropertyInstance.createFromTemplate(instance, p);
            prop.This2VisuObjectInstance = instance.ObjId;
            instance.ObjectProperties.push(prop);
        }

        instance.afterFromJson();

        return instance;
    }

    constructor() {
        super();

    }


    addVirtualProperties() {
        this.Properties = [...this.ObjectProperties];
        this.Properties.push(new VirtualUserGroupPropertyInstance(this));
    }

    protected afterFromJson() {
        super.afterFromJson();

        for (const x of this.ObjectProperties) {
            x.notifyChangeEvent.subscribe((n) => {
                this.notifyChange("Property" + n);
            });
        }


        this.addVirtualProperties();
    }

    public getPropertyValue(key: string) {
        for (const x of this.ObjectProperties) {
            if (x.PropertyTemplate.Key === key) {
                return x.Value;
            }
        }
        return void 0;
    }


    public getProperty(key: string): PropertyInstance {
        for (const x of this.ObjectProperties) {
            if (x.PropertyTemplate.Key === key) {
                return x;
            }
        }
        return void 0;
    }

    public createInstance() {
        return new VisuObjectInstance();
    }

    @JsonProperty()
    public get X(): number {
        return this._X;
    }
    public set X(v: number) {
        this._X = v;
    }


    @JsonProperty()
    public get Y(): number {
        return this._Y;
    }
    public set Y(v: number) {
        this._Y = v;
    }

    @JsonProperty()
    public get Height(): number {
        return this._Height;
    }
    public set Height(v: number) {
        this._Height = v;
    }

    @JsonProperty()
    public get Width(): number {
        return this._Width;
    }
    public set Width(v: number) {
        this._Width = v;
    }




    public typeInfo(): string {
        return "VisuObjectInstance";
    }



    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }
}
