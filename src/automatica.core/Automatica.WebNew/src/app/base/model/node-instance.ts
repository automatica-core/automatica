import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"
import { NodeTemplate } from "./node-template";
import { PropertyInstance } from "./property-instance"
import { BoardInterface } from "./board-interface"
import { VirtualNamePropertyInstance } from "./virtual-props/virtual-name-property-instance"
import { VirtualDescriptionPropertyInstance } from "./virtual-props/virtual-description-property-instance"
import { VirtualReadablePropertyInstance } from "./virtual-props/virtual-readable-property-instance"
import { VirtualWriteablePropertyInstance } from "./virtual-props/virtual-writeable-property-instance"
import { VirtualRemanentPropertyInstance } from "./virtual-props/virtual-remanent-property-instance"
import { ITreeNode } from "./ITreeNode";
import { INameModel } from "./INameModel";
import { IDescriptionModel } from "./IDescriptionModel";
import { VirtualPropertyInstance } from "./virtual-props/virtual-property-instance";
import { MetaHelper } from "../base/meta-helper";
import { IPropertyModel } from "./interfaces/ipropertyModel";
import { VirtualUseInVisuPropertyInstance } from "./virtual-props/virtual-use-in-visu-property-instance";
import { VirtualAreaPropertyInstance } from "./virtual-props/virtual-area-property-instance";
import { VirtualCategoryPropertyInstance } from "./virtual-props/virtual-category-property-instance";
import { IAreaInstanceModel } from "./IAreaInstanceModel";
import { ICategoryInstanceModel } from "./ICategoryInstanceModel";
import { VirtualUserGroupPropertyInstance } from "./virtual-props/virtual-usergroup-property-instance";
import { VirtualGenericPropertyInstance } from "./virtual-props/virtual-generic-property-instance";
import { NodeDataTypeEnum } from "./node-data-type";
import { PropertyTemplateType, EnumExtendedPropertyTemplate } from "./property-template";
import { VirtualGenericTrendingPropertyInstance } from "./virtual-props/node-instance/virtual-generic-trending-property";
import { INodeInstance } from "./INodeInstance";
import { VirtualObjIdPropertyInstance } from "./virtual-props/virtual-objid-property-instance";
import { CategoryInstance } from "./categories";
import { AreaInstance } from "./areas";
import { VirtualIsFavoriteVisuPropertyInstance } from "./virtual-props/virtual-is-fav-visu-property-instance";
import { VirtualSatellitePropertyInstance } from "./virtual-props/virtual-satellite-property-instance";
import { VirtualDisabledPropertyInstance } from "./virtual-props/virtual-disabled-property-instance";
import { VirtualOnlyWriteIfChangedPropertyInstance } from "./virtual-props/virtual-only-write-if-changed-property-instance";
import { VirtualCreatedAtPropertyInstance } from "./virtual-props/virtual-created-at-property-instance";
import { VirtualModifedAtPropertyInstance } from "./virtual-props/virtual-modified-at-property-instance";
import { ITimestampModifiedTrackingModel } from "./ITimestampModifiedTrackingModel";

class NodeInstanceMetaHelper {
    private static pad(num, size) {
        const s = "000000000" + num;
        return s.substr(s.length - size);
    }

    public static getName(nodeInstance: NodeInstance): string {

        const meta = nodeInstance.NodeTemplate.NameMeta;
        let addName = "";
        const matches = MetaHelper.checkMatches(meta);

        for (const ma of matches) {
            const wBraces = ma.substr(1, ma.length - 2);
            const split = wBraces.split(":");

            if (split && split.length >= 2) {
                const key = split[0];
                const value = split[1];
                let propValue = this.getValueForKey(key, value, nodeInstance);
                if (typeof propValue === "number") {
                    propValue = propValue.toString();

                    if (split.length >= 3) {
                        const padSize = split.length >= 3 ? split[2] : 2;
                        propValue = this.pad(propValue, padSize);
                    }

                    addName += propValue;
                } else {
                    addName += propValue;
                }
            }
        }

        if (addName !== "") {
            return addName;
        }

        return nodeInstance.Name;
    }

    private static getValueForKey(key: string, value: string, nodeInstance: NodeInstance) {
        if (key === "PROPERTY") {
            return nodeInstance.getPropertyValue(value);
        } else if (key === "NODE") {
            return nodeInstance[value];
        } else if (key === "CONST") {
            return value;
        }
        return "";
    }

}

export enum NodeInstanceState {
    New = 0,
    Saved = 1,
    Loaded = 2,
    Initialized = 3,
    InUse = 4,
    OutOfDataPoints = 5,
    UnknownError = 6,
    Unloaded = 7,
    Unknown = 8,
    Remote = 9,
    OutOfSatelliteLicenses = 10
}

export enum TrendingTypes {
    Average = 0,
    Raw = 1,
    Max = 2,
    Min = 3,
    OnChange = 4
}

export enum ValueSource {
    Read,
    Write,
    User
}

@Model()
export class NodeInstance extends BaseModel implements ITreeNode, INameModel, IDescriptionModel, IPropertyModel, IAreaInstanceModel, ICategoryInstanceModel, INodeInstance, ITimestampModifiedTrackingModel {

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    This2NodeTemplate?: string;

    @JsonProperty()
    This2ParentNodeInstance?: string;

    @JsonProperty()
    This2BoardInterface?: string;

    @JsonProperty()
    State: NodeInstanceState;
    
    @JsonProperty()
    Error: string;

    @JsonProperty()
    CreatedAt: Date;

    @JsonProperty()
    ModifiedAt: Date;

    public get ParentId() {
        if (this.This2BoardInterface) {
            return this.This2BoardInterface;
        }
        return this.This2ParentNodeInstance;
    }

    public get Id() {
        return this.ObjId;
    }


    private _name: string;
    private _displayName: string;

    public set DisplayName(value: string) {
        this._displayName = value;
        this._name = value;
    }

    public get DisplayName() {
        if (this._displayName) {
            return `${this._displayName}`;
        }
        if (this.VisuName) {
            return this.VisuName;
        }
        return this._name;
    }

    public get VisuDisplayName() {
        if (this.VisuName) {
            return this.VisuName;
        }
        if (this._displayName) {
            return `${this._displayName}`;
        }
        
        return this._name;
    }

    @JsonProperty()
    public get Name(): string {
        return this._name;
    }
    public set Name(v: string) {
        this._name = v;
        this.notifyChange("Name");
        this.updateDisplayName();
    }

    public get FullName(): string {
        var ret = void 0;

        if (this.Parent instanceof NodeInstance) {
            ret = this.Parent.FullName;
            if(this.Name)
                ret += ` → ${this.DisplayName}`;
        }

        if(!ret) {
            return "Root";
        }

        return ret;
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



    private _Value: any;
    public get Value(): any {
        return this._Value;
    }
    public set Value(v: any) {
        this._Value = v;
        this.notifyChange("Value");
    }

    
    private _writeValue : any;
    public get WriteValue() : any {
        return this._writeValue;
    }
    public set WriteValue(v : any) {
        this._writeValue = v;
        this.notifyChange("WriteValue");
    }
    
    private _readValue : any;
    public get ReadValue() : any {
        return this._readValue;
    }
    public set ReadValue(v : any) {
        this._readValue = v;
        this.notifyChange("ReadValue");
    }

    private _valueSource : ValueSource;
    public get ValueSource() : ValueSource {
        return this._valueSource;
    }
    public set ValueSource(v : ValueSource) {
        this._valueSource = v;
        this.notifyChange("ValueSource");
    }
    
    


    private _valueTimestamp: Date;
    public get ValueTimestamp(): Date {
        return this._valueTimestamp;
    }
    public set ValueTimestamp(v: Date) {
        this._valueTimestamp = v;
    }



    @JsonPropertyName("This2NodeTemplateNavigation")
    NodeTemplate: NodeTemplate;

    @JsonPropertyName("PropertyInstance")
    Properties: PropertyInstance[] = [];

    @JsonPropertyName("InverseThis2ParentNodeInstanceNavigation")
    Children: NodeInstance[] = [];

    @JsonProperty()
    IsReadable: boolean;
    @JsonProperty()
    IsWriteable: boolean;
    @JsonProperty()
    IsDisabled: boolean;


    public get IsAnyDisabled(): boolean {
        if (this.IsDisabled) {
            return true;
        }
        if (this.Parent instanceof NodeInstance) {
            return this.Parent.IsAnyDisabled;
        }
        return false;
    }


    @JsonProperty()
    UseInVisu: boolean;

    @JsonProperty()
    This2AreaInstance: string;

    @JsonProperty()
    This2AreaInstanceNavigation: AreaInstance;

    @JsonProperty()
    This2CategoryInstance: string;

    @JsonProperty()
    This2CategoryInstanceNavigation: CategoryInstance;

    @JsonProperty()
    IsFavorite: boolean;

    @JsonProperty()
    Rating: number;

    @JsonProperty()
    This2UserGroup: string;

    @JsonProperty()
    This2Slave: string;

    @JsonProperty()
    IsRemanent: boolean;


    @JsonProperty()
    StateTextValueTrue: string;
    @JsonProperty()
    StateTextValueFalse: string;
    @JsonProperty()
    StateColorValueTrue: string;
    @JsonProperty()
    StateColorValueFalse: string;

    @JsonProperty()
    VisuName: string;

    @JsonProperty()
    Trending: boolean;

    @JsonProperty()
    TrendingInterval: number;

    @JsonProperty()
    TrendingType: TrendingTypes;

    @JsonProperty()
    TrendingToCloud: boolean;

    @JsonProperty()
    WriteOnlyIfChanged: boolean;

    private _ValidationOk: boolean = true;
    public get ValidationOk(): boolean {
        return this._ValidationOk;
    }
    public set ValidationOk(v: boolean) {
        this._ValidationOk = v;
        if (this.Parent) {
            this.Parent.ValidationOk = v;
        }
    }

    constructor(public Parent: ITreeNode) {
        super();
        this.This2ParentNodeInstance = null;
        this.This2BoardInterface = null;

        this.StateColorValueFalse = "rgba(255, 255, 255, 1)";
        this.StateColorValueTrue = "rgba(255, 255, 255, 1)";
        this.StateTextValueTrue = "1";
        this.StateTextValueFalse = "0";
        this.State = NodeInstanceState.New;

        this.Properties = [];
        this.Children = [];
    }

    private addVirtualProperties() {

        if (!this.Properties) {
            this.Properties = [];
        }

        this.Properties.push(new VirtualNamePropertyInstance(this));
        this.Properties.push(new VirtualDescriptionPropertyInstance(this));

        this.Properties.push(new VirtualCreatedAtPropertyInstance(this));
        this.Properties.push(new VirtualModifedAtPropertyInstance(this));

        if (this.NodeTemplate) {
            this.Properties.push(new VirtualGenericPropertyInstance("TYPE", 2, this, () => this.translationService.translate(this.NodeTemplate.Name), void 0, true, PropertyTemplateType.Text, "COMMON.CATEGORY.MISC"));
        }

        this.Properties.push(new VirtualObjIdPropertyInstance(this));

        if (this.isDriverNode()) {
            this.Properties.push(new VirtualSatellitePropertyInstance(this));
        }

        if (this.Parent) {
            this.Properties.push(new VirtualDisabledPropertyInstance(this));
        }


        if (this.NodeTemplate && this.NodeTemplate.This2NodeDataType > 0) {
            this.Properties.push(new VirtualReadablePropertyInstance(this));

            if (!this.NodeTemplate.IsWriteableFixed) {
                this.Properties.push(new VirtualWriteablePropertyInstance(this));
                this.Properties.push(new VirtualRemanentPropertyInstance(this));
                this.Properties.push(new VirtualOnlyWriteIfChangedPropertyInstance(this));
            }

            this.Properties.push(new VirtualUseInVisuPropertyInstance(this));
            this.Properties.push(new VirtualAreaPropertyInstance(this));
            this.Properties.push(new VirtualCategoryPropertyInstance(this));
            this.Properties.push(new VirtualIsFavoriteVisuPropertyInstance(this));

            this.Properties.push(new VirtualUserGroupPropertyInstance(this));
            this.Properties.push(new VirtualGenericPropertyInstance("VISU_NAME", 5, this, () => this.VisuName, (value) => this.VisuName = value, false, PropertyTemplateType.Text, "COMMON.CATEGORY.VISU"));

            if (this.NodeTemplate.This2NodeDataType === NodeDataTypeEnum.Boolean) {
                this.Properties.push(new VirtualGenericPropertyInstance("STATUS_TEXT_TRUE", 6, this, () => this.StateTextValueTrue, (value) => this.StateTextValueTrue = value, false, PropertyTemplateType.Text, "COMMON.CATEGORY.VISU"));
                this.Properties.push(new VirtualGenericPropertyInstance("STATUS_TEXT_FALSE", 7, this, () => this.StateTextValueFalse, (value) => this.StateTextValueFalse = value, false, PropertyTemplateType.Text, "COMMON.CATEGORY.VISU"));

                this.Properties.push(new VirtualGenericPropertyInstance("STATUS_COLOR_TRUE", 8, this, () => this.StateColorValueTrue, (value) => this.StateColorValueTrue = value, false, PropertyTemplateType.Color, "COMMON.CATEGORY.VISU"));
                this.Properties.push(new VirtualGenericPropertyInstance("STATUS_COLOR_FALSE", 9, this, () => this.StateColorValueFalse, (value) => this.StateColorValueFalse = value, false, PropertyTemplateType.Color, "COMMON.CATEGORY.VISU"));
            }
            this.Properties.push(new VirtualGenericPropertyInstance("TRENDING", 10, this, () => this.Trending, (value) => this.Trending = value, false, PropertyTemplateType.Bool, "COMMON.CATEGORY.TRENDING"));
            this.Properties.push(new VirtualGenericTrendingPropertyInstance(this, "TRENDING_TYPE", 11, this, () => this.TrendingType, (value) => this.TrendingType = value, false, PropertyTemplateType.Enum, EnumExtendedPropertyTemplate.createFromEnum(TrendingTypes)));
            this.Properties.push(new VirtualGenericTrendingPropertyInstance(this, "TRENDING_INTERVAL", 12, this, () => this.TrendingInterval, (value) => this.TrendingInterval = value, false, PropertyTemplateType.Numeric));


            this.Properties.push(new VirtualGenericPropertyInstance("VALUE", 1, this, () => this.Value, void 0, false, PropertyTemplateType.Text, "COMMON.CATEGORY.VALUE", false));
            this.Properties.push(new VirtualGenericPropertyInstance("VALUE_TIMESTAMP", 2, this, () => this.ValueTimestamp, void 0, false, PropertyTemplateType.DateTime, "COMMON.CATEGORY.VALUE", false));
            this.Properties.push(new VirtualGenericPropertyInstance("VALUE_SOURCE", 3, this, () => ValueSource[this.ValueSource], void 0, false, PropertyTemplateType.Text, "COMMON.CATEGORY.VALUE", false));

        }

        this.updateDisplayName();

        for (const x of this.Properties) {
            x.propertyChanged.subscribe(() => {
                this.updateDisplayName();
                this.notifyChange("Properties");
            });
        }
    }

    private isDriverNode() {
        if (this.NodeTemplate && this.NodeTemplate.ProvidesInterface && this.NodeTemplate.ProvidesInterface.IsDriverInterface) {
            return true;
        }
        return false;
    }

    public setParent(parent: ITreeNode) {
        if (parent instanceof NodeInstance) {
            this.This2BoardInterface = void 0;
            this.This2ParentNodeInstance = parent.Id;
        } else if (parent instanceof BoardInterface) {
            this.This2BoardInterface = parent.ObjId;
            this.This2ParentNodeInstance = void 0;
        }
        this.Parent = parent;
    }

    useBaseModelInstanceForJson(baseModel: BaseModel) {
        if (baseModel instanceof VirtualPropertyInstance) {
            return false;
        }
        if (baseModel instanceof NodeTemplate) {
            return false;
        }
        return true;
    }

    protected afterFromJson() {
        super.afterFromJson();

        this.addVirtualProperties();

        for (const x of this.Properties) {
            x.notifyChangeEvent.subscribe((n) => {
                this.notifyChange("Property" + n);
            });
        }

    }
    public updateDisplayName() {
        if (this.NodeTemplate && this.NodeTemplate.NameMeta) {
            this._displayName = NodeInstanceMetaHelper.getName(this);
        }
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public setPropertyValueIfPresent(property: string, value: any) {
        for (const e of this.Properties) {
            if (e.PropertyTemplate.Key === property) {
                e.Value = value;
            }
        }
    }

    public getPropertyValue(property: string): any {
        for (const e of this.Properties) {
            if (e.PropertyTemplate && e.PropertyTemplate.Key === property) {
                return e.Value;
            }
        }
        return void 0;
    }

    getPropertyValueById(id: string) {
        for (const e of this.Properties) {
            if (e.PropertyTemplate.ObjId === id) {
                return e.Value;
            }
        }
        return void 0;
    }

    validate(): boolean {
        let result = false;

        for (const x of this.Properties) {
            result = result || x.validate();
        }
        return result;
    }

    public hasPropertyValue(property: string): boolean {
        for (const e of this.Properties) {
            if (e.PropertyTemplate.Key === property) {
                return true;
            }
        }
        return false;
    }

    public typeInfo(): string {
        return "NodeInstance";
    }
}
